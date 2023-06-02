using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Model;
using System.Text;
using UrlShortenerAPI.Services;

// Skapa en WebApplicationBuilder med de angivna argumenten
var builder = WebApplication.CreateBuilder(args);

// L�gg till en Database Context i tj�nsterna med r�tt anslutningsstr�ng
builder.Services.AddDbContext<UrlContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// H�mta den hemliga nyckeln f�r JWT fr�n konfigurationen, kasta ett undantag om den saknas
var secretToken = builder.Configuration["Jwt:Key"] ?? throw new Exception("No JWT Key");

// L�gg till JWT-autentisering i tj�nsterna
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
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretToken))
	};
});

// L�gg till gRPC-tj�nsten i tj�nsterna
builder.Services.AddGrpc();

// L�gg till beh�righetshantering i tj�nsterna
builder.Services.AddAuthorization();

// Bygg WebApplication-objektet
var app = builder.Build();

// Utf�r databasmigreringar
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<UrlContext>();
	db.Database.Migrate();
	Console.WriteLine("Migrations Applied");
}

// Anv�nd autentisering och auktorisering i appen
app.UseAuthentication();
app.UseAuthorization();

// Konfigurera en gRPC-rutt f�r URL-tj�nsten
app.MapGrpcService<UrlService>();

// H�mta v�rden fr�n konfigurationen f�r host-url
string hostUrl = builder.Configuration.GetSection("host_url").Value ?? "http://localhost/";

// Skapa ett objekt f�r URL-f�rkortning
URLshort shorter = new URLshort();

// Konfigurera en POST-rutt f�r att f�rkorta en URL
app.MapPost("/shorten", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] async (URL? url, UrlContext db) =>
{
	// Kontrollera om URL-objektet eller URL:n �r tomma
	if (url == null || url.url == null)
	{
		// Returnera ett felmeddelande vid ogiltig beg�ran
		return Results.BadRequest();
	}

	// F�rkorta URL:n
	var shortURL = await shorter.shorten(url.url, hostUrl, db);

	// Returnera det f�rkortade URL-svaret
	return Results.Ok(shortURL);
});

// Starta webbapplikationen
app.Run();

