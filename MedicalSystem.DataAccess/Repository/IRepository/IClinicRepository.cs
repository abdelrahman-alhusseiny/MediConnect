using MedicalSystem.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.DataAccess.Repository.IRepository
{
    public interface IClinicRepository : IRepository<Clinic>
    {
        Task<bool> IsNameExistsAsync(string name, int? excludeId = null);

        Task<(IEnumerable<Clinic>, int)> GetFilteredClinicsAsync(
            int doctorId,
            string searchString,
            string currentSort,
            int currentPage,
            int pageSize);

        Task UpdateAsync(Clinic clinic);
    }
}
