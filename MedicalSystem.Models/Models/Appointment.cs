using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.Models
{
    public class Appointment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [HiddenInput]
        public int Id { get; set; }

        [Required(ErrorMessage = "Patient Name is Required")]
        [MinLength(3, ErrorMessage = "Patient Name must be at least 3 characters long")]
        [MaxLength(100,
            ErrorMessage = "Patient Name must be less than 100 characters long")]
        [Display(Name = "Patient Name", Prompt = "Enter patient name")]
        public string PatientName { get; set; }

        [Required(ErrorMessage = "Patient Phone Number is Required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [Display(Name = "Patient Phone Number", Prompt = "Enter patient phone number")]
        public string PatientPhoneNumber { get; set; }

        [Required(ErrorMessage = "Gender is Required")]
        [Display(Name = "Patient Gender", Prompt = "Select gender")]
        public string PatientGender { get; set; }

        [Required(ErrorMessage = "Patient Age is Required")]
        [Range(0, 120, ErrorMessage = "Age must be between 0 and 120 years")]
        [Display(Name = "Patient Age", Prompt = "Enter patient age")]
        public int PatientAge { get; set; }

        [Required(ErrorMessage = "Address is Required")]
        [MinLength(5, ErrorMessage = "Address must be at least 5 characters long")]
        [MaxLength(200, ErrorMessage = "Address must be less than 200 characters long")]
        [Display(Name = "Patient Address", Prompt = "Enter address")]
        public string PatientAddress { get; set; }

        [Required(ErrorMessage = "Reason for Visit is Required")]
        [MinLength(10, ErrorMessage = "Reason for Visit must be at least 10 characters long")]
        [MaxLength(500, ErrorMessage = "Reason for Visit must be less than 500 characters long")]
        [Display(Name = "Reason for Visit", Prompt = "Enter reason for visit")]
        public string ReasonForVisit { get; set; }

        [Required(ErrorMessage = "Payment Method is Required")]
        [Display(Name = "Payment Method", Prompt = "Select payment method")]
        public string PaymentMethod { get; set; }

        [ValidateNever]
        public string? PatientImageUrl { get; set; }

        [NotMapped]
        [Display(Name = "Image (Optional)", Prompt = "Upload image")]
        public IFormFile? ImageFile { get; set; }


        [Required]
        [ValidateNever]
        public DateTime AppointmentDate { get; set; } 

        [Required]
        [ValidateNever]
        public TimeSpan AppointmentTime { get; set; } 

        [Required]
        [ValidateNever]
        public string Status { get; set; }   

        [Required]
        [ValidateNever] 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        [Required]
        [ValidateNever]
        public int PatientId { get; set; }  

        [ForeignKey("PatientId")]
        [ValidateNever]
        public Patient Patient { get; set; }

        [Required]
        [ValidateNever]
        public int ScheduleId { get; set; }

        [ForeignKey("ScheduleId")]
        [ValidateNever]
        public Schedule Schedule { get; set; }

        [Required]
        [ValidateNever]
        public Payment Payment { get; set; }
    }
}
