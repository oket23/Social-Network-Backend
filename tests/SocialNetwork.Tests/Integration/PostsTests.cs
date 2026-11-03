using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SocialNetwork.Application.Auth.Responses;
using SocialNetwork.Application.Common.Responses;
using SocialNetwork.Application.Posts.Responses;
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
        Assert.EndsWith(".jpg", post.Media[1].Url);
        Assert.Equal(auth.User.Username, post.Author.Username);
        Assert.True(post.IsMine);
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

        var page2 = await GetPage(client, $"{url}&cursor={Uri.EscapeDataString(page1.NextCursor.Value.ToString("O"))}");
        Assert.Equal([first.Id], page2.Items.Select(p => p.Id));
        Assert.Null(page2.NextCursor);
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
