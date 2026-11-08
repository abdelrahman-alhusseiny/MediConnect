using MedicalSystem.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.DataAccess.Repository.IRepository
{
    public interface IDoctorReviewRepository : IRepository<DoctorReview>
    {
        Task<double> GetAverageRatingAsync(int doctorId);
        Task<IEnumerable<DoctorReview>> GetFilteredDoctorReviewsAsync(int? doctorId);


        Task UpdateAsync(DoctorReview doctorReview);
    }
}
