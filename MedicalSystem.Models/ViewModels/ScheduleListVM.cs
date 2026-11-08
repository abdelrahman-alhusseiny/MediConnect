using MedicalSystem.Models.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.ViewModels
{
    public class ScheduleRowVM
    {
        public Schedule Schedule { get; set; }
        public string? ClinicName { get; set; }
        public string? DisplayNotes { get; set; }
    }

    public class ScheduleListVM
    {
        public IEnumerable<ScheduleRowVM> Schedules { get; set; }
        public string? SourceView { get; set; }


        public int? DoctorId { get; set; }
        public int? ClinicId { get; set; }
        public IEnumerable<SelectListItem>? ClinicList { get; set; }
        public string? DayOfWeek { get; set; }
        public IEnumerable<SelectListItem>? DayOfWeekList { get; set; }
        public string? CurrentSort { get; set; }
        public string? DaySort { get; set; }
        public string? CreatedAtSort { get; set; }
        public int? CurrentPage { get; set; }
        public int? TotalPages { get; set; }
    }
}
