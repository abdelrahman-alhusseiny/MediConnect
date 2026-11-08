using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.DTOs.Appointment
{
    public class GetAllPatientAppointments
    {
        public int Id { get; set; }
        public string DepartmentName { get; set; }
        public string ClinicName { get; set; }
        public string ScheduleDayOfWeek { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string Status { get; set; }

        public bool IsPending { get; set; } = false;
        public bool IsPayable { get; set; } = false;
    }
}
