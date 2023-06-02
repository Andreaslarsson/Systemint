using Grpc.Net.Client;
using UrlRedirectService.Protos;

var builder = WebApplication.CreateBuilder(args);


// L�gg till gRPC-tj�nsten i byggaren
builder.Services.AddGrpc();

// Bygg WebApplication-objektet
var app = builder.Build();

// Konfigurera en GET-rutt f�r att h�mta en URL baserat p� ID
app.MapGet("/{id}", async (string id) =>
{
	// Skapa en gRPC-kanal f�r att ansluta till URL-f�rkortningstj�nsten
	var channel = GrpcChannel.ForAddress("http://urlshortenerapi");
	var client = new GetUrlService.GetUrlServiceClient(channel);

	// Skapa en URL-beg�ran med det angivna ID:et
	var urlRequest = new UrlRequest
	{
		Nanoid = id
	};

	// Skicka beg�ran till URL-f�rkortningstj�nsten och f� tillbaka URL-objektet
	var url = await client.GetUrlAsync(urlRequest);

	// Kontrollera om URL-objektet �r tomt
	if (url == null)
	{
		// Returnera ett svar som indikerar att sidan inte hittades (404)
		return Results.NotFound("404 not found");
	}

	// Returnera ett omdirigeringsresultat till den ursprungliga URL:en
	return Results.Redirect(url.Url);
});

// Starta webbapplikationen
app.Run();
