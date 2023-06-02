using Grpc.Net.Client;
using UrlRedirectService.Protos;

var builder = WebApplication.CreateBuilder(args);


// Lägg till gRPC-tjänsten i byggaren
builder.Services.AddGrpc();

// Bygg WebApplication-objektet
var app = builder.Build();

// Konfigurera en GET-rutt för att hämta en URL baserat på ID
app.MapGet("/{id}", async (string id) =>
{
	// Skapa en gRPC-kanal för att ansluta till URL-förkortningstjänsten
	var channel = GrpcChannel.ForAddress("http://urlshortenerapi");
	var client = new GetUrlService.GetUrlServiceClient(channel);

	// Skapa en URL-begäran med det angivna ID:et
	var urlRequest = new UrlRequest
	{
		Nanoid = id
	};

	// Skicka begäran till URL-förkortningstjänsten och få tillbaka URL-objektet
	var url = await client.GetUrlAsync(urlRequest);

	// Kontrollera om URL-objektet är tomt
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
