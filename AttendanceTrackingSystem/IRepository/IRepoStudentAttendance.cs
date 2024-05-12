using AttendanceTrackingSystem.Models;

namespace AttendanceTrackingSystem.IRepository
{
    public interface IRepoStudentAttendance
    {
        public List<StudentAttendance> getAll();
        public StudentAttendance getById(int id);
        public void Add(StudentAttendance studentAttendance);
        public void Update(StudentAttendance studentAttendance);

        public List<StudentAttendance> getStudentAttendaces();
        public List<StudentAttendance> GetByStudentId(int studentId);
		public void Delete(int id);
        public List<int> GetAttendanceForToday();
        public void DeleteByUserIdAndDate(int id);
        public void UpdateByUserIdAndDate(int id);
        public StudentAttendance GetByUserIdAndDate(int id);
        public int CheckCountOfAbsentAndLateDays(int id);
        public void UpdateDegree(int id, int newDegree);
        public bool IsLate(int id, int? scheduleId);
		public bool HavePermission(int id);
        public void SendWarningMsg(int id, int degree);
	

        public List<StudentAttendance> GetStudentAttendance(int studentId, DateTime startDate, DateTime endDate);

        public List<DateTime> GetStudentScheduleDates(int studentId, DateTime startDate, DateTime endDate);


    }
}
