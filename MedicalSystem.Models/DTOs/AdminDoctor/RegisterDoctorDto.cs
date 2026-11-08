using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.DTOs.Doctor
{
    public class RegisterDoctorDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int DepartmentId { get; set; }
        public string? AboutMe { get; set; }
        public IFormFile? ImageFile { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
