using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore.Design;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using LoginService.Model;
using LoginService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LoginContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LoginContext>();
    db.Database.Migrate();
}

app.MapPost("/register", async (User user, LoginContext db) =>
{

    await db.Users.AddAsync(user);
    await db.SaveChangesAsync();

    return Results.Created("/login", "Registered user successfully!");

});

app.MapPost("/login", async (UserLogin userLogin, LoginContext db) =>
{

    User? user = await db.Users.FirstOrDefaultAsync(user => user.email.Equals(userLogin.email) && user.password.Equals(userLogin.password));

    if (user == null)
    {
        return Results.NotFound("The username or password is not correct!");
    }

    var secretkey = builder.Configuration["Jwt:Key"];

    if (secretkey == null)
    {
        return Results.StatusCode(500);
    }

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
        new Claim(ClaimTypes.Email, user.email),
        new Claim(ClaimTypes.GivenName, user.name),
        new Claim(ClaimTypes.Surname, user.name),
        new Claim(ClaimTypes.Role, user.role)
    };

    var token = new JwtSecurityToken
    (
        issuer: builder.Configuration["Jwt:Issuer"],
        audience: builder.Configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(30),
        notBefore: DateTime.UtcNow,
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretkey)), SecurityAlgorithms.HmacSha256)
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(tokenString);


});

app.Run();
