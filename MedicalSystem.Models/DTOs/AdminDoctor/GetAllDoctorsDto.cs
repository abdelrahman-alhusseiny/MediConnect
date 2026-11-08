using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.DTOs.Doctor
{
    public class GetAllDoctorsDto
    {
        public int Id { get; set; }
        public string DisplayImageUrl { get; set; }
        public string FullName { get; set; }
        public string AppUserEmail { get; set; }
        public string AppUserLockoutStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
