﻿using AttendanceTrackingSystem.IRepository;
using AttendanceTrackingSystem.Models;

namespace AttendanceTrackingSystem.Repository
{
    public class RepoPermission : IRepoPermission
    {
        AttendanceDBContext db;
        public RepoPermission(AttendanceDBContext _db)
        {
            db = _db;
        }

        public void Add(Permission permission)
        {
            db.Permissions.Add(permission);
            db.SaveChanges();
        }
		public string CheckSchedule(int studentId, DateTime date)
		{
			var student = db.Students.FirstOrDefault(s => s.UserId == studentId);
			if (student == null)
			{
				return "Student not found";
			}

			var trackId = student.TrackId;

			var scheduleDay = db.Schedules.FirstOrDefault(s => s.Date.Date == date.Date && s.TrackId == trackId);
			if (scheduleDay == null)
			{
				return "No schedule found for the specified date";
			}

			if (scheduleDay.Type == ScheduleType.Funday || scheduleDay.Type == ScheduleType.Holiday)
			{
				return "This Day Is Holiday or Funday";
			}
			
			else
			{
				var schedule = db.Schedules.FirstOrDefault(s => s.Date.Date == date.Date && s.TrackId == trackId);
				return schedule != null ? "Schedule found" : "No schedule found for the specified date";
			}
		}

		public bool CheckPermission(int studentId, DateTime date)
        {
            return db.Permissions.Any(p => p.UserId == studentId && p.Date.Date == date.Date);

        }

        public void Delete(int id)
        {
            var obj = db.Permissions.FirstOrDefault(a => a.PermissionId == id);
            db.Permissions.Remove(obj);
            db.SaveChanges();
        }

        public List<Permission> getAll()
        {
            return db.Permissions.ToList();
        }

        public List<Permission> getAllById(int userId)
        {
            return db.Permissions.Where(p=>p.UserId==userId).ToList();
        }
        public List<Permission> GetAllBySupervisorId(int supervisorId)
        {
            var track = db.Tracks.FirstOrDefault(t => t.SupervisorId == supervisorId);

            var studentIds = track.Students.Select(s => s.UserId).ToList();

            return db.Permissions.Where(p => studentIds.Contains(p.UserId)).ToList();
        }

        public Permission getById(int id)
        {
            return db.Permissions.FirstOrDefault(a => a.PermissionId == id);
        }

        public void Update(Permission permission)
        {
            try
            {
                if (permission == null)
                {
                    throw new ArgumentNullException(nameof(permission), "Permission cannot be null.");
                }

                var student = db.Students.FirstOrDefault(u => u.UserId == permission.UserId);
                //var attendance = db.Attendances.FirstOrDefault(a => a.UserId == permission.UserId && a.Date == permission.Date);

                if (student == null)
                {
                    throw new InvalidOperationException($"Student with UserId '{permission.UserId}' does not exist.");
                }
                var schedule = db.Schedules.FirstOrDefault(s => s.Date.Date == permission.Date.Date && s.TrackId == student.TrackId);
                if (schedule == null)
                {
                    throw new InvalidOperationException($"Schedule for UserId '{permission.UserId}' on date '{permission.Date}' does not exist.");
                }
                //if (permission.State == PermissionState.Approved)
                //{
                //    if (attendance != null)
                //    {
                //        attendance.Status = permission.Type == PermissionType.Late ? AttendaneStatus.LateWithPermission : AttendaneStatus.AbsentWithPermission;
                //        db.Attendances.Update(attendance);
                //    }
                //    else
                //    {

                //        throw new InvalidOperationException($"Attendance record for UserId '{permission.UserId}' on date '{permission.Date}' does not exist.");
                //    }
                //}

                db.Permissions.Update(permission);

                db.SaveChanges();
            }
            catch (ArgumentNullException)
            {
                throw; 
            }
            catch (InvalidOperationException)
            {
                throw; 
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while updating permission.", ex);
            }
        }
       
    }
}
