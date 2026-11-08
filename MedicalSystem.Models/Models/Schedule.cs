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
    public class Schedule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [HiddenInput]
        public int Id { get; set; }

        [Required(ErrorMessage = "Clinic is Required")]
        [Display(Name = "Schedule Clinic", Prompt = "Select clinic")]
        public int ClinicId { get; set; }

        [ForeignKey("ClinicId")]
        [ValidateNever]
        public Clinic Clinic { get; set; }

        [Required(ErrorMessage = "Day is Required")]
        [Display(Name = "Schedule Day", Prompt = "Select day")]
        public string DayOfWeek { get; set; }

        [Required(ErrorMessage = "Start Time is Required")]
        [DataType(DataType.Time)]
        [Display(Name = "Schedule Start Time", Prompt = "Enter schedule start time")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "End Time is Required")]
        [DataType(DataType.Time)]
        [Display(Name = "Schedule End Time", Prompt = "Enter schedule end time")]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Appointment Duration is Required")]
        [Range(5, 120, ErrorMessage = "Appointment Duration must be between 5 and 120 minutes")]
        [Display(Name = "Schedule Appointment Duration (minutes)", Prompt = "Enter schedule appointment duration in minutes")]
        public int AppointmentDuration { get; set; }

        [Required]
        [ValidateNever]
        public int MaxPatientsPerDay { get; set; }

        [MinLength(10, ErrorMessage = "Notes must be at least 10 characters long")]
        [MaxLength(500, ErrorMessage = "Notes must be less than 500 characters long")]
        [Display(Name = "Schedule Notes (Optional)", Prompt = "Enter any additional notes about the schedule")]
        public string? Notes { get; set; }

        [Required]
        [ValidateNever]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ValidateNever]
        public ICollection<Appointment> Appointments { get; set; }
    }
}
