using Microsoft.AspNetCore.Authorization;
using FirebaseAdmin.Auth;

public class FirebaseAuthorizationHandler : AuthorizationHandler<FirebaseAuthorizationRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FirebaseAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FirebaseAuthorizationRequirement requirement)
    {
        // Si ya está autenticado con el sistema normal, continuar
        if (context.User.Identity?.IsAuthenticated ?? false)
        {
            context.Succeed(requirement);
            return;
        }

        // Si falla, intentar con Firebase
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            context.Fail();
            return;
        }

        var token = ExtractTokenFromHeader(httpContext);
        if (string.IsNullOrEmpty(token))
        {
            context.Fail();
            return;
        }

        try
        {
            // Validar con Firebase
            var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);

            // Agregar el userId de Firebase al contexto
            httpContext.Items["UserId"] = decodedToken.Uid;
            httpContext.Items["FirebaseToken"] = decodedToken;

            context.Succeed(requirement);
        }
        catch (FirebaseAuthException)
        {
            context.Fail();
        }
    }

    private string? ExtractTokenFromHeader(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader)){
            return null;
        }

        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        return null;
    }
}

public class FirebaseAuthorizationRequirement : IAuthorizationRequirement
{
}
