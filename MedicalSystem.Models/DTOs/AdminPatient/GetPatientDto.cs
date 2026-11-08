using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.DTOs.Patient
{
    public class GetPatientDto
    {
        public int Id { get; set; }
        public string DisplayImageUrl { get; set; }
        public string FullName { get; set; }
        public string AppUserEmail { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string AppUserLockoutStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
