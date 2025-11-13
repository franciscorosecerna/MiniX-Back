namespace MiniX.Backend
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
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
                await WriteErrorAsync(context, 403, "UnauthorizedAccess", ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argumento inválido");
                await WriteErrorAsync(context, 400, "InvalidArgument", ex.Message);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Solicitud cancelada por el cliente");
                context.Response.StatusCode = 499;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no manejado");
                await WriteErrorAsync(context, 500, "InternalServerError", "Error interno del servidor");
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
