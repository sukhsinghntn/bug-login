using DynamicFormsApp.Shared.Models;
using System.Collections.Generic;

namespace DynamicFormsApp.Server.Services
{
    public class CreateFormDto
    {
        public string Name { get; set; }
        public List<FormField> Fields { get; set; }
        public bool RequireLogin { get; set; } = true;
        public bool NotifyOnResponse { get; set; } = false;
        public string? CreatedBy { get; set; }
    }
}
