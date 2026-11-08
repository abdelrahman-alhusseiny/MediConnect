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
    public class Department
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [HiddenInput]
        public int Id { get; set; }

        [Required(ErrorMessage = "Department Name is Required")]
        [MinLength(3, ErrorMessage = "Department Name must be at least 3 characters long")]
        [MaxLength(100, ErrorMessage = "Deparmtment Name must be less than 100")]
        [Display(Name = "Department Name", Prompt = "Enter Department name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Department Description is Required")]
        [MinLength(10, ErrorMessage = "Description must be at least 10 characters long")]
        [MaxLength(500, ErrorMessage = "Description must be less than 500 characters")]
        [Display(Name = "Department Description", Prompt = "Enter Department description")]
        public string Description { get; set; }

        [Required]
        [ValidateNever]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ValidateNever]
        public ICollection<Doctor> Doctors { get; set; }
    }
}
