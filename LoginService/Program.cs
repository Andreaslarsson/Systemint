using LoginService;
using LoginService.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


// Skapa en WebApplicationBuilder med de angivna argumenten
var builder = WebApplication.CreateBuilder(args);

// Lägg till en Database Context i tjänsterna med rätt anslutningssträng
builder.Services.AddDbContext<LoginContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Bygg WebApplication-objektet
var app = builder.Build();

// Skapa en omfattning (scope) och hämta LoginContext från tjänsterna
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<LoginContext>();

	// Utför eventuella databasmigreringar
	db.Database.Migrate();
}

// Konfigurera en POST-rutt för registrering av användare
app.MapPost("/register", async (User user, LoginContext db) =>
{
	// Lägg till användaren i databasen
	await db.Users.AddAsync(user);
	await db.SaveChangesAsync();

	// Returnera ett svar som indikerar att användaren har registrerats framgångsrikt
	return Results.Created("/login", "Användaren registrerades framgångsrikt!");
});

// Konfigurera en POST-rutt för inloggning
app.MapPost("/login", async (UserLogin userLogin, LoginContext db) =>
{
	// Hämta användaren från databasen baserat på e-post och lösenord
	User? user = await db.Users.FirstOrDefaultAsync(u => u.email.Equals(userLogin.email) && u.password.Equals(userLogin.password));

	// Kontrollera om användaren inte hittades
	if (user == null)
	{
		// Returnera ett svar som indikerar att användarnamnet eller lösenordet är felaktigt
		return Results.NotFound("Användarnamnet eller lösenordet är inte korrekt!");
	}

	// Hämta den hemliga nyckeln för JWT-generering från konfigurationen
	var secretKey = builder.Configuration["Jwt:Key"];

	// Kontrollera om den hemliga nyckeln inte är konfigurerad
	if (secretKey == null)
	{
		// Returnera ett svar med statuskod 500 (Intern serverfel)
		return Results.StatusCode(500);
	}

	// Skapa en lista med claims för JWT
	var claims = new[]
	{
		new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
		new Claim(ClaimTypes.Email, user.email),
		new Claim(ClaimTypes.GivenName, user.name),
		new Claim(ClaimTypes.Surname, user.name),
		new Claim(ClaimTypes.Role, user.role)
	};

	// Skapa JWT-token med rätt parametrar
	var token = new JwtSecurityToken
	   (
		   issuer: builder.Configuration["Jwt:Issuer"],
		   audience: builder.Configuration["Jwt:Audience"],
		   claims: claims,
		   expires: DateTime.UtcNow.AddMinutes(30),
		   notBefore: DateTime.UtcNow,
		   signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)), SecurityAlgorithms.HmacSha256)
	   );

	var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

	return Results.Ok(tokenString);
});



app.Run();
