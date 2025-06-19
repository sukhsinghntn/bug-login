using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Generic;

namespace DynamicFormsApp.Shared.Models
{
    public class Form
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CreatedBy { get; set; }
        public bool RequireLogin { get; set; } = true;
        public bool NotifyOnResponse { get; set; } = false;
        public string? NotificationEmail { get; set; }
        public List<FormField>? Fields { get; set; }
    }
}
