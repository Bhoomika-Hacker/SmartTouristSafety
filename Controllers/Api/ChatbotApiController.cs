using Microsoft.AspNetCore.Mvc;
using SmartTouristSafety.Models.ViewModels;
using SmartTouristSafety.Services;

namespace SmartTouristSafety.Controllers.Api
{
    [ApiController]
    [Route("api/chatbot")]
    public class ChatbotApiController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;

        public ChatbotApiController(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        // POST api/chatbot/ask
        [HttpPost("ask")]
        public async Task<ActionResult<ChatResponseDto>> Ask(ChatRequestDto request)
        {
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Tourist"))
            {
                var claim = User.FindFirst("TouristId");
                if (claim is not null && int.TryParse(claim.Value, out var ownTouristId))
                {
                    request.TouristId = ownTouristId; // always answer using the signed-in tourist's own context
                }
            }

            return Ok(await _chatbotService.GetResponseAsync(request));
        }
    }
}
