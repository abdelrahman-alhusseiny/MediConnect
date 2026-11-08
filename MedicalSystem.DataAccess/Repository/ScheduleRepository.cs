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
    public class ScheduleRepository : Repository<Schedule>, IScheduleRepository
    {
        private readonly ApplicationDbContext _db;
        public ScheduleRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<bool> HasTimeConflictAsync(
            int doctorId,
            string dayOfWeek,
            TimeSpan startTime,
            TimeSpan endTime,
            int? excludeScheduleId = null)
        {
            return await _db.Schedules.AnyAsync(s =>
                s.Clinic.DoctorId == doctorId &&               
                s.DayOfWeek == dayOfWeek &&
                (excludeScheduleId == null || s.Id != excludeScheduleId) &&
                (
                    (startTime >= s.StartTime && startTime < s.EndTime) || 
                    (endTime > s.StartTime && endTime <= s.EndTime) ||      
                    (startTime <= s.StartTime && endTime >= s.EndTime)     
                ));
        }


        public async Task<(IEnumerable<Schedule>, int)> GetFilteredSchedulesAsync(
            int? doctorId,
            int? clinicId,
            string dayOfWeek,
            string currentSort,
            int currentPage,
            int pageSize)
        {
            var query = _db.Schedules.Include(s => s.Clinic).AsQueryable();

            if (doctorId.HasValue)
            {
                query = query.Where(s => s.Clinic.DoctorId == doctorId.Value);
            }

            if (clinicId.HasValue)
            {
                query = query.Where(s => s.ClinicId == clinicId);
            }

            if (!string.IsNullOrWhiteSpace(dayOfWeek))
            {
                query = query.Where(s => s.DayOfWeek == dayOfWeek);
            }

            query = currentSort switch
            {
                "day" => query.OrderBy(s =>
                    s.DayOfWeek == "Saturday" ? 1 :
                    s.DayOfWeek == "Sunday" ? 2 :
                    s.DayOfWeek == "Monday" ? 3 :
                    s.DayOfWeek == "Tuesday" ? 4 :
                    s.DayOfWeek == "Wednesday" ? 5 :
                    s.DayOfWeek == "Thursday" ? 6 : 7),
                "day_desc" => query.OrderByDescending(s =>
                    s.DayOfWeek == "Saturday" ? 1 :
                    s.DayOfWeek == "Sunday" ? 2 :
                    s.DayOfWeek == "Monday" ? 3 :
                    s.DayOfWeek == "Tuesday" ? 4 :
                    s.DayOfWeek == "Wednesday" ? 5 :
                    s.DayOfWeek == "Thursday" ? 6 : 7),
                "created" => query.OrderBy(s => s.CreatedAt),
                "created_desc" => query.OrderByDescending(s => s.CreatedAt),
                _ => query.OrderBy(s => s.DayOfWeek)
            };

            int totalItems = await query.CountAsync();

            var schedules = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (schedules, totalItems);
        }

        public async Task UpdateAsync(Schedule schedule)
        {
            var objFromDb = await _db.Schedules.FirstOrDefaultAsync(
                s => s.Id == schedule.Id);

            if (objFromDb != null)
            {
                objFromDb.ClinicId = schedule.ClinicId;
                objFromDb.DayOfWeek = schedule.DayOfWeek;
                objFromDb.StartTime = schedule.StartTime;
                objFromDb.EndTime = schedule.EndTime;
                objFromDb.AppointmentDuration = schedule.AppointmentDuration;
                objFromDb.MaxPatientsPerDay = schedule.MaxPatientsPerDay;
                objFromDb.Notes = schedule.Notes;
            }
        }
    }
}
