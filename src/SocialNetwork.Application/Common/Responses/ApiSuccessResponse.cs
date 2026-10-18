namespace SocialNetwork.Application.Common.Responses;

/// <summary>Успішна відповідь без даних: { "success": true, "message": "..." }.</summary>
public record ApiSuccessResponse(string Message)
{
    public bool Success => true;
}

/// <summary>Успішна відповідь з даними: { "success": true, "message": "...", "data": {...} }.</summary>
public record ApiSuccessResponse<T>(T Data, string Message = "OK") : ApiSuccessResponse(Message);
