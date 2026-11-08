using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.ViewModels
{
    public class PayAppointmentVM
    {
        public int AppointmentId { get; set; }

        [Required(ErrorMessage = "Card number is required.")]
        [Display(Name = "Card Number", Prompt = "Enter your card number")]
        public string CardNumber { get; set; }

        [Required(ErrorMessage = "Expiry month is required.")]
        [Display(Name = "Expiry Month", Prompt = "Enter expiry month")]
        public int ExpiryMonth { get; set; }

        [Required(ErrorMessage = "Expiry year is required.")]
        [Display(Name = "Expiry Year", Prompt = "Enter expiry year")]
        public int ExpiryYear { get; set; }

        [Required(ErrorMessage = "CVV is required.")]
        [Display(Name = "CVV", Prompt = "Enter CVV")]
        public string CVV { get; set; }

        public string PatientName { get; set; }
        public string DoctorName { get; set; }
        public string ClinicName { get; set; }
        public decimal ConsultationPrice { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }

        public string? SourceView { get; set; }

        public string? SearchString { get; set; }
        public string? DayOfWeek { get; set; }
        public string? CurrentSort { get; set; }
        public int? CurrentPage { get; set; }
    }
}
