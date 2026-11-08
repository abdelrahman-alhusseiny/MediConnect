using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.DTOs.DoctorAppointment
{
    public class GetAllDoctorAppointmentsDto
    {
        public int Id { get; set; }
        public string ClinicName { get; set; }
        public string PatientName { get; set; }
        public string ScheduleDayOfWeek { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string Status { get; set; }

        public bool IsPending { get; set; } = false;
        public bool CanBeCompleted { get; set; } = false;
    }
}
