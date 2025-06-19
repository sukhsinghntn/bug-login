using DynamicFormsApp.Shared.Services;
using DynamicFormsApp.Shared.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DynamicFormsApp.Client.Services
{
    public class EmailServiceProxy : IEmailService
    {
        private readonly HttpClient _httpClient;

        public EmailServiceProxy(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task SendBugReportEmail(EmailModel email)
        {
            await _httpClient.PostAsJsonAsync("api/email/bug/", email);
        }
    }
}
