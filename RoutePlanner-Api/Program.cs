using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RoutePlanner_Api.Data;
using RoutePlanner_Api.OpenApi;
using RoutePlanner_Api.Services;
using RoutePlanner_Api.Validator;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var jwtConfig = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtConfig["SecretKey"] ?? throw new ArgumentNullException("Jwt Config is empty"));

// Add services to the container.
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

        ValidIssuer = jwtConfig["Issuer"],
        ValidAudience = jwtConfig["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<ApiInfoDocumentTransformer>();
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<AuthorizeOperationTransformer>();
});

builder.Services.AddHttpContextAccessor();

// broker rabbitmq
builder.Services.AddSingleton<IBrokerService, BrokerService>();

builder.Services.AddScoped<VRPConnectionFactory>();
builder.Services.AddScoped<GPSBConnectionFactory>();
builder.Services.AddScoped<UserIdentityService>();
builder.Services.AddScoped<GPSBService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<PrambananValidator>();
builder.Services.AddScoped<ActionLogService>();
builder.Services.AddScoped<PrambananRunService>();
builder.Services.AddScoped<IntegrateService>();
builder.Services.AddScoped<RunService>();

builder.Services.AddHostedService(provider =>
{
    return (BrokerService)provider.GetRequiredService<IBrokerService>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Route Planner API")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
