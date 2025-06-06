using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client.Extensions.Msal;
using Newtonsoft.Json.Linq;
using OneSpanWebApi.Services;
using OneSpanWebApi.Webhooks;
using System.Net;
using OneSpanSign.Sdk;
using Microsoft.Extensions.Options;
using OneSpanWebApi.Models;

namespace OneSpanWebApi.Controllers
{
    [Route("api/webhook/onespan")]
    [ApiController]
    public class OneSpanWebhookController : ControllerBase
    {
        private readonly OneSpanService _oneSpanService;
        private readonly string _authKey;
        //private readonly DocumentStorageService _storage;

        public OneSpanWebhookController(OneSpanService oneSpanService, IOptions<OneSpanOptions> options)//, DocumentStorageService storage)
        {
            _oneSpanService = oneSpanService;
            var config = options.Value;
            _authKey = config.CallbackKey ?? string.Empty;
            //_storage = storage;
        }

        [HttpPost("sendsigneddoc")]
        public async Task<IActionResult> SendDoc(object item)
        {
            if (Request.Headers.ContainsKey("Authorization") && (Request.Headers["Authorization"].ToString() == _authKey))
            {
                if (item != null)
                {
                    JObject docInfo = JObject.Parse(item.ToString());
                    string? eventName = docInfo["name"]?.ToString();
                    string? sessionUser = docInfo["sessionUser"]?.ToString();
                    string? packageId = docInfo["packageId"]?.ToString();
                    string? message = docInfo["message"]?.ToString();
                    string? documentId = docInfo["documentId"]?.ToString();
                    string? createdDate = docInfo["createdDate"]?.ToString();

                    if (!string.IsNullOrEmpty(packageId) && eventName?.ToUpper() == "DOCUMENT_SIGNED" && !string.IsNullOrEmpty(documentId) && documentId.ToLower() != "default-consent")
                    {
                        string path = await _oneSpanService.DownloadSignedDocumentAsync(packageId, documentId);
                    }
                }
                return Ok();
            }
            else
            {
                return Unauthorized();
            }
        }

    }
}
