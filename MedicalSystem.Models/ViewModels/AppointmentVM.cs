using MedicalSystem.Models.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.ViewModels
{
    public class AppointmentVM
    {
        public Appointment Appointment { get; set; }
        public string? SourceView { get; set; }
        public IEnumerable<SelectListItem>? GenderList { get; set; }
        public IEnumerable<SelectListItem>? PaymentMethodList { get; set; }


        public int? ScheduleId { get; set; }
        public int? DoctorId { get; set; }

        public string? DoctorDisplayImageUrl { get; set; }
        public string? DoctorName { get; set; }
        public int? DoctorAge { get; set; }
        public string? DepartmentName { get; set; }
        public string? DoctorAboutMe { get; set; }

        public string? ClinicName { get; set; }
        public string? ClinicAddress { get; set; }
        public string? ClinicPhoneNumber { get; set; }
        public decimal? ConsultationPrice { get; set; }

        public string? ScheduleDayOfWeek { get; set; }
        public int? AppointmentDuration { get; set; }
        public string? ScheduleNotes { get; set; }

        public string? PatientDisplayImageUrl { get; set; }
        public bool HasCustomImage { get; set; } = false;

        public bool IsPending { get; set; } = false;
        public bool CanBeCompleted { get; set; } = false;
        public bool IsPayable { get; set; } = false;
        public string? PaymentStatus { get; set; }

        public string? SearchString { get; set; }
        public int? ClinicId { get; set; }
        public string? DayOfWeek { get; set; }
        public string? CurrentSort { get; set; }
        public int? CurrentPage { get; set; }
    }
}

