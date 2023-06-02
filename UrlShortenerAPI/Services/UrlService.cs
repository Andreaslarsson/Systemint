using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using UrlShortenerAPI.Protos;

namespace UrlShortenerAPI.Services
{

	// En anpassad tj�nstklass f�r URL-tj�nsten som implementerar gRPC-gr�nssnittet GetUrlService.GetUrlServiceBase
	public class UrlService : GetUrlService.GetUrlServiceBase
	{
		private readonly ILogger<UrlService> _logger;
		private readonly UrlContext _db;

		// Konstruktor som tar emot en logger och en URL Context
		public UrlService(ILogger<UrlService> logger, UrlContext db)
		{
			_logger = logger;
			_db = db;
		}

		// Implementering av gRPC-metoden GetUrl
		public override async Task<UrlResponse> GetUrl(UrlRequest request, ServerCallContext context)
		{
			// Skapa ett URLResponse-objekt
			var response = new UrlResponse();

			// H�mta ID fr�n request
			var id = request.Nanoid;

			// H�mta URL-objekt fr�n databasen baserat p� ID
			var urlObj = await _db.URLs.Where(x => x.nanoid == id).FirstOrDefaultAsync();

			// Kontrollera om URL-objektet och URL:n �r giltiga
			if (urlObj != null && urlObj.url != null)
			{
				// Fyll i URLResponse-objektet med data fr�n URL-objektet
				response.Id = urlObj.id;
				response.Nanoid = urlObj.nanoid;
				response.Url = urlObj.url;
			}

			// Returnera URLResponse-objektet som ett resultat
			return await Task.FromResult(response);
		}
	}


}