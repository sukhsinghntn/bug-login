using Microsoft.AspNetCore.Mvc;
using DynamicFormsApp.Shared.Services;
using DynamicFormsApp.Shared.Models;
using System.Threading.Tasks;

namespace DynamicFormsApp.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("bug")]
        public async Task<IActionResult> SendBugReportEmail([FromBody] EmailModel email)
        {
            await _emailService.SendBugReportEmail(email);
            return Ok();
        }
    }
}
