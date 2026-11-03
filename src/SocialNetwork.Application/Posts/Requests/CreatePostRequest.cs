using SocialNetwork.Application.Common.Files;

namespace SocialNetwork.Application.Posts.Requests;

public class CreatePostRequest
{
    public string? Caption { get; init; }

    /// <summary>Фото в порядку каруселі.</summary>
    public required IReadOnlyList<FileUpload> Files { get; init; }
}
