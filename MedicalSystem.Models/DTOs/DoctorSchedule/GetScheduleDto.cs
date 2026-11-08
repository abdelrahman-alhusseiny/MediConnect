using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.DTOs.Schedule
{
    public class GetScheduleDto
    {
        public int Id { get; set; }
        public string ClinicName { get; set; }
        public string DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int AppointmentDuration { get; set; }
        public int MaxPatientsPerDay { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; } 
    }
}
