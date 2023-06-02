using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Model;
using System.Text;
using UrlShortenerAPI.Services;

// Skapa en WebApplicationBuilder med de angivna argumenten
var builder = WebApplication.CreateBuilder(args);

// Lägg till en Database Context i tjänsterna med rätt anslutningssträng
builder.Services.AddDbContext<UrlContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Hämta den hemliga nyckeln för JWT från konfigurationen, kasta ett undantag om den saknas
var secretToken = builder.Configuration["Jwt:Key"] ?? throw new Exception("No JWT Key");

// Lägg till JWT-autentisering i tjänsterna
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

// Lägg till gRPC-tjänsten i tjänsterna
builder.Services.AddGrpc();

// Lägg till behörighetshantering i tjänsterna
builder.Services.AddAuthorization();

// Bygg WebApplication-objektet
var app = builder.Build();

// Utför databasmigreringar
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<UrlContext>();
	db.Database.Migrate();
	Console.WriteLine("Migrations Applied");
}

// Använd autentisering och auktorisering i appen
app.UseAuthentication();
app.UseAuthorization();

// Konfigurera en gRPC-rutt för URL-tjänsten
app.MapGrpcService<UrlService>();

// Hämta värden från konfigurationen för host-url
string hostUrl = builder.Configuration.GetSection("host_url").Value ?? "http://localhost/";

// Skapa ett objekt för URL-förkortning
URLshort shorter = new URLshort();

// Konfigurera en POST-rutt för att förkorta en URL
app.MapPost("/shorten", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] async (URL? url, UrlContext db) =>
{
	// Kontrollera om URL-objektet eller URL:n är tomma
	if (url == null || url.url == null)
	{
		// Returnera ett felmeddelande vid ogiltig begäran
		return Results.BadRequest();
	}

	// Förkorta URL:n
	var shortURL = await shorter.shorten(url.url, hostUrl, db);

	// Returnera det förkortade URL-svaret
	return Results.Ok(shortURL);
});

// Starta webbapplikationen
app.Run();

