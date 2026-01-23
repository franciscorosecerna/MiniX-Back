using Microsoft.AspNetCore.Authorization;
using FirebaseAdmin.Auth;

public class OptionalFirebaseAuthorizationHandler : AuthorizationHandler<OptionalFirebaseAuthorizationRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OptionalFirebaseAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OptionalFirebaseAuthorizationRequirement requirement)
    {
        // Si ya está autenticado con el sistema normal, continuar
        if (context.User.Identity?.IsAuthenticated ?? false)
        {
            context.Succeed(requirement);
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            // En modo opcional, si no hay contexto HTTP, permitir acceso anónimo
            context.Succeed(requirement);
            return;
        }

        var token = ExtractTokenFromHeader(httpContext);

        // Si NO hay token, permitir acceso anónimo
        if (string.IsNullOrEmpty(token))
        {
            context.Succeed(requirement);
            return;
        }

        // Si HAY token, validarlo
        try
        {
            var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
            httpContext.Items["UserId"] = decodedToken.Uid;
            httpContext.Items["FirebaseToken"] = decodedToken;
            context.Succeed(requirement);
        }
        catch (FirebaseAuthException)
        {
            // Si el token es inválido, FALLAR (no permitir acceso con token malo)
            context.Fail();
        }
    }

    private string? ExtractTokenFromHeader(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader))
        {
            return null;
        }

        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        return null;
    }
}

public class OptionalFirebaseAuthorizationRequirement : IAuthorizationRequirement
{
}
