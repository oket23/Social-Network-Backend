using System.ComponentModel.DataAnnotations;

namespace SocialNetwork.Api.Dtos;

public class UpdateUserProfileRequest
{
    [Required]
    [StringLength(30, MinimumLength = 3)]
    [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "Username може містити лише латинські літери, цифри та підкреслення.")]
    public required string Username { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public required string DisplayName { get; set; }

    [StringLength(160)]
    public string? Bio { get; set; }

    [StringLength(200)]
    [Url(ErrorMessage = "WebsiteUrl має бути коректним URL.")]
    public string? WebsiteUrl { get; set; }

    public bool IsPrivate { get; set; }
}
