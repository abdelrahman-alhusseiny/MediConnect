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
    public class DepartmentRepository : Repository<Department>, IDepartmentRepository
    {
        private readonly ApplicationDbContext _db;
        public DepartmentRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<bool> IsNameExistsAsync(
            string name, int? excludeId = null)
        {
            return await _db.Departments.AnyAsync(
                d => d.Name == name &&
                (!excludeId.HasValue || d.Id != excludeId.Value));
        }

        public async Task<(IEnumerable<Department>, int)> GetFilteredDepartmentsAsync(
            string searchString,
            string currentSort,
            int currentPage,
            int pageSize)
        {
            var query = _db.Departments.AsQueryable();

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

            var departments = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (departments, totalItems);
        }

        public async Task UpdateAsync(Department department)
        {
            var objFromDb = await _db.Departments.FirstOrDefaultAsync(
                d => d.Id == department.Id);

            if (objFromDb != null)
            {
                objFromDb.Name = department.Name;
                objFromDb.Description = department.Description;
            }
        }
    }
}
