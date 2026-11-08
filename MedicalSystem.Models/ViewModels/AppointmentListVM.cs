using MedicalSystem.Models.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.ViewModels
{
    public class AppointmentRowVM
    {
        public Appointment Appointment { get; set; }
        public string? ClinicName { get; set; }
        public string? AppointmentDay { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsPending { get; set; } = false;
        public bool CanBeCompleted { get; set; } = false;
        public bool IsPayable { get; set; } = false;
    }

    public class AppointmentListVM
    {
        public IEnumerable<AppointmentRowVM> Appointments { get; set; }
        public string? SourceView { get; set; }


        public string? SearchString { get; set; }
        public int? ClinicId { get; set; }
        public IEnumerable<SelectListItem>? ClinicList { get; set; }
        public string? DayOfWeek { get; set; }
        public IEnumerable<SelectListItem>? DayOfWeekList { get; set; }
        public string? CurrentSort { get; set; }
        public string? NameSort { get; set; }
        public string? PatientSort { get; set; }
        public string? DateSort { get; set; }
        public int? CurrentPage { get; set; }
        public int? TotalPages { get; set; }
    }
}
