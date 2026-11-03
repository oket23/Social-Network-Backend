using FluentValidation;
using Microsoft.AspNetCore.Identity;
using SocialNetwork.Application.Common.Abstractions;
using SocialNetwork.Application.Common.Exceptions;
using SocialNetwork.Application.Common.Files;
using SocialNetwork.Application.Common.Responses;
using SocialNetwork.Application.Posts.Requests;
using SocialNetwork.Application.Posts.Responses;
using SocialNetwork.Domain.Posts;
using SocialNetwork.Domain.Users;

namespace SocialNetwork.Application.Posts;

public class PostService : IPostService
{
    public const int MaxMediaCount = 10;
    public const int MaxCaptionLength = 2200;
    public const int MaxPageSize = 50;

    private const string PostNotFoundMessage = "Пост не знайдено.";
    private const string PrivateAccountMessage = "Це приватний акаунт.";

    private readonly IPostRepository _postRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorage _fileStorage;
    private readonly UserManager<User> _userManager;
    private readonly IValidator<CreatePostRequest> _createValidator;

    public PostService(
        IPostRepository postRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IFileStorage fileStorage,
        UserManager<User> userManager,
        IValidator<CreatePostRequest> createValidator)
    {
        _postRepository = postRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _userManager = userManager;
        _createValidator = createValidator;
    }

    public async Task<PostResponse> Create(CreatePostRequest request, CancellationToken ct = default)
    {
        // Запит з файлами збирає контролер, тож ValidationFilter його не бачить — валідуємо тут.
        await _createValidator.ValidateAndThrowAsync(request, ct);

        // Спершу перевіряємо всі файли, щоб не завантажити половину і впасти на сьомому.
        var extensions = request.Files
            .Select(f => ImageFiles.GetExtensionOrThrow(f.ContentType, f.Length))
            .ToList();

        var authorId = _currentUser.GetCurrentUserId();
        var post = new Post { AuthorId = authorId, Caption = NormalizeCaption(request.Caption) };
        var uploadedUrls = new List<string>();

        try
        {
            for (var i = 0; i < request.Files.Count; i++)
            {
                var file = request.Files[i];
                var path = $"posts/{authorId}/{Guid.NewGuid():N}{extensions[i]}";
                var url = await _fileStorage.UploadAsync(file.Content, path, file.ContentType, ct);

                uploadedUrls.Add(url);
                post.Media.Add(new PostMedia { Url = url, Position = i });
            }

            _postRepository.Add(post);
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch
        {
            // Пост не створився — завантажені фото нікому не потрібні, прибираємо їх зі сховища.
            foreach (var url in uploadedUrls)
            {
                await _fileStorage.DeleteAsync(url, CancellationToken.None);
            }

            throw;
        }

        return await GetVisiblePost(post.Id, authorId, ct);
    }

    public Task<PostResponse> GetById(Guid id, CancellationToken ct = default) =>
        GetVisiblePost(id, _currentUser.GetCurrentUserId(), ct);

    public async Task<PostResponse> Update(Guid id, UpdatePostRequest request, CancellationToken ct = default)
    {
        var viewerId = _currentUser.GetCurrentUserId();
        var post = await GetOwnPost(id, viewerId, ct);

        post.Caption = NormalizeCaption(request.Caption);
        post.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        return await GetVisiblePost(id, viewerId, ct);
    }

    public async Task Delete(Guid id, CancellationToken ct = default)
    {
        var post = await GetOwnPost(id, _currentUser.GetCurrentUserId(), ct);

        // Soft delete: запис лишається в БД з DeletedAt і зникає з усіх запитів (query filter).
        _postRepository.Delete(post);
        await _unitOfWork.SaveChangesAsync(ct);

        // А фото видаляємо по-справжньому: інакше вони відкривалися б за прямим посиланням.
        foreach (var media in post.Media)
        {
            await _fileStorage.DeleteAsync(media.Url, ct);
        }
    }

    public async Task<CursorPage<PostResponse>> GetFeed(GetPostsRequest request, CancellationToken ct = default)
    {
        var viewerId = _currentUser.GetCurrentUserId();
        var posts = await _postRepository.GetFeed(viewerId, request.Cursor?.UtcDateTime, request.Limit + 1, ct);
        return ToPage(posts, request.Limit, viewerId);
    }

    public async Task<CursorPage<PostResponse>> GetUserPosts(
        string username, GetPostsRequest request, CancellationToken ct = default)
    {
        var viewerId = _currentUser.GetCurrentUserId();
        var author = await _userManager.FindByNameAsync(username)
            ?? throw new NotFoundException("Користувача не знайдено.");

        if (!CanView(author, viewerId))
        {
            throw new ForbiddenException(PrivateAccountMessage);
        }

        var posts = await _postRepository.GetByAuthor(author.Id, request.Cursor?.UtcDateTime, request.Limit + 1, ct);
        return ToPage(posts, request.Limit, viewerId);
    }

    private async Task<PostResponse> GetVisiblePost(Guid id, Guid viewerId, CancellationToken ct)
    {
        var post = await _postRepository.GetDetails(id, ct)
            ?? throw new NotFoundException(PostNotFoundMessage);

        if (!CanView(post.Author, viewerId))
        {
            throw new ForbiddenException(PrivateAccountMessage);
        }

        return post.ToResponse(viewerId);
    }

    /// <summary>Пост для зміни/видалення: існує і належить поточному користувачу.</summary>
    private async Task<Post> GetOwnPost(Guid id, Guid viewerId, CancellationToken ct)
    {
        var post = await _postRepository.GetWithMedia(id, ct)
            ?? throw new NotFoundException(PostNotFoundMessage);

        if (post.AuthorId != viewerId)
        {
            throw new ForbiddenException("Змінювати й видаляти можна лише власні пости.");
        }

        return post;
    }

    // Поки немає підписок: приватний акаунт бачить лише власник. З модулем Follow — ще й підписники.
    private static bool CanView(User author, Guid viewerId) => !author.IsPrivate || author.Id == viewerId;

    /// <summary>Репозиторій повертає limit + 1 пост: якщо "зайвий" є — існує наступна сторінка.</summary>
    private static CursorPage<PostResponse> ToPage(List<Post> posts, int limit, Guid viewerId)
    {
        var hasMore = posts.Count > limit;
        var items = posts.Take(limit).Select(p => p.ToResponse(viewerId)).ToList();

        return new CursorPage<PostResponse>
        {
            Items = items,
            NextCursor = hasMore ? items[^1].CreatedAt : null,
        };
    }

    private static string? NormalizeCaption(string? caption) =>
        string.IsNullOrWhiteSpace(caption) ? null : caption.Trim();
}
