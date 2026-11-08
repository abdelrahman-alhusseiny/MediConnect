using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } // Online or Cash
        public string? TransactionId { get; set; } // For online payments
        public string Status { get; set; } // Pending, Completed, Failed
        public DateTime? PaidAt { get; set; } // Timestamp if completed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int AppointmentId { get; set; }
        public Appointment Appointment { get; set; }
    }
}
