using CloudinaryDotNet;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MiniX.Backend;
using MiniX.Backend.Repositories;
using MiniX.Backend.Services;
using MongoDB.Driver;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- Controllers ---
builder.Services.AddControllers();

// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("MinixPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://minix-front.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// --- Rate Limiting ---
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = builder.Environment.IsDevelopment() ? 5 : 100;
    });
});

// --- MONGO CONFIG ---
var mongoSettings = new MongoDbSettings();
builder.Configuration.GetSection("MongoDB").Bind(mongoSettings);

if (builder.Environment.IsDevelopment())
{
    mongoSettings.ConnectionString = builder.Configuration.GetConnectionString("MongoDBdev")!;
}
else
{
    mongoSettings.ConnectionString = builder.Configuration.GetConnectionString("MongoDB")!;
}

builder.Services.AddSingleton<IMongoClient>(_ =>
{
    return new MongoClient(mongoSettings.ConnectionString);
});

builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoSettings.DatabaseName);
});

// --- Repos & Services ---
var cloudinarySettings = builder.Configuration.GetSection("Cloudinary");
var account = new Account(
    cloudinarySettings["CloudName"],
    cloudinarySettings["ApiKey"],
    cloudinarySettings["ApiSecret"]
);

builder.Services.AddSingleton(new Cloudinary(account));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IFollowRepository, FollowRepository>();
builder.Services.AddScoped<ILikeRepository, LikeRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IImageService, ImageService>();

// --- Auth ---
builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            ),
            RoleClaimType = ClaimTypes.Role
        };
    });

FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromFile("./oauth-minix-firebase-adminsdk-fbsvc-82472f40b1.json"),
});
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthorization();

// Nuevo tipo de auth
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("FirebaseOrDefault", policy =>
    {
        policy.Requirements.Add(new FirebaseAuthorizationRequirement());
    });

    options.AddPolicy("OptionalFirebase", policy =>
    {
        policy.Requirements.Add(new OptionalFirebaseAuthorizationRequirement());
    });
});

builder.Services.AddSingleton<IAuthorizationHandler, FirebaseAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, OptionalFirebaseAuthorizationHandler>();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });

    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MiniX API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Token JWT: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityDefinition("Firebase", new OpenApiSecurityScheme
    {
        Description = "Firebase ID Token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Firebase"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}

app.UseCors("MinixPolicy");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();

app.Run();
