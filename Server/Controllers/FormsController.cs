using DynamicFormsApp.Server.Services;
using DynamicFormsApp.Shared.Models;
using DynamicFormsApp.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicFormsApp.Server.Controllers
{
    [Route("api/forms")]
    [ApiController]
    public class FormsController : ControllerBase
    {
        private readonly DynamicFormService _svc;
        private readonly IUserService _userSvc;
        private readonly IEmailService _emailSvc;

        public FormsController(DynamicFormService svc, IUserService userSvc, IEmailService emailSvc)
        {
            _svc = svc;
            _userSvc = userSvc;
            _emailSvc = emailSvc;
        }

        // POST /api/forms
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFormDto dto)
        {
            if (!Request.Cookies.TryGetValue("userName", out var user) || string.IsNullOrEmpty(user))
            {
                return Unauthorized();
            }

            var newFormId = await _svc.CreateFormAsync(dto.Name, dto.Description, dto.Fields, user, dto.RequireLogin, dto.NotifyOnResponse, dto.NotificationEmail, dto.IsActive);
            return Ok(new { FormId = newFormId });
        }


        // GET /api/forms/{id}/responses
        [HttpGet("{id}/responses")]
        public async Task<ActionResult<List<Dictionary<string, object>>>> GetResponses(int id)
        {
            var rows = await _svc.GetResponsesAsync(id);
            return Ok(rows);
        }

        [HttpGet("{id}/responses/{responseId}")]
        public async Task<ActionResult<Dictionary<string, object>>> GetResponse(int id, int responseId)
        {
            var row = await _svc.GetResponseAsync(id, responseId);
            return Ok(row);
        }


        // GET /api/forms/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Form>> Get(int id)
        {
            var form = await _svc.GetFormAsync(id);
            return Ok(form);
        }
        // GET /api/forms
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Form>>> GetAll()
        {
            var all = await _svc.GetAllFormsAsync();
            return Ok(all);
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Form>>> Search([FromQuery] bool includePrivate = false)
        {
            var loggedIn = Request.Cookies.TryGetValue("userName", out var user) && !string.IsNullOrEmpty(user);
            var results = await _svc.SearchFormsAsync(loggedIn && includePrivate);
            return Ok(results);
        }

        [HttpGet("mine")]
        public async Task<ActionResult<IEnumerable<Form>>> GetMine()
        {
            if (!Request.Cookies.TryGetValue("userName", out var user) || string.IsNullOrEmpty(user))
            {
                return Unauthorized();
            }

            var mine = await _svc.GetFormsByUserAsync(user);
            return Ok(mine);
        }

        // DELETE /api/forms/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!Request.Cookies.TryGetValue("userName", out var user) || string.IsNullOrEmpty(user))
            {
                return Unauthorized();
            }

            await _svc.DeactivateFormAsync(id, user);
            return NoContent();
        }

        // POST /api/forms/{id}/responses
        [HttpPost("{id}/responses")]

        public async Task<IActionResult> Submit(int id, [FromBody] Dictionary<string, object> values)
        {
            try
            {
                var form = await _svc.StoreResponseAsync(id, values);

                if (form.NotifyOnResponse)
                {
                    string? to = form.NotificationEmail;
                    if (string.IsNullOrWhiteSpace(to))
                    {
                        var user = await _userSvc.GetUserData(form.CreatedBy);
                        if (user != null && !string.IsNullOrEmpty(user.Email))
                        {
                            to = user.Email;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(to))
                    {
                        await _emailSvc.SendFormResponseNotification(to, form.Name, form.Id);
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                // Return full exception details in development
                return StatusCode(500, new
                {
                    Error = ex.Message,
                    Details = ex.ToString()
                });
            }
        }
    }
}
