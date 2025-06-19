using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public string? NotificationEmail { get; set; }
    }
}
