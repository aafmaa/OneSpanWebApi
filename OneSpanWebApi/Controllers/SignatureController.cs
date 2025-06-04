using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneSpanWebApi.Models;
using OneSpanWebApi.Services;

namespace OneSpanWebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class SignatureController : ControllerBase
    {
        private readonly OneSpanService _oneSpanService;

        public SignatureController(OneSpanService oneSpanService)
        {
            _oneSpanService = oneSpanService;
        }

        [HttpGet("GetSignature")]
        public IActionResult GetSignature(BeneficiaryRequest beneficiaryRequest)
        {
            try
            {
                var signature = _oneSpanService.GetSignature(beneficiaryRequest);
                return Ok(signature);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("cancel/{designationId}")]
        public async Task<IActionResult> CancelPackage(string designationId)
        {
            await _oneSpanService.CancelPackageAsync(designationId);
            return Ok(new { message = "Package canceled." });
        }
    }
}
