using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneSpanWebApi.Models;
using OneSpanWebApi.Services;
using Microsoft.Extensions.Logging;

namespace OneSpanWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class SignatureController : ControllerBase
    {
        private readonly OneSpanService _oneSpanService;
        private readonly ILogger<OneSpanWebhookController> _logger;

        public SignatureController(OneSpanService oneSpanService, ILogger<OneSpanWebhookController> logger)
        {
            _oneSpanService = oneSpanService;
            _logger = logger;
        }

        [HttpGet("GetDesignationSignature")]
        public IActionResult GetDesignationSignature(BeneficiaryRequest beneficiaryRequest)
        {
            try
            {
                _logger.LogInformation("Received GetSignature request.");
                if (beneficiaryRequest == null)
                {
                    return BadRequest("Beneficiary request cannot be null.");
                }
                if (string.IsNullOrEmpty(beneficiaryRequest.SignerEmail) ||
                    string.IsNullOrEmpty(beneficiaryRequest.SignerFirstName) ||
                    string.IsNullOrEmpty(beneficiaryRequest.SignerLastName) ||
                    string.IsNullOrEmpty(beneficiaryRequest.SignerDateOfBirth) ||
                    string.IsNullOrEmpty(beneficiaryRequest.SignerLast4SSN) ||
                    string.IsNullOrEmpty(beneficiaryRequest.DesignationId) ||
                    string.IsNullOrEmpty(beneficiaryRequest.CN))
                {
                    return BadRequest("All fields in BeneficiaryRequest must be provided.");
                }
                _logger.LogInformation($"Processing request for signer: {beneficiaryRequest.SignerFirstName} {beneficiaryRequest.SignerLastName}");

                var packageid = _oneSpanService.GetDesignationSignature(beneficiaryRequest);

                if (packageid == null)
                {
                    _logger.LogWarning("Signature package Id is null.");
                    return NotFound("Signature package not found.");
                }
                else
                {
                    _logger.LogInformation($"Signature package ID generated: {packageid}");
                }

                return Ok(packageid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging request to get signature.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("cancel/{designationId}")]
        public async Task<IActionResult> CancelPackage(string designationId)
        {
            try
            {
                _logger.LogInformation($"Received request to cancel package with designationId: {designationId}");

                if (string.IsNullOrEmpty(designationId))
                {
                    return BadRequest("Designation ID cannot be null or empty.");
                }
                // Call the service to cancel the package
                await _oneSpanService.CancelPackageAsync(designationId);
                // Log the successful cancellation
                _logger.LogInformation($"Package with designationId {designationId} has been canceled successfully.");
                // Return a success response
                return Ok(new { message = "Package canceled." });
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error logging request to cancel package.");
                // Return an error response
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
           
        }
    }
}
