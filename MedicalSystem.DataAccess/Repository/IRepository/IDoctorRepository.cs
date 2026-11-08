using MedicalSystem.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.DataAccess.Repository.IRepository
{
    public interface IDoctorRepository : IRepository<Doctor>
    {
        Task<(IEnumerable<Doctor>, int)> GetFilteredDoctorsAsync(
            string searchString,
            string lockoutStatus,
            int? departmentId,
            string currentSort,
            int currentPage,
            int pageSize);

        Task UpdateAsync(Doctor doctor);
    }
}
