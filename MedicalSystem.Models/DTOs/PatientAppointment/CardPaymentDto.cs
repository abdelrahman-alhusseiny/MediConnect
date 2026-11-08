using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.DTOs.PatientAppointment
{
    public class CardPaymentDto
    {
        public int AppointmentId { get; set; }
        public string StripeToken { get; set; }
    }
}
