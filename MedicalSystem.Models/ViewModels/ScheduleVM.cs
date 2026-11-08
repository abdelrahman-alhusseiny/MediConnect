using MedicalSystem.Models.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.ViewModels
{
    public class ScheduleVM
    {
        public Schedule Schedule { get; set; }
        public string? SourceView { get; set; }
        public IEnumerable<SelectListItem>? ClinicList { get; set; }
        public IEnumerable<SelectListItem>? DayOfWeekList { get; set; }

        public string? ClinicName { get; set; }
        public string? DisplayNotes { get; set; }

        public int? ClinicId { get; set; }
        public string? DayOfWeek { get; set; }
        public string? CurrentSort { get; set; }
        public int? CurrentPage { get; set; }
    }
}
