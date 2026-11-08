using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.DTOs.Booking
{
    public class GetDoctorDto
    {
        public int DoctorId { get; set; }
        public string DisplayImageUrl { get; set; }
        public string FullName { get; set; }
        public string AppUserEmail { get; set; }
        public string PhoneNumber { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public string DepartmentName { get; set; }
        public string DepartmentDescription { get; set; }
        public string AboutMe { get; set; }
        public double AverageRating { get; set; }
    }
}
