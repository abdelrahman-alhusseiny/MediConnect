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
    public class DoctorReview
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }


        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        [Display(Name = "Rating")]
        public int Rating { get; set; }

        [MaxLength(1000, ErrorMessage = "Review must not exceed 1000 characters.")]
        [Display(Name = "Comment (Optional)", Prompt = "Write your feedback about the doctor")]
        public string? Comment { get; set; }

        [Required]
        [ValidateNever]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [ValidateNever]
        public int DoctorId { get; set; }

        [ForeignKey("DoctorId")]
        [ValidateNever]
        public Doctor Doctor { get; set; }

        [Required]
        [ValidateNever]
        public int? PatientId { get; set; }

        [ForeignKey("PatientId")]
        [ValidateNever]
        public Patient Patient { get; set; }
    }
}
