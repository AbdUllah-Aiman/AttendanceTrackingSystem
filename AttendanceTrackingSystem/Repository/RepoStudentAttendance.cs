using AttendanceTrackingSystem.IRepository;
using AttendanceTrackingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceTrackingSystem.Repository
{
    public class RepoStudentAttendance : IRepoStudentAttendance
    {
        AttendanceDBContext db;
        public RepoStudentAttendance(AttendanceDBContext _db)
        {
            db = _db;
        }

        public void Add(StudentAttendance studentAttendance)
        {
            db.StudentAttendances.Add(studentAttendance);
            db.SaveChanges();
        }

        public void Delete(int id)
        {
            var obj = db.StudentAttendances.FirstOrDefault(a => a.AttendanceId == id);
            db.StudentAttendances.Remove(obj);
            db.SaveChanges();
        }

        public List<StudentAttendance> getAll()
        {
            return db.StudentAttendances.ToList();
        }
		public List<StudentAttendance> GetByStudentId(int studentId)
		{
			var result = db.StudentAttendances
				.Include(a => a.StudentSchdule)
				.Where(a => a.UserId == studentId && a.Date.Date == DateTime.Now.Date)
				.ToList();
            return result;
		}

		public List<StudentAttendance> getStudentAttendaces()
		{
            var studentAttendances = db.StudentAttendances.ToList();

			foreach (var item in studentAttendances)
			{
				var student = db.Students.FirstOrDefault(a => a.UserId == item.UserId);
				item.currentDegree = student?.StudentDegree;
			}
		
            return db.StudentAttendances.OrderByDescending(s=>s.Date).ToList();
		}

		public StudentAttendance getById(int id)
        {
            return db.StudentAttendances.FirstOrDefault(a => a.AttendanceId == id);
        }

        public void Update(StudentAttendance studentAttendance)
        {
            db.StudentAttendances.Update(studentAttendance);
            db.SaveChanges();
        }
		public List<int> GetAttendanceForToday()
		{
			DateTime today = DateTime.Today.Date;
			var attendanceIds = db.StudentAttendances
				.Where(a => a.Date.Date ==today && a.Status != 0)
				.Select(a => a.UserId)
				.ToList();

			return attendanceIds;
		}

		public void DeleteByUserIdAndDate(int id)
        {
            var obj = db.Attendances.FirstOrDefault(a=>a.UserId == id && a.Date.Date== DateTime.Today.Date);
            db.Attendances.Remove(obj);
            db.SaveChanges();
        }
        public void UpdateByUserIdAndDate(int id)
        {
            var obj = db.Attendances.FirstOrDefault(a => a.UserId == id && a.Date.Date ==DateTime.Today.Date);
            db.Attendances.Update(obj);
            db.SaveChanges();
        }
        public StudentAttendance GetByUserIdAndDate(int id)
        {
            var obj = db.StudentAttendances.FirstOrDefault(a => a.UserId == id && a.Date.Date == DateTime.Today.Date);
            return obj;
        }

		public int CheckCountOfAbsentAndLateDays(int id)
		{
            return db.StudentAttendances.Where(a => a.UserId == id && a.Status!=AttendaneStatus.onTime).Count();

		}
        public bool HavePermission(int id)
        {
            return db.Permissions.Where(a => a.UserId == id && a.Date.Date == DateTime.Today.Date).Any();

        }
        public void UpdateDegree(int id,int newDegree)
        {
            var student = db.Students.FirstOrDefault(a => a.UserId == id);
            student.StudentDegree = newDegree;
            db.Students.Update(student);
            db.SaveChanges();
        }
        public bool IsLate(int id , int? scheduleId)
        {
            var schedule = db.Schedules.FirstOrDefault(a=>a.ScheduleId==scheduleId);
            var starttaime = schedule.StartPeriod;

            if(TimeOnly.FromDateTime(DateTime.Now) > starttaime.AddMinutes(15))
                return true;

            return false;
        }
        public void SendWarningMsg(int id , int degree)
        {
            if (degree <= 150 && degree >= 125)
            {
                Msg msg = new Msg()
                {
                    IsRead = false,
                    Date = DateTime.Now,
                    Description = "This is to inform you that you have received your first warning due to prolonged absence. Please make arrangements to resume your responsibilities promptly.",
                    Title= "First Warning - Absence Alert",
                    UserId = id,

				};
                db.Msgs.Add(msg);
            }
            else if (degree <= 125 && degree >= 85)
            {
				Msg msg = new Msg()
				{
					IsRead = false,
					Date = DateTime.Now,
					Description = "This message is to notify you that you have received a second warning due to continued absence. Please prioritize your responsibilities and ensure your presence accordingly.",
					Title = "Second Warning - Absence Alert",
					UserId = id,

				};
				db.Msgs.Add(msg);
			}
            else if (degree <85)
            {
                var student = db.Students.FirstOrDefault(a => a.UserId == id);
                student.IsApproved = Approve.Fired;
                db.Update(student);
                db.SaveChanges();
            }

        }
	

        //for only students

        public List<StudentAttendance> GetStudentAttendance(int studentId, DateTime startDate, DateTime endDate)
        {
            var student = db.Students.Find(studentId);
            if (student != null)
            {
              
                var schedules = db.Schedules
                    .Where(s => s.Date >= startDate && s.Date <= endDate)
                    .ToList();

            
                var studentAttendances = new List<StudentAttendance>();
                foreach (var schedule in schedules)
                {
                    var attendance = new StudentAttendance
                    {
                        Date = schedule.Date,
                        CheckIn = new TimeOnly(0, 0),
                        CheckOut = new TimeOnly(0, 0),
                        UserId = studentId,
                        StudentSchdule = schedule
                    };

                    studentAttendances.Add(attendance);
                }

                return studentAttendances;
            }
            return new List<StudentAttendance>(); 
        }

        public List<DateTime> GetStudentScheduleDates(int studentId, DateTime startDate, DateTime endDate)
        {
            var studentSchedules = db.Schedules
                .Where(s => s.StudentAttendances.Any(sa => sa.UserId == studentId) && s.Date >= startDate && s.Date <= endDate)
                .Select(s => s.Date)
                .ToList();

            return studentSchedules;
        }


    }
}
