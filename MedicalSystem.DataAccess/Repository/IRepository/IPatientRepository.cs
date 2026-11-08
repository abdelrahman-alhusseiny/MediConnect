using MedicalSystem.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.DataAccess.Repository.IRepository
{
    public interface IPatientRepository : IRepository<Patient>
    {
        Task<(IEnumerable<Patient>, int)> GetFilteredPatientsAsync(
            string searchString,
            string lockoutStatus,
            string currentSort,
            int currentPage,
            int pageSize);

        Task UpdateAsync(Patient patient);
    }
}
