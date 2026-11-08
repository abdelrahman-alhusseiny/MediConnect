using MedicalSystem.Models.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.ViewModels
{
    public class NotificationRowVM
    {
        public Notification Notification { get; set; }
    }

    public class NotificationListVM
    {
        public IEnumerable<NotificationRowVM> Notifications { get; set; }

        public string? SearchString { get; set; }
        public string? TypeStatus { get; set; }
        public IEnumerable<SelectListItem>? TypeStatusList { get; set; }
        public string? CurrentSort { get; set; }
        public string? NameSort { get; set; }
        public string? CreatedAtSort { get; set; }
        public int? CurrentPage { get; set; }
        public int? TotalPages { get; set; }
    }
}
