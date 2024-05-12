using AttendanceTrackingSystem.IRepository;
using AttendanceTrackingSystem.Models;
using System.Security;

namespace AttendanceTrackingSystem.Repository
{
	public class RepoSchedule : IRepoSchedule
	{
		AttendanceDBContext db;
		public RepoSchedule(AttendanceDBContext _db)
		{
			db = _db;
		}

		public void Add(Schedule schedule)
		{
			db.Schedules.Add(schedule);
			db.SaveChanges();

			if (schedule.Type != ScheduleType.Funday && schedule.Type != ScheduleType.Holiday)
			{
				CreateAttendanceRecords(schedule.ScheduleId, schedule.Date);
			}

		}

		public void Delete(int id)
		{
			var obj = db.Schedules.FirstOrDefault(a => a.ScheduleId == id);
			db.Schedules.Remove(obj);
			db.SaveChanges();
		}

		public List<Schedule> getAll()
		{
			return db.Schedules.ToList();
		}

		public Schedule getById(int id)
		{
			return db.Schedules.FirstOrDefault(a => a.ScheduleId == id);
		}

		public void Update(Schedule schedule)
		{

			db.Schedules.Update(schedule);
			db.SaveChanges();

			if (schedule.Type == ScheduleType.Funday || schedule.Type == ScheduleType.Holiday)
			{
				var ids = db.Students
					.Where(a => a.TrackId == schedule.TrackId).Select(a => a.UserId).ToList();

				if (ids.Count() > 0)
				{
					var attendanceRecordsToDelete = db.StudentAttendances
				   .Where(a => ids.Contains(a.UserId) && a.Date.Date == schedule.Date.Date)
				   .ToList();
					if (attendanceRecordsToDelete.Count() > 0)
					{
						db.StudentAttendances.RemoveRange(attendanceRecordsToDelete);
						db.SaveChanges();

					}

				}

			}


		}
		// Abdullah
		public List<Schedule> GetWeeklyScheduleForTrack(int trackId)
		{
			var today = DateTime.Today;
			var saturday = today;
			if (today.DayOfWeek != DayOfWeek.Saturday)
			{
				saturday = today.AddDays(-1 - (int)today.DayOfWeek);
			}
			var friday = saturday.AddDays(6);

			var weeklySchedule = db.Schedules
				.Where(s => s.TrackId == trackId && s.Date >= saturday && s.Date <= friday)
				.OrderBy(a => a.Date).ToList();
			return weeklySchedule;
		}
		public List<Schedule> GetAllScheduleForTrack(int trackId, bool FromToday)
		{
			var today = DateTime.Today;
			var saturday = today;

			if (today.DayOfWeek != DayOfWeek.Saturday && !FromToday)
			{
				saturday = today.AddDays(-1 - (int)today.DayOfWeek);
			}
			var WholeSchedule = db.Schedules
				.Where(s => s.TrackId == trackId && s.Date >= saturday)
				.OrderBy(a => a.Date).ToList();
			return WholeSchedule;
		}


		public int CheckStudentStatus(TimeOnly arriveTime, TimeOnly timeInScedule, int id)
		{
			var obj = db.Schedules.FirstOrDefault(a => a.ScheduleId == id);

			if (obj.StartPeriod.AddMinutes(15) < arriveTime)
			{
				return 1;
			}
			else return 0;

		}

		public void CreateAttendanceRecords(int scheduleId, DateTime date)
		{

			var existingStudentAttendances = db.StudentAttendances
				.Any(sa => sa.SchduleId == scheduleId && sa.Date == date);

			if (!existingStudentAttendances)
			{
				CreateStudentAttendanceRecords(scheduleId, date);
			}

			var existingInstructorAttendance = db.Attendances
				.Any(a => (a.User.UserType == "Instructor" || a.User.UserType == "Employee") && a.Date.Date == date.Date);

			if (!existingInstructorAttendance)
			{
				CreateInstructorAndEmployeeAttendanceRecords(date);
			}
		}

		public void CreateStudentAttendanceRecords(int scheduleId, DateTime date)
		{
			var schedule = db.Schedules.FirstOrDefault(a => a.ScheduleId == scheduleId);
			var students = db.Students.Where(s => s.TrackId == schedule.TrackId && s.IsApproved == Approve.Accepted).ToList();

			foreach (var student in students)
			{
				var attendance = new StudentAttendance
				{
					Date = date,
					SchduleId = scheduleId,
					UserId = student.UserId,
					Status = AttendaneStatus.Absent,
					CheckIn= TimeOnly.FromDateTime(DateTime.Parse("0:00")),
					CheckOut = TimeOnly.FromDateTime(DateTime.Parse("0:00")),
				};
				db.StudentAttendances.Add(attendance);
			}

			db.SaveChanges();
		}

		public void CreateInstructorAndEmployeeAttendanceRecords(DateTime date)
		{
			var instructorsAndEmployees = db.Users.Where(u => u.UserType == "Instructor" || u.UserType == "Employee").ToList();

			foreach (var user in instructorsAndEmployees)
			{
				var attendance = new Attendance
				{
					Date = date,
					UserId = user.UserId,
					Status = AttendaneStatus.Absent,
					CheckIn = TimeOnly.FromDateTime(DateTime.Parse("0:00")),
					CheckOut = TimeOnly.FromDateTime(DateTime.Parse("0:00")),
				};
				db.Attendances.Add(attendance);
			}

			db.SaveChanges();
		}

		public bool checkSechduleToday()
		{
			var obj = db.Schedules.FirstOrDefault(a => a.Date.Date == DateTime.Now.Date && (a.Type == ScheduleType.Offline || a.Type == ScheduleType.Online));
			if (obj != null)
				return true;
			else return false;
		}
		public bool checkSechduleTodayFoeTrack(int id)
		{
			var obj = db.Schedules.FirstOrDefault(a => a.Date.Date == DateTime.Now.Date && (a.Type == ScheduleType.Offline || a.Type == ScheduleType.Online) && a.TrackId == id);
			if (obj != null)
				return true;
			else return false;
		}

		public List<Schedule> CreateNextWeekScheduleTemplate(int trackId)
		{
			DateTime maxDate = DateTime.Today;
			List<Schedule> schedules = db.Schedules.Where(a => a.TrackId == trackId).ToList();
			if (schedules.Count() != 0)
				maxDate = schedules.Max(a => a.Date);

			DateTime startDate = maxDate.AddDays(6 - (int)maxDate.DayOfWeek);

			List<Schedule> upcomingWeekSchedules = new List<Schedule>();
			for (int i = 0; i < 7; i++)
			{
				DateTime currentDate = startDate.AddDays(i);
				Schedule schedule = new Schedule
				{
					TrackId = trackId,
					Date = currentDate,
					StartPeriod = TimeOnly.MinValue,
					EndPeriod = TimeOnly.MinValue
				};
				upcomingWeekSchedules.Add(schedule);
			}
			return upcomingWeekSchedules;
		}
		public bool IsScheduleExist(int trackId, DateTime date)
		{
			return db.Schedules.Any(a => a.TrackId == trackId && a.Date == date);
		}

		public void UpdateByTrackIdAndDate(Schedule schedule)
		{
			var obj = db.Schedules.FirstOrDefault(a => a.TrackId == schedule.TrackId && a.Date == schedule.Date);
			obj.StartPeriod = schedule.StartPeriod;
			obj.EndPeriod = schedule.EndPeriod;
			obj.Type = schedule.Type;
			db.SaveChanges();
		}
		public void AddOrReplaceSchedule(Schedule schedule)
		{
			if (IsScheduleExist(schedule.TrackId, schedule.Date))
			{
				UpdateByTrackIdAndDate(schedule);
			}
			else
			{
				db.Schedules.Add(schedule);
				db.SaveChanges();
				if (schedule.Type != ScheduleType.Funday && schedule.Type != ScheduleType.Holiday)
				{
					CreateAttendanceRecords(schedule.ScheduleId, schedule.Date);
				}
			}
				
			
		}
	}
}
