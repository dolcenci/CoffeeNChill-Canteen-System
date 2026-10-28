using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using CoffeeNChill.Gateway.Services;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Controllers
builder.Services.AddControllers();

// 2. Configure Swagger with XML Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CoffeeNChill Gateway API",
        Version = "v1",
        Description = "Unified API gateway for CoffeeNChill canteen management system"
    });

    // Include XML documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Add JWT Security Definition
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter 'Bearer' [space] and then your token. Example: Bearer eyJhbGciOi...",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// 3. Configure Azure Table Storage
var storageConnectionString = builder.Configuration["AzureWebJobsStorage"]
    ?? "UseDevelopmentStorage=true";
builder.Services.AddSingleton<CloudStorageAccount>(sp =>
    CloudStorageAccount.Parse(storageConnectionString));

// 4. Register Services
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddHttpClient<AzureFunctionService>();

// 5. Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? "CoffeeNChillSuperSecretKeyForJwtToken2026!@#Minimum32Chars";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CoffeeNChill";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CoffeeNChillClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// 6. Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOrStaff", policy =>
        policy.RequireRole("Admin", "Staff"));
});

// 7. Configure CORS (allow all for development)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 8. Configure Middleware Pipeline
// REMOVED the 'if (IsDevelopment)' check so Swagger works on Render
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CoffeeNChill Gateway API v1");
    c.RoutePrefix = "swagger"; // This ensures it is available at /swagger
});

// REMOVED UseHttpsRedirection because Render handles SSL termination
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();