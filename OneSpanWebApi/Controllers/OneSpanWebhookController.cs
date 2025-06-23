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
using Microsoft.Extensions.Logging;
using System.Diagnostics.Eventing.Reader;

namespace OneSpanWebApi.Controllers
{
    [Route("api/webhook/onespan")]
    [ApiController]
    public class OneSpanWebhookController : ControllerBase
    {
        private readonly OneSpanService _oneSpanService;
        private readonly string _authKey;
        private readonly ILogger<OneSpanWebhookController> _logger;
       
        public OneSpanWebhookController(OneSpanService oneSpanService, IOptions<OneSpanConfig> options, ILogger<OneSpanWebhookController> logger)//, DocumentStorageService storage)
        {
            _oneSpanService = oneSpanService;
            var config = options.Value;
            _authKey = config.CallbackKey ?? string.Empty;
            _logger = logger;
        }

        [HttpPost("sendsigneddoc")]
        public async Task<IActionResult> SendDoc(object docPayload)
        {
            try
            {
                _logger.LogInformation("Received webhook request from OneSpan.");

                if (Request.Headers.ContainsKey("Authorization") && (Request.Headers["Authorization"].ToString() == _authKey))
                {
                    if (docPayload != null)
                    {
                        string? payloadString = docPayload?.ToString();
                        if (!string.IsNullOrEmpty(payloadString))
                        {
                            JObject payloadJason = JObject.Parse(payloadString);
                            string? eventName = payloadJason["name"]?.ToString();
                            string? sessionUser = payloadJason["sessionUser"]?.ToString();
                            string? packageId = payloadJason["packageId"]?.ToString();
                            string? message = payloadJason["message"]?.ToString();
                            string? documentId = payloadJason["documentId"]?.ToString();
                            string? createdDate = payloadJason["createdDate"]?.ToString();

                            _logger.LogInformation($"Received signed document from OneSpan: event={eventName}, packageId={packageId}, documentId={documentId}");

                            if (!string.IsNullOrEmpty(packageId) && eventName?.ToUpper() == "DOCUMENT_SIGNED" && !string.IsNullOrEmpty(documentId) && documentId.ToLower() != "default-consent")
                            {
                                try
                                {   // Download the signed document
                                    string path = await _oneSpanService.DownloadSignedDocumentAsync(packageId, documentId);
                                    _logger.LogInformation($"Signed document downloaded successfully: PackageId: {packageId}");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Error downloading signed document for packageId={packageId}, documentId={documentId}");
                                    return StatusCode((int)HttpStatusCode.InternalServerError, "Error processing signed document");
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Received empty payload from OneSpan.");
                            return BadRequest("Empty payload received");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Received null payload from OneSpan.");
                        return BadRequest("Null payload received");
                    }

                    return Ok();
                }
                else
                {
                    _logger.LogWarning("Unauthorized OneSpan webhook request.");
                    return Unauthorized();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing OneSpan webhook request.");
                return StatusCode((int)HttpStatusCode.InternalServerError, "Internal server error");
            }

        }
    }
}
