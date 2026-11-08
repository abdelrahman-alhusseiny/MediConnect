using MedicalSystem.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.DataAccess.Repository.IRepository
{
    public interface IScheduleRepository : IRepository<Schedule>
    {
        Task<bool> HasTimeConflictAsync(
                    int doctorId,
                    string dayOfWeek,
                    TimeSpan startTime,
                    TimeSpan endTime,
                    int? excludeScheduleId = null);
        Task<(IEnumerable<Schedule>, int)> GetFilteredSchedulesAsync(
            int? doctorId,
            int? clinicId,
            string dayOfWeek,
            string currentSort,
            int currentPage,
            int pageSize);

        Task UpdateAsync(Schedule schedule);
    }
}
