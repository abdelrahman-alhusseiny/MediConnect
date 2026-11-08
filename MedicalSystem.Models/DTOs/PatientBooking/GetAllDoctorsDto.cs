using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.DTOs.Booking
{
    public class GetAllDoctorsDto
    {
        public int DoctorId { get; set; }
        public string DisplayImageUrl { get; set; }
        public string FullName { get; set; }
        public string DepartmentName { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public double AverageRating { get; set; }
    }
}
