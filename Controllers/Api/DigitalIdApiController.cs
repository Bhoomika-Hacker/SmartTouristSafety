using Microsoft.AspNetCore.Mvc;
using SmartTouristSafety.Services;

namespace SmartTouristSafety.Controllers.Api
{
    [ApiController]
    [Route("api/digitalid")]
    public class DigitalIdApiController : ControllerBase
    {
        private readonly IBlockchainService _blockchainService;

        public DigitalIdApiController(IBlockchainService blockchainService)
        {
            _blockchainService = blockchainService;
        }

        // GET api/digitalid/verify/5
        [HttpGet("verify/{touristId:int}")]
        public IActionResult Verify(int touristId)
        {
            var result = _blockchainService.VerifyDigitalId(touristId);
            return Ok(result);
        }

        // GET api/digitalid/verify-chain  -> integrity check of the entire ledger
        [HttpGet("verify-chain")]
        public IActionResult VerifyChain()
        {
            var intact = _blockchainService.VerifyEntireChain();
            return Ok(new { chainIntact = intact });
        }

        // POST api/digitalid/issue/5?days=30
        [HttpPost("issue/{touristId:int}")]
        public IActionResult Issue(int touristId, [FromQuery] int days = 30)
        {
            var block = _blockchainService.IssueDigitalId(touristId, TimeSpan.FromDays(days));
            return Ok(block);
        }

        // POST api/digitalid/revoke/5
        [HttpPost("revoke/{touristId:int}")]
        public IActionResult Revoke(int touristId)
        {
            _blockchainService.RevokeDigitalId(touristId);
            return NoContent();
        }
    }
}
