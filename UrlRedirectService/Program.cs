using Microsoft.EntityFrameworkCore;
using Grpc.Net.Client;
using UrlRedirectService.Protos;

var builder = WebApplication.CreateBuilder(args);

/* Not using database since this is input output only. The UrlContext stil exists in case we decide that a database is needed in the future. */
// builder.Services.AddDbContext<UrlContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGet("/{id}", async (string id) =>
{
    var channel = GrpcChannel.ForAddress("http://urlshortenerapi");
    var client = new GetUrlService.GetUrlServiceClient(channel);

    var urlRequest = new UrlRequest
    {
        Nanoid = id
    };

    var url = await client.GetUrlAsync(urlRequest);

    if (url == null)
    {
        return Results.NotFound("404 not found");
    }
    return Results.Redirect(url.Url);
});

app.Run();
