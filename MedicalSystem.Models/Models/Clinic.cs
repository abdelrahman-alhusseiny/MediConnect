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
    public class Clinic
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [HiddenInput]
        public int Id { get; set; }

        [Required(ErrorMessage = "Clinic Name is Required")]
        [MinLength(3, ErrorMessage = "Clinic Name must be at least 3 characters long")]
        [MaxLength(100, ErrorMessage = "Clinic Name must be less than 100")]
        [Display(Name = "Clinic Name", Prompt = "Enter clinic name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Clinic Address is Required")]
        [MinLength(5, ErrorMessage = "Clinic Address must be at least 5 characters long")]
        [MaxLength(200, ErrorMessage = "Clinic Address must be less than 200 characters long")]
        [Display(Name = "Clinic Address", Prompt = "Enter clinic address")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Clinic Phone Number is Required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [Display(Name = "Clinic Phone Number", Prompt = "Enter clinic phone number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Consultation price is required")]
        [Range(50, 5000, ErrorMessage = "Price must be between 50 and 5000 EGP")]
        [DataType(DataType.Currency)]
        [Display(Name = "Clinic Consultation Price", Prompt = "Enter clinic consultation price")]
        public decimal ConsultationPrice { get; set; }

        [Required]
        [ValidateNever]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [ValidateNever]
        public int DoctorId { get; set; }

        [ForeignKey("DoctorId")]
        [ValidateNever]
        public Doctor Doctor { get; set; }

        [ValidateNever]
        public ICollection<Schedule> Schedules { get; set; }
    }
}
