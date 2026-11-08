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
    public class PatientRepository : Repository<Patient>, IPatientRepository
    {
        private readonly ApplicationDbContext _db;
        public PatientRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<Patient>, int)> GetFilteredPatientsAsync(
            string searchString,
            string lockoutStatus,
            string currentSort,
            int currentPage,
            int pageSize)
        {
            var query = _db.Patients.Include(d => d.AppUser).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(p => p.FullName.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(lockoutStatus))
            {
                if (lockoutStatus == "Locked")
                {
                    query = query.Where(p => p.AppUser.LockoutEnd.HasValue &&
                                             p.AppUser.LockoutEnd > DateTimeOffset.UtcNow);
                }
                else if (lockoutStatus == "Unlocked")
                {
                    query = query.Where(p => !p.AppUser.LockoutEnd.HasValue ||
                                             p.AppUser.LockoutEnd <= DateTimeOffset.UtcNow);
                }
            }

            query = currentSort switch
            {
                "name" => query.OrderBy(p => p.FullName),
                "name_desc" => query.OrderByDescending(p => p.FullName),
                "created" => query.OrderBy(p => p.CreatedAt),
                "created_desc" => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderBy(p => p.FullName)
            };

            int totalItems = await query.CountAsync();

            var patients = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (patients, totalItems);
        }

        public async Task UpdateAsync(Patient patient)
        {
            var objFromDb = await _db.Patients.FirstOrDefaultAsync(p => p.Id == patient.Id);

            if (objFromDb != null)
            {
                objFromDb.FullName = patient.FullName;
                objFromDb.PhoneNumber = patient.PhoneNumber;
                objFromDb.Address = patient.Address;
                objFromDb.Gender = patient.Gender;
                objFromDb.DateOfBirth = patient.DateOfBirth;
                objFromDb.ImageUrl = patient.ImageUrl;
            }
        }
    }
}
