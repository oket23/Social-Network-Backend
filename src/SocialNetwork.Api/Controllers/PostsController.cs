using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetwork.Application.Common.Files;
using SocialNetwork.Application.Common.Responses;
using SocialNetwork.Application.Posts;
using SocialNetwork.Application.Posts.Requests;
using SocialNetwork.Application.Posts.Responses;

namespace SocialNetwork.Api.Controllers;

[ApiController]
[Authorize]
[Route("posts")]
[Produces("application/json")]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;

    public PostsController(IPostService postService)
    {
        _postService = postService;
    }

    /// <summary>Новий пост (multipart/form-data): поле "caption" і 1–10 полів "files" (JPEG/PNG/WEBP до 5 МБ кожне).</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(PostService.MaxMediaCount * ImageFiles.MaxSizeBytes + 1024 * 1024)]
    [ProducesResponseType<ApiSuccessResponse<PostResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromForm] string? caption, [FromForm] List<IFormFile> files, CancellationToken ct)
    {
        var request = new CreatePostRequest
        {
            Caption = caption,
            Files = files.Select(f => new FileUpload(f.OpenReadStream(), f.ContentType, f.Length)).ToList(),
        };

        var post = await _postService.Create(request, ct);
        return Ok(new ApiSuccessResponse<PostResponse>(post, "Пост опубліковано."));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ApiSuccessResponse<PostResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var post = await _postService.GetById(id, ct);
        return Ok(new ApiSuccessResponse<PostResponse>(post));
    }

    /// <summary>Змінити підпис. Лише автор поста.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ApiSuccessResponse<PostResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdatePostRequest request, CancellationToken ct)
    {
        var post = await _postService.Update(id, request, ct);
        return Ok(new ApiSuccessResponse<PostResponse>(post, "Пост оновлено."));
    }

    /// <summary>Видалити пост. Лише автор поста.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType<ApiSuccessResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _postService.Delete(id, ct);
        return Ok(new ApiSuccessResponse("Пост видалено."));
    }

    /// <summary>Стрічка, новіші зверху. Наступна сторінка — ?cursor={nextCursor}.</summary>
    [HttpGet("/feed")]
    [ProducesResponseType<ApiSuccessResponse<CursorPage<PostResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFeed([FromQuery] GetPostsRequest request, CancellationToken ct)
    {
        var page = await _postService.GetFeed(request, ct);
        return Ok(new ApiSuccessResponse<CursorPage<PostResponse>>(page));
    }

    /// <summary>Пости користувача (сітка в профілі). Наступна сторінка — ?cursor={nextCursor}.</summary>
    [HttpGet("/users/{username}/posts")]
    [ProducesResponseType<ApiSuccessResponse<CursorPage<PostResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserPosts(string username, [FromQuery] GetPostsRequest request, CancellationToken ct)
    {
        var page = await _postService.GetUserPosts(username, request, ct);
        return Ok(new ApiSuccessResponse<CursorPage<PostResponse>>(page));
    }
}
