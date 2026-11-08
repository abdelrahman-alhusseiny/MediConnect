using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.DTOs.DoctorReview
{
    public class GetAllReviews
    {
        public double AverageRating { get; set; } = 0;
        public string? PatientImageUrl { get; set; }
        public string PatientName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
