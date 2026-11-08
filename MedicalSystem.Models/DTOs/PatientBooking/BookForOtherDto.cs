using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.DTOs.Booking
{
    public class BookForOtherDto
    {
        public string PatientName { get; set; }
        public string PatientPhoneNumber { get; set; }
        public string PatientAddress { get; set; }
        public string PatientGender { get; set; }
        public int PatientAge { get; set; }
        public IFormFile? ImageFile { get; set; }
        public string ReasonForVisit { get; set; }
        public string PaymentMethod { get; set; }
    }
}
