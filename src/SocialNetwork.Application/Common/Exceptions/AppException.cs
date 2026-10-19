namespace SocialNetwork.Application.Common.Exceptions;

/// <summary>
/// Базовий клас бізнес-помилок. Сервіси кидають нащадків цього класу,
/// а GlobalExceptionHandlingMiddleware перетворює їх на HTTP-відповідь з потрібним статусом.
/// </summary>
public abstract class AppException(string message, IDictionary<string, string[]>? errors = null)
    : Exception(message)
{
    public IDictionary<string, string[]>? Errors { get; } = errors;
}

/// <summary>404 — ресурс не знайдено.</summary>
public class NotFoundException(string message) : AppException(message);

/// <summary>400 — некоректний запит (бізнес-правило порушено).</summary>
public class BadRequestException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(message, errors);

/// <summary>409 — конфлікт (наприклад, username уже зайнятий).</summary>
public class ConflictException(string message, IDictionary<string, string[]>? errors = null)
    : AppException(message, errors);

/// <summary>401 — не автентифікований або невірні облікові дані.</summary>
public class UnauthorizedException(string message) : AppException(message);

/// <summary>403 — автентифікований, але немає прав.</summary>
public class ForbiddenException(string message) : AppException(message);
