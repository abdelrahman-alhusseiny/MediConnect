using MedicalSystem.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.DataAccess.Repository.IRepository
{
    public interface IAppointmentRepository : IRepository<Appointment>
    {
        Task<(IEnumerable<Appointment>, int)> GetFilteredAppointmentsAsync(
            int? patientId,
            int? doctorId,
            string searchString,
            int? clinicId,
            string dayOfWeek,
            string currentSort,
            int currentPage,
            int pageSize,
            string[] statusFilter = null);

        Task UpdateAsync(Appointment appointment);
    }
}
