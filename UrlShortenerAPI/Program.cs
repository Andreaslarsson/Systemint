using Microsoft.EntityFrameworkCore;
using UrlShortenerAPI.Services;
using Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using System.Net.Sockets;
using Microsoft.AspNetCore.Authorization;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<UrlContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var SecretToken = builder.Configuration["Jwt:Key"] ?? throw new Exception("No JWT Key");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateActor = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ClockSkew = TimeSpan.Zero,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretToken))
    };

});

// GRPC
builder.Services.AddGrpc();

builder.Services.AddAuthorization();

var app = builder.Build();

// Apply Migrations 
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UrlContext>();
    db.Database.Migrate();
    Console.WriteLine("Migrations Applied");
}

app.UseAuthentication();
app.UseAuthorization();
// GRPC
app.MapGrpcService<UrlService>();


string HOSTNAME = builder.Configuration.GetSection("host_url").Value ?? "http://localhost/";

URLshort shorter = new();


app.MapPost("/shorten", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] async (URL? url, UrlContext db) =>
{
    if (url == null || url.url == null)
    {
        return Results.BadRequest();
    }

    var shortURL = await shorter.shorten(url.url, HOSTNAME, db);
    return Results.Ok(shortURL);

});

app.Run();
