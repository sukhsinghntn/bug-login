﻿using DynamicFormsApp.Shared.Services;
using DynamicFormsApp.Shared.Models;
using System.Net.Mail;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace DynamicFormsApp.Server.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _env;

        public EmailService(IConfiguration configuration, IHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        public async Task SendBugReportEmail(EmailModel email)
        {
            var mail = new MailMessage
            {
                From = new MailAddress("NDABugReport@ntnanderson.com"),
                Subject = $"Bug Report for Application Device Tracking",
                IsBodyHtml = true,
                Body = $"{email.UserName} has reported a bug:<br/><br/>{email.Body}" +
                       $"<br/><br/><a href='{email.Link}'>View the page</a>."
            };

            // Primary recipient
            mail.To.Add(_configuration["Email:BugReportEmailTo"]);

            // Attach any files
            if (email.Attachments != null)
            {
                foreach (var att in email.Attachments)
                {
                    var stream = new MemoryStream(att.Content);
                    mail.Attachments.Add(new Attachment(stream, att.Name));
                }
            }

            using var client = new SmtpClient(_configuration["Email:IP"])
            {
                Port = int.Parse(_configuration["Email:Port"]!)
            };
            await client.SendMailAsync(mail);
        }

        public async Task SendFormResponseNotification(string toEmail, string formName, int formId)
        {
            var mail = new MailMessage
            {
                From = new MailAddress("NDABugReport@ntnanderson.com"),
                Subject = $"New response for form '{formName}'",
                Body = $"A new response has been submitted for your form '{formName}'.",
                IsBodyHtml = false
            };

            mail.To.Add(toEmail);

            using var client = new SmtpClient(_configuration["Email:IP"])
            {
                Port = int.Parse(_configuration["Email:Port"]!)
            };
            await client.SendMailAsync(mail);
        }
    }
}
