using MedicalSystem.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.DataAccess.Repository.IRepository
{
    public interface IDepartmentRepository : IRepository<Department>
    {
        Task<bool> IsNameExistsAsync(string name, int? excludeId = null);

        Task<(IEnumerable<Department>, int)> GetFilteredDepartmentsAsync(
            string searchString,
            string currentSort,
            int currentPage,
            int pageSize);

        Task UpdateAsync(Department department);
    }
}
