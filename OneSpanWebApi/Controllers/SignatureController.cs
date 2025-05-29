using Microsoft.AspNetCore.Mvc;
using OneSpanWebApi.Services;

namespace OneSpanWebApi.Controllers
{
    public class SignatureController : ControllerBase
    {
        private readonly OneSpanService _oneSpanService;

        public SignatureController(OneSpanService oneSpanService)
        {
            _oneSpanService = oneSpanService;
        }

        [HttpGet("GetSignature")]
        public IActionResult GetSignature()
        {
            try
            {
                var signature = _oneSpanService.GetSignature();
                return Ok(signature);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
