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
    public class Notification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [HiddenInput]
        public int Id { get; set; }

        [Required(ErrorMessage = "Notification title is required")]
        [MinLength(3, ErrorMessage = "Title must be at least 3 characters long")]
        [MaxLength(100, ErrorMessage = "Title must be less than 100 characters")]
        [Display(Name = "Title", Prompt = "Enter notification title")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Notification message is required")]
        [MinLength(10, ErrorMessage = "Title must be at least 10 characters long")]
        [MaxLength(500, ErrorMessage = "Message must be less than 500 characters")]
        [Display(Name = "Message", Prompt = "Enter notification message")]
        public string Message { get; set; }

        [Required(ErrorMessage = "Notification type is required")]
        [Display(Name = "Type", Prompt = "Select notification type")]
        public string Type { get; set; } 

        [Required]
        [ValidateNever]
        public string Status { get; set; } = "Unread";

        [Required]
        [ValidateNever]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;




        [ValidateNever]
        public int? PatientId { get; set; } = null;

        [ForeignKey("PatientId")]
        [ValidateNever]
        public Patient Patient { get; set; }

        [ValidateNever]
        public int? DoctorId { get; set; } = null;

        [ForeignKey("DoctorId")]
        [ValidateNever]
        public Doctor Doctor { get; set; }
    }
}
