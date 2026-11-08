using MedicalSystem.DataAccess.Data;
using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.DataAccess.Repository
{
    public class DoctorReviewRepository : Repository<DoctorReview>, IDoctorReviewRepository
    {
        private readonly ApplicationDbContext _db;
        public DoctorReviewRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<double> GetAverageRatingAsync(int doctorId)
        {
            var avgRating = await _db.DoctorReviews
                .Where(r => r.DoctorId == doctorId)
                .AverageAsync(r => (double?)r.Rating) ?? 0;

            return avgRating;
        }

        public async Task<IEnumerable<DoctorReview>> GetFilteredDoctorReviewsAsync(int? doctorId)
        {
            var query = _db.DoctorReviews.Include(d => d.Patient).Include(d => d.Doctor).AsQueryable();

            if (doctorId.HasValue)
            {
                query = query.Where(d => d.DoctorId == doctorId);
            }

            query = query.OrderByDescending(d => d.CreatedAt);
        
            int totalItems = await query.CountAsync();

            var doctors = await query.ToListAsync();

            return doctors;
        }

        public async Task UpdateAsync(DoctorReview doctorReview)
        {
            var objFromDb = await _db.DoctorReviews.FirstOrDefaultAsync(d => d.Id == doctorReview.Id);

            if (objFromDb != null)
            {
                objFromDb.Rating = doctorReview.Rating;
                objFromDb.Comment = doctorReview.Comment;
            }
        }
    }
}
