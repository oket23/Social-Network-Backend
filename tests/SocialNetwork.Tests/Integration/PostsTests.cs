using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SocialNetwork.Application.Auth.Responses;
using SocialNetwork.Application.Common.Responses;
using SocialNetwork.Application.Posts.Responses;
using SocialNetwork.Domain.Posts;
using SocialNetwork.Domain.Storage;
using SocialNetwork.Infrastructure.Persistence;
using SocialNetwork.Infrastructure.Storage;
using SocialNetwork.Tests.Infrastructure;

namespace SocialNetwork.Tests.Integration;

[Collection(ApiCollection.Name)]
public class PostsTests
{
    private readonly ApiFactory _factory;

    public PostsTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, AuthResponse Auth)> CreateUser()
    {
        var auth = await _factory.CreateClient().Register();
        return (_factory.CreateClient().Authorize(auth.AccessToken), auth);
    }

    [Fact]
    public async Task Create_WithPhotos_ReturnsPostWithMediaInOrder()
    {
        var (client, auth) = await CreateUser();

        var response = await client.PostAsync("/posts", PostForm("Перший пост", "image/png", "image/jpeg"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var post = (await response.ReadEnvelope<PostResponse>()).Data!;
        Assert.Equal("Перший пост", post.Caption);
        Assert.Equal(2, post.Media.Count);
        Assert.EndsWith(".png", post.Media[0].Url);
        Assert.Equal(MediaType.Image, post.Media[0].Type);
        Assert.EndsWith(".jpg", post.Media[1].Url);
        Assert.Equal(MediaType.Image, post.Media[1].Type);
        Assert.Equal(auth.User.Username, post.Author.Username);
        Assert.True(post.IsMine);
    }

    [Fact]
    public async Task Create_WithVideo_ReturnsPostWithVideoMedia()
    {
        var (client, _) = await CreateUser();

        var response = await client.PostAsync("/posts", PostForm("Відео", "image/png", "video/mp4"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var post = (await response.ReadEnvelope<PostResponse>()).Data!;
        Assert.Equal(2, post.Media.Count);
        Assert.Equal(MediaType.Image, post.Media[0].Type);
        Assert.EndsWith(".mp4", post.Media[1].Url);
        Assert.Equal(MediaType.Video, post.Media[1].Type);
    }

    [Fact]
    public async Task Create_MediaTypeInJson_IsString()
    {
        var (client, _) = await CreateUser();

        var response = await client.PostAsync("/posts", PostForm("Відео", "image/png", "video/mp4"));

        // Фронт отримує "image"/"video", а не 0/1.
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"type\":\"image\"", json);
        Assert.Contains("\"type\":\"video\"", json);
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().PostAsync("/posts", PostForm("x", "image/png"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutPhotos_Returns400()
    {
        var (client, _) = await CreateUser();

        var response = await client.PostAsync("/posts", PostForm("Без фото"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Files", (await response.ReadEnvelope<object>()).Errors!.Keys);
    }

    [Fact]
    public async Task Create_TooManyPhotos_Returns400()
    {
        var (client, _) = await CreateUser();

        var response = await client.PostAsync("/posts", PostForm("x", Enumerable.Repeat("image/png", 11).ToArray()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WrongFileType_Returns400()
    {
        var (client, _) = await CreateUser();

        var response = await client.PostAsync("/posts", PostForm("x", "image/png", "text/plain"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OtherUser_SeesPostInFeed()
    {
        // Сценарій для демо: один користувач публікує пост, інший бачить його у стрічці.
        var (author, _) = await CreateUser();
        var post = await CreatePost(author, "Привіт усім!");
        var (reader, _) = await CreateUser();

        var feed = await GetPage(reader, "/feed?limit=50");

        var fromFeed = Assert.Single(feed.Items, p => p.Id == post.Id);
        Assert.Equal("Привіт усім!", fromFeed.Caption);
        Assert.False(fromFeed.IsMine);
    }

    [Fact]
    public async Task Update_ByOwner_ChangesCaption()
    {
        var (client, _) = await CreateUser();
        var post = await CreatePost(client, "Старий підпис");

        var response = await client.PutAsJsonAsync($"/posts/{post.Id}", new { caption = "Новий підпис" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.ReadEnvelope<PostResponse>()).Data!;
        Assert.Equal("Новий підпис", updated.Caption);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAndDelete_ByOtherUser_Return403()
    {
        var (owner, _) = await CreateUser();
        var post = await CreatePost(owner, "Мій пост");
        var (stranger, _) = await CreateUser();

        var update = await stranger.PutAsJsonAsync($"/posts/{post.Id}", new { caption = "Зламано" });
        var delete = await stranger.DeleteAsync($"/posts/{post.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        var unchanged = (await (await owner.GetAsync($"/posts/{post.Id}")).ReadEnvelope<PostResponse>()).Data!;
        Assert.Equal("Мій пост", unchanged.Caption);
    }

    [Fact]
    public async Task Delete_ByOwner_HidesPostEverywhere()
    {
        var (client, auth) = await CreateUser();
        var post = await CreatePost(client, "Тимчасовий");

        var delete = await client.DeleteAsync($"/posts/{post.Id}");

        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/posts/{post.Id}")).StatusCode);
        Assert.DoesNotContain((await GetPage(client, "/feed?limit=50")).Items, p => p.Id == post.Id);
        Assert.Empty((await GetPage(client, $"/users/{auth.User.Username}/posts")).Items);
    }

    [Fact]
    public async Task PrivateAccount_PostsVisibleOnlyToOwner()
    {
        var (owner, ownerAuth) = await CreateUser();
        await owner.PutAsJsonAsync("/users/me",
            new { username = ownerAuth.User.Username, displayName = "Приватний", isPrivate = true });
        var post = await CreatePost(owner, "Лише для мене");
        var (stranger, _) = await CreateUser();

        Assert.DoesNotContain((await GetPage(stranger, "/feed?limit=50")).Items, p => p.Id == post.Id);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"/posts/{post.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await stranger.GetAsync($"/users/{ownerAuth.User.Username}/posts")).StatusCode);

        Assert.Contains((await GetPage(owner, "/feed?limit=50")).Items, p => p.Id == post.Id);
        Assert.Single((await GetPage(owner, $"/users/{ownerAuth.User.Username}/posts")).Items);
    }

    [Fact]
    public async Task UserPosts_PaginatesWithCursor_NewestFirst()
    {
        var (client, auth) = await CreateUser();
        var first = await CreatePost(client, "1");
        var second = await CreatePost(client, "2");
        var third = await CreatePost(client, "3");
        var url = $"/users/{auth.User.Username}/posts?limit=2";

        var page1 = await GetPage(client, url);
        Assert.Equal([third.Id, second.Id], page1.Items.Select(p => p.Id));
        Assert.NotNull(page1.NextCursor);

        var page2 = await GetPage(client, $"{url}&cursor={Uri.EscapeDataString(page1.NextCursor!)}");
        Assert.Equal([first.Id], page2.Items.Select(p => p.Id));
        Assert.Null(page2.NextCursor);
    }

    [Fact]
    public async Task UserPosts_WithSameCreatedAt_NoneAreSkippedAcrossPages()
    {
        // Регресія: курсор лише за CreatedAt губив пости з однаковим часом створення на межі сторінок.
        var (client, auth) = await CreateUser();
        var first = await CreatePost(client, "1");
        var second = await CreatePost(client, "2");
        var third = await CreatePost(client, "3");
        await ForceSameCreatedAt(first.Id, second.Id, third.Id);

        var url = $"/users/{auth.User.Username}/posts?limit=2";
        var ids = new List<Guid>();
        string? cursor = null;
        do
        {
            var page = await GetPage(client, cursor is null ? url : $"{url}&cursor={Uri.EscapeDataString(cursor)}");
            ids.AddRange(page.Items.Select(p => p.Id));
            cursor = page.NextCursor;
        } while (cursor is not null);

        Assert.Equal(
            new[] { first.Id, second.Id, third.Id }.OrderBy(x => x),
            ids.OrderBy(x => x));
    }

    [Fact]
    public async Task Delete_WhenStorageFailsForOnePhoto_StillDeletesPostAndOtherPhoto()
    {
        var (client, _) = await CreateUser();
        var response = await client.PostAsync("/posts", PostForm("Два фото", "image/png", "image/jpeg"));
        var post = (await response.ReadEnvelope<PostResponse>()).Data!;
        _factory.Files.FailNextDeleteFor(post.Media[0].Url);

        var delete = await client.DeleteAsync($"/posts/{post.Id}");

        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/posts/{post.Id}")).StatusCode);
        Assert.True(_factory.Files.WasDeleted(post.Media[1].Url));
    }

    [Fact]
    public async Task Delete_WhenStorageFails_EnqueuesFileForRetry()
    {
        var (client, _) = await CreateUser();
        var response = await client.PostAsync("/posts", PostForm("Збій сховища", "image/png"));
        var post = (await response.ReadEnvelope<PostResponse>()).Data!;
        var url = post.Media[0].Url;
        _factory.Files.FailNextDeleteFor(url);

        var delete = await client.DeleteAsync($"/posts/{post.Id}");
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pending = await db.Set<FailedFileDeletion>().SingleAsync(f => f.Url == url);
        Assert.Equal(0, pending.Attempts);
        Assert.False(_factory.Files.WasDeleted(url));
    }

    [Fact]
    public async Task CleanupBackgroundService_RetriesDueFile_RemovesRowOnSuccess()
    {
        var (client, _) = await CreateUser();
        var response = await client.PostAsync("/posts", PostForm("Повторна спроба", "image/png"));
        var post = (await response.ReadEnvelope<PostResponse>()).Data!;
        var url = post.Media[0].Url;
        _factory.Files.FailNextDeleteFor(url);
        await client.DeleteAsync($"/posts/{post.Id}");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pending = await db.Set<FailedFileDeletion>().SingleAsync(f => f.Url == url);
            pending.NextAttemptAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        // Тепер сховище вже готове прийняти видалення — так, ніби збій минув.
        _factory.Files.AllowDeleteFor(url);
        var cleanup = _factory.Services.GetServices<IHostedService>().OfType<FileCleanupBackgroundService>().Single();
        await cleanup.ProcessDueAsync();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await verifyDb.Set<FailedFileDeletion>().AnyAsync(f => f.Url == url));
        Assert.True(_factory.Files.WasDeleted(url));
    }

    [Fact]
    public async Task CleanupBackgroundService_StillFailing_IncrementsAttemptsAndReschedules()
    {
        var (client, _) = await CreateUser();
        var response = await client.PostAsync("/posts", PostForm("Досі падає", "image/png"));
        var post = (await response.ReadEnvelope<PostResponse>()).Data!;
        var url = post.Media[0].Url;
        _factory.Files.FailNextDeleteFor(url);
        await client.DeleteAsync($"/posts/{post.Id}");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pending = await db.Set<FailedFileDeletion>().SingleAsync(f => f.Url == url);
            pending.NextAttemptAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        // Сховище й далі падає — сам факт повторної спроби вже показовий.
        var cleanup = _factory.Services.GetServices<IHostedService>().OfType<FileCleanupBackgroundService>().Single();
        await cleanup.ProcessDueAsync();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stillPending = await verifyDb.Set<FailedFileDeletion>().SingleAsync(f => f.Url == url);
        Assert.Equal(1, stillPending.Attempts);
        Assert.True(stillPending.NextAttemptAt > DateTime.UtcNow);
    }

    private async Task ForceSameCreatedAt(params Guid[] postIds)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sameMoment = DateTime.UtcNow;

        var posts = await db.Set<Post>().Where(p => postIds.Contains(p.Id)).ToListAsync();
        foreach (var post in posts)
        {
            post.CreatedAt = sameMoment;
        }

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task UserPosts_UnknownUser_Returns404()
    {
        var (client, _) = await CreateUser();

        var response = await client.GetAsync("/users/no_such_user_404/posts");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Feed_InvalidLimit_Returns400()
    {
        var (client, _) = await CreateUser();

        var response = await client.GetAsync("/feed?limit=500");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<PostResponse> CreatePost(HttpClient client, string caption)
    {
        var response = await client.PostAsync("/posts", PostForm(caption, "image/png"));
        response.EnsureSuccessStatusCode();
        return (await response.ReadEnvelope<PostResponse>()).Data!;
    }

    private static async Task<CursorPage<PostResponse>> GetPage(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return (await response.ReadEnvelope<CursorPage<PostResponse>>()).Data!;
    }

    private static MultipartFormDataContent PostForm(string caption, params string[] contentTypes)
    {
        var form = new MultipartFormDataContent { { new StringContent(caption), "caption" } };
        foreach (var contentType in contentTypes)
        {
            var file = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 1, 2, 3]);
            file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(file, "files", "photo");
        }

        return form;
    }
}
