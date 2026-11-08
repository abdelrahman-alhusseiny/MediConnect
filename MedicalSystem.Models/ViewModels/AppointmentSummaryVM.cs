using MedicalSystem.Models.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.ViewModels
{
    public class AppointmentSummaryVM
    {
        public int ScheduleId { get; set; }
        public int ClinicId { get; set; }
        public int DoctorId { get; set; }

        public string DoctorDisplayImageUrl { get; set; }
        public string DoctorName { get; set; }
        public int DoctorAge { get; set; }
        public string DepartmentName { get; set; }
        public string DoctorAboutMe { get; set; }

        public string ClinicName { get; set; }
        public string ClinicAddress { get; set; }
        public string ClinicPhoneNumber { get; set; }
        public decimal ConsultationPrice { get; set; }

        public string ScheduleDayOfWeek { get; set; }
        public TimeSpan ScheduleStartTime { get; set; }
        public TimeSpan ScheduleEndTime { get; set; }
        public int AppointmentDuration { get; set; }
        public string ScheduleNotes { get; set; }

        public string PatientDisplayImageUrl { get; set; }
        public string PatientName { get; set; }
        public string PatientPhoneNumber { get; set; }
        public string PatientAddress { get; set; }
        public string PatientGender { get; set; }
        public int? PatientAge { get; set; }
    }
}
