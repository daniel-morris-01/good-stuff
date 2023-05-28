using Microsoft.Net.Http.Headers;
using NLog.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;
using Aims.Services.JWT;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using $safeprojectname$.EF;
using $safeprojectname$.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(SwaggerSetup);
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContextFactory<$safeprojectname$Context>();


builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        builder => builder.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .WithHeaders(HeaderNames.ContentType, HeaderNames.AccessControlAllowOrigin));
});
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.IncludeErrorDetails = true;

    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = builder.Configuration["AimsAuth_JWT:Audience"],
        ValidIssuer = builder.Configuration["AimsAuth_JWT:Issuer"],
        IssuerSigningKey = JWTRsaSecurityKeyProvider.GetPublicKey("AimsAuth_JWT", builder.Configuration)
    };
});

builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
builder.Host.UseNLog();

builder.Services.AddSingleton<$safeprojectname$.Services.$safeprojectname$Main> ();


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error-local-development");
}
else
{
    app.UseExceptionHandler("/error");
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

void SwaggerSetup(SwaggerGenOptions options)
{
    OpenApiSecurityScheme securityDefinition = new OpenApiSecurityScheme()
    {
        Name = "Bearer",
        BearerFormat = "JWT",
        Scheme = "bearer",
        Description = "Specify the authorization token.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
    };
    options.AddSecurityDefinition("jwt_auth", securityDefinition);

    // Make sure swagger UI requires a Bearer token specified
    OpenApiSecurityScheme securityScheme = new OpenApiSecurityScheme()
    {
        Reference = new OpenApiReference()
        {
            Id = "jwt_auth",
            Type = ReferenceType.SecurityScheme
        }
    };
    OpenApiSecurityRequirement securityRequirements = new OpenApiSecurityRequirement()
            {
                {securityScheme, new string[] { } }
            };
    options.AddSecurityRequirement(securityRequirements);
}