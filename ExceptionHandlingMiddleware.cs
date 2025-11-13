using Amazon.Runtime.Internal;
using System.Text.Json;

namespace MiniX.Backend
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment environment)
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
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                await WriteErrorAsync(context, 401, "Unauthorized",
                    "No tiene permisos para acceder a este recurso");
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Recurso no encontrado");
                await WriteErrorAsync(context, 404, "NotFound",
                    "El recurso solicitado no fue encontrado");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argumento inválido");
                var message = _environment.IsDevelopment() ? ex.Message : "Solicitud inválida";
                await WriteErrorAsync(context, 400, "InvalidArgument", message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operación inválida");
                var message = _environment.IsDevelopment() ? ex.Message : "La operación no puede completarse";
                await WriteErrorAsync(context, 422, "InvalidOperation", message);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Solicitud cancelada por el cliente");
                context.Response.StatusCode = 499;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operación cancelada");
                context.Response.StatusCode = 499;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no manejado en la aplicación");
                await WriteErrorAsync(context, 500, "InternalServerError",
                    "Ha ocurrido un error interno en el servidor");
            }
        }

        private static async Task WriteErrorAsync(HttpContext context, int statusCode, string code, string message)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    code,
                    message,
                    traceId = context.TraceIdentifier                    
                });
            }
        }
    }
}