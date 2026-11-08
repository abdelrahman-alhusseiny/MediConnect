using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.DTOs.Booking
{
    public class GetClinicSchedulesDto
    {
        public int ScheduleId { get; set; }
        public string DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int AppointmentDuration { get; set; }
        public string Notes { get; set; }
    }
}
