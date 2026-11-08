using MedicalSystem.DataAccess.Data;
using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.DataAccess.Repository
{
    public class ClinicRepository : Repository<Clinic>, IClinicRepository
    {
        private readonly ApplicationDbContext _db;
        public ClinicRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<bool> IsNameExistsAsync(
            string name, int? excludeId = null)
        {
            return await _db.Clinics.AnyAsync(
                c => c.Name == name &&
                (!excludeId.HasValue || c.Id != excludeId.Value));
        }

        public async Task<(IEnumerable<Clinic>, int)> GetFilteredClinicsAsync(
            int doctorId,
            string searchString,
            string currentSort,
            int currentPage,
            int pageSize)
        {
            var query = _db.Clinics.AsQueryable();

            query = query.Where(c => c.DoctorId == doctorId);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(a => a.Name.Contains(searchString));
            }

            query = currentSort switch
            {
                "name" => query.OrderBy(c => c.Name),
                "name_desc" => query.OrderByDescending(c => c.Name),
                "created" => query.OrderBy(c => c.CreatedAt),
                "created_desc" => query.OrderByDescending(c => c.CreatedAt),
                _ => query.OrderBy(c => c.Name)
            };

            int totalItems = await query.CountAsync();

            var clinics = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (clinics, totalItems);
        }

        public async Task UpdateAsync(Clinic clinic)
        {
            var objFromDb = await _db.Clinics.FirstOrDefaultAsync(
                c => c.Id == clinic.Id);

            if (objFromDb != null)
            {
                objFromDb.Name = clinic.Name;
                objFromDb.Address = clinic.Address;
                objFromDb.PhoneNumber = clinic.PhoneNumber;
                objFromDb.ConsultationPrice = clinic.ConsultationPrice;
            }
        }
    }
}
