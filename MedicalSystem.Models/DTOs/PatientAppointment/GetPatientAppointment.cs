using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.DTOs.Appointment
{
    public class GetPatientAppointment
    {
        public int Id { get; set; }

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
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public int AppointmentDuration { get; set; }
        public string ScheduleNotes { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; } 

        public string PatientDisplayImageUrl { get; set; }
        public string PatientName { get; set; }
        public string PatientPhoneNumber { get; set; }
        public string PatientAddress { get; set; }
        public string PatientGender { get; set; }
        public int PatientAge { get; set; }

        public string ReasonForVisit { get; set; }
        public string PaymentMethod { get; set; }

        public bool IsPending { get; set; } = false;
        public bool IsPayable { get; set; } = false;
        public string? PaymentStatus { get; set; }
    }
}
