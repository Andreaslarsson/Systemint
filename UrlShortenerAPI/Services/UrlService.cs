using Grpc.Core;
using UrlShortenerAPI.Protos;
using Microsoft.EntityFrameworkCore;

namespace UrlShortenerAPI.Services
{

    public class UrlService : GetUrlService.GetUrlServiceBase
    {

        private readonly ILogger<UrlService> _logger;
        private readonly UrlContext _db;

        public UrlService(ILogger<UrlService> logger, UrlContext db)
        {
            _logger = logger;
            _db = db;
        }


        public override async Task<UrlResponse> GetUrl(UrlRequest request, ServerCallContext context)
        {

            var response = new UrlResponse();

            var id = request.Nanoid;

            var UrlObj = await _db.URLs.Where(x => x.nanoid == id).FirstOrDefaultAsync();

            if (UrlObj != null && UrlObj.url != null)
            {
                response.Id = UrlObj.id;
                response.Nanoid = UrlObj.nanoid;
                response.Url = UrlObj.url;
            }

            return await Task.FromResult(response);
        }

    }

}