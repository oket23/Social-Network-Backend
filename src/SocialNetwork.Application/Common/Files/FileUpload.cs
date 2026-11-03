namespace SocialNetwork.Application.Common.Files;

/// <summary>
/// Файл із запиту. Application не знає про IFormFile (це ASP.NET Core) —
/// контролер перекладає IFormFile у цей record.
/// </summary>
public record FileUpload(Stream Content, string ContentType, long Length);
