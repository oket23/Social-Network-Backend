using System.Text.Json.Serialization;

namespace SocialNetwork.Application.Common.Responses;

/// <summary>
/// Відповідь з помилкою: { "success": false, "message": "...", "errors": { "Username": ["..."] } }.
/// Errors є лише для помилок валідації (ключ — назва поля), TraceId — лише для 500.
/// </summary>
public record ApiErrorResponse(
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IDictionary<string, string[]>? Errors = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? TraceId = null)
{
    public bool Success => false;
}
