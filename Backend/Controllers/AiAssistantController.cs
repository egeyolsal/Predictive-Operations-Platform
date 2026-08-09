using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Sadece giriş yapmış kullanıcılar soru sorabilsin
    public class AiAssistantController : ControllerBase
    {
        private readonly IAiAssistantService _aiAssistantService;

        public AiAssistantController(IAiAssistantService aiAssistantService)
        {
            _aiAssistantService = aiAssistantService;
        }

        [HttpPost("ask")]
        [EnableRateLimiting("ai-assistant")]
        public async Task<IActionResult> AskQuestion([FromBody] AiRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest("Soru alanı boş bırakılamaz.");
            }

            var answer = await _aiAssistantService.AskAssistantAsync(request.Question);
            
            return Ok(new { Answer = answer });
        }
    }

    // Frontend'den gelecek JSON verisini karşılamak için küçük bir DTO
    public class AiRequestDto
    {
        public string Question { get; set; } = string.Empty;
    }
}