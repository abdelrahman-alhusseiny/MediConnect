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
    public class Patient
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [HiddenInput]
        public int Id { get; set; }

        [Required(ErrorMessage = "Full Name is Required")]
        [MinLength(3, ErrorMessage = "Full Name must be at least 3 characters long")]
        [MaxLength(100,
            ErrorMessage = "Full Name must be less than 100 characters long")]
        [Display(Name = "Full Name", Prompt = "Enter full name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Phone Number is Required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [Display(Name = "Phone Number", Prompt = "Enter phone number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Address is Required")]
        [MinLength(5, ErrorMessage = "Address must be at least 5 characters long")]
        [MaxLength(200, ErrorMessage = "Address must be less than 200 characters long")]
        [Display(Name = "Address", Prompt = "Enter address")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Gender is Required")]
        [Display(Name = "Gender", Prompt = "Select gender")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Date of Birth is Required")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth", Prompt = "Enter date of birth")]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [ValidateNever]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ValidateNever]
        public string? ImageUrl { get; set; }

        [NotMapped]
        [Display(Name = "Profile Image (Optional)", Prompt = "Upload profile image")]
        public IFormFile? ImageFile { get; set; }

        [Required]
        [ValidateNever]
        public string AppUserId { get; set; }

        [ForeignKey("AppUserId")]
        [ValidateNever]
        public ApplicationUser AppUser { get; set; }

        [ValidateNever]
        public ICollection<Appointment> Appointments { get; set; }

        [ValidateNever]
        public ICollection<Notification> Notifications { get; set; }

        [ValidateNever]
        public ICollection<DoctorReview> Reviews { get; set; }
    }
}
