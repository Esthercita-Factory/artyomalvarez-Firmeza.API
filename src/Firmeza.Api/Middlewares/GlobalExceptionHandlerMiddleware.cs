using System.Net;
using System.Text.Json;
using Firmeza.Application.Common.Exceptions;
using Firmeza.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Firmeza.Api.Middlewares;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate _next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        this._next = _next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción no controlada detectada en el pipeline HTTP: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title, detail) = exception switch
        {
            NotFoundException notFound => (
                HttpStatusCode.NotFound,
                "Recurso no encontrado",
                notFound.Message
            ),
            ValidationException validation => (
                HttpStatusCode.BadRequest,
                "Error de validación de negocio",
                validation.Message
            ),
            DomainException domain => (
                HttpStatusCode.BadRequest,
                "Inconsistencia en reglas de dominio",
                domain.Message
            ),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "Acceso no autorizado",
                "No tienes permisos para realizar esta operación."
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "Error interno del servidor",
                "Ocurrió un error inesperado al procesar la solicitud."
            )
        };

        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        var json = JsonSerializer.Serialize(problemDetails);
        await context.Response.WriteAsync(json);
    }
}
