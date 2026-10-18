using System.ComponentModel.DataAnnotations;

namespace SocialNetwork.Api.Dtos;

public class ChangePasswordRequest
{
    [Required]
    public required string CurrentPassword { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public required string NewPassword { get; set; }
}
