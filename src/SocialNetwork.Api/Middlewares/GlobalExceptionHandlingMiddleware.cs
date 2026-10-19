using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SocialNetwork.Application.Common.Exceptions;
using SocialNetwork.Application.Common.Responses;

namespace SocialNetwork.Api.Middlewares;

/// <summary>
/// Ловить усі винятки й повертає єдиний формат ApiErrorResponse.
/// Завдяки цьому контролери й сервіси не містять try/catch — просто кидають NotFoundException тощо.
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Клієнт закрив з'єднання — відповідати нікому.
        }
        catch (Exception ex)
        {
            await HandleException(context, ex);
        }
    }

    private async Task HandleException(HttpContext context, Exception exception)
    {
        var (statusCode, response) = MapException(exception, context.TraceIdentifier);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Необроблена помилка під час {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(response);
    }

    private (int StatusCode, ApiErrorResponse Response) MapException(Exception exception, string traceId) => exception switch
    {
        ValidationException ex => (StatusCodes.Status400BadRequest, new ApiErrorResponse(
            "Помилка валідації.",
            ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()))),

        NotFoundException ex => (StatusCodes.Status404NotFound, new ApiErrorResponse(ex.Message, ex.Errors)),
        BadRequestException ex => (StatusCodes.Status400BadRequest, new ApiErrorResponse(ex.Message, ex.Errors)),
        ConflictException ex => (StatusCodes.Status409Conflict, new ApiErrorResponse(ex.Message, ex.Errors)),
        UnauthorizedException ex => (StatusCodes.Status401Unauthorized, new ApiErrorResponse(ex.Message)),
        ForbiddenException ex => (StatusCodes.Status403Forbidden, new ApiErrorResponse(ex.Message)),

        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
            new ApiErrorResponse("Дані змінилися паралельним запитом. Повторіть дію.")),

        BadHttpRequestException ex => (ex.StatusCode, new ApiErrorResponse(ex.Message)),

        // Деталі внутрішніх помилок показуємо лише в Development.
        _ => (StatusCodes.Status500InternalServerError, new ApiErrorResponse(
            _environment.IsDevelopment() ? exception.Message : "Внутрішня помилка сервера.",
            TraceId: traceId)),
    };
}
