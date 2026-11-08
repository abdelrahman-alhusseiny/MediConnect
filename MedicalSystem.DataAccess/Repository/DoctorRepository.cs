using MedicalSystem.DataAccess.Data;
using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.Models;
using MedicalSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.DataAccess.Repository
{
    public class DoctorRepository : Repository<Doctor>, IDoctorRepository
    {
        private readonly ApplicationDbContext _db;
        public DoctorRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<Doctor>, int)> GetFilteredDoctorsAsync(
            string searchString,
            string lockoutStatus,
            int? departmentId,
            string currentSort,
            int currentPage,
            int pageSize)
        {
            var query = _db.Doctors.Include(d => d.AppUser).Include(d => d.Department).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(d => d.FullName.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(lockoutStatus))
            {
                if (lockoutStatus == "Locked")
                {
                    query = query.Where(d => d.AppUser.LockoutEnd.HasValue &&
                                             d.AppUser.LockoutEnd > DateTimeOffset.UtcNow);
                }
                else if (lockoutStatus == "Unlocked")
                {
                    query = query.Where(d => !d.AppUser.LockoutEnd.HasValue ||
                                             d.AppUser.LockoutEnd <= DateTimeOffset.UtcNow);
                }
            }

            if (departmentId.HasValue)
            {
                query = query.Where(d => d.DepartmentId == departmentId);
            }

            query = currentSort switch
            {
                "name" => query.OrderBy(d => d.FullName),
                "name_desc" => query.OrderByDescending(d => d.FullName),
                "created" => query.OrderBy(d => d.CreatedAt),
                "created_desc" => query.OrderByDescending(d => d.CreatedAt),
                "rate" => query.OrderBy(d => d.Reviews.Average(r => r.Rating)),
                "rate_desc" => query.OrderByDescending(d => d.Reviews.Average(r => r.Rating)),
                _ => query.OrderBy(d => d.FullName)
            };

            int totalItems = await query.CountAsync();

            var doctors = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (doctors, totalItems);
        }

        public async Task UpdateAsync(Doctor doctor)
        {
            var objFromDb = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == doctor.Id);

            if (objFromDb != null)
            {
                objFromDb.FullName = doctor.FullName;
                objFromDb.PhoneNumber = doctor.PhoneNumber;
                objFromDb.Address = doctor.Address;
                objFromDb.Gender = doctor.Gender;
                objFromDb.DateOfBirth = doctor.DateOfBirth;
                objFromDb.DepartmentId = doctor.DepartmentId;
                objFromDb.AboutMe = doctor.AboutMe;
                objFromDb.ImageUrl = doctor.ImageUrl;
            }
        }
    }
}
