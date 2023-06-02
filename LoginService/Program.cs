using LoginService;
using LoginService.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


// Skapa en WebApplicationBuilder med de angivna argumenten
var builder = WebApplication.CreateBuilder(args);

// L�gg till en Database Context i tj�nsterna med r�tt anslutningsstr�ng
builder.Services.AddDbContext<LoginContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Bygg WebApplication-objektet
var app = builder.Build();

// Skapa en omfattning (scope) och h�mta LoginContext fr�n tj�nsterna
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<LoginContext>();

	// Utf�r eventuella databasmigreringar
	db.Database.Migrate();
}

// Konfigurera en POST-rutt f�r registrering av anv�ndare
app.MapPost("/register", async (User user, LoginContext db) =>
{
	// L�gg till anv�ndaren i databasen
	await db.Users.AddAsync(user);
	await db.SaveChangesAsync();

	// Returnera ett svar som indikerar att anv�ndaren har registrerats framg�ngsrikt
	return Results.Created("/login", "Anv�ndaren registrerades framg�ngsrikt!");
});

// Konfigurera en POST-rutt f�r inloggning
app.MapPost("/login", async (UserLogin userLogin, LoginContext db) =>
{
	// H�mta anv�ndaren fr�n databasen baserat p� e-post och l�senord
	User? user = await db.Users.FirstOrDefaultAsync(u => u.email.Equals(userLogin.email) && u.password.Equals(userLogin.password));

	// Kontrollera om anv�ndaren inte hittades
	if (user == null)
	{
		// Returnera ett svar som indikerar att anv�ndarnamnet eller l�senordet �r felaktigt
		return Results.NotFound("Anv�ndarnamnet eller l�senordet �r inte korrekt!");
	}

	// H�mta den hemliga nyckeln f�r JWT-generering fr�n konfigurationen
	var secretKey = builder.Configuration["Jwt:Key"];

	// Kontrollera om den hemliga nyckeln inte �r konfigurerad
	if (secretKey == null)
	{
		// Returnera ett svar med statuskod 500 (Intern serverfel)
		return Results.StatusCode(500);
	}

	// Skapa en lista med claims f�r JWT
	var claims = new[]
	{
		new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
		new Claim(ClaimTypes.Email, user.email),
		new Claim(ClaimTypes.GivenName, user.name),
		new Claim(ClaimTypes.Surname, user.name),
		new Claim(ClaimTypes.Role, user.role)
	};

	// Skapa JWT-token med r�tt parametrar
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
