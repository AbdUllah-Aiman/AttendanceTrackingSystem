using AttendanceTrackingSystem.IRepository;
using AttendanceTrackingSystem.Models;
using AttendanceTrackingSystem.Repository;
using AttendanceTrackingSystem.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Metrics;

namespace AttendanceTrackingSystem.Controllers
{
    [Authorize]
    [Authorize(Roles = "Security")]
    public class AttendanceController : Controller
    {
        IRepoAttendance repoAttendance;
        IRepoStudent repoStudent;
        IRepoTrack repoTrack;
        IRepoStudentAttendance repoStudentAttendance;
        IRepoSchedule repoSchedule;
        IRepoUser repoUser;
        public AttendanceController(IRepoAttendance _repoAttendance,IRepoStudent _repoStudent,IRepoTrack _repoTrack,IRepoStudentAttendance _repoStudentAttendance,IRepoSchedule _repoSchedule,IRepoUser _repoUser)
        {
            repoAttendance = _repoAttendance;
            repoStudent = _repoStudent;
            repoTrack = _repoTrack;
            repoStudentAttendance = _repoStudentAttendance;
            repoSchedule = _repoSchedule;
            repoUser = _repoUser;
        }
        public IActionResult Attendance()
        {
            //var obj = new Schedule()
            //{
            //    StartPeriod = TimeOnly.FromDateTime(DateTime.Now),
            //    EndPeriod = TimeOnly.FromDateTime(DateTime.Now).AddHours(7),
            //    TrackId = 1,
            //    Date = DateTime.Now

            //};
            //repoSchedule.Add(obj);
            //var obj2 = new Schedule()
            //{
            //    StartPeriod = TimeOnly.FromDateTime(DateTime.Now),
            //    EndPeriod = TimeOnly.FromDateTime(DateTime.Now).AddHours(7),
            //    TrackId = 2,
            //    Date = DateTime.Now
            //};
            //repoSchedule.Add(obj2);


            //var obj = new Schedule()
            //{
            //    StartPeriod = TimeOnly.FromDateTime(DateTime.Now),
            //    EndPeriod = TimeOnly.FromDateTime(DateTime.Now).AddHours(7),
            //    TrackId = 1,
            //    Date = DateTime.Now

            //};
            //repoSchedule.Add(obj);
            //var obj2 = new Schedule()
            //{
            //    StartPeriod = TimeOnly.FromDateTime(DateTime.Now),
            //    EndPeriod = TimeOnly.FromDateTime(DateTime.Now).AddHours(7),
            //    TrackId = 2,
            //    Date = DateTime.Now
            //};
            //repoSchedule.Add(obj2);



            ViewModel.AttendanceViewModel modal = new ViewModel.AttendanceViewModel();
            modal.Students = repoStudent.getAll();
            modal.Tracks = repoTrack.getAll();
            return View(modal);
        }
        public IActionResult AttendanceStudent()
        {
            ViewModel.AttendanceViewModel modal = new ViewModel.AttendanceViewModel();
            modal.Students = repoStudent.getAll().Where(a=>a.IsApproved==Approve.Accepted).ToList();
            modal.Tracks = repoTrack.getAll();
            modal.UserId = repoStudentAttendance.GetAttendanceForToday();
            modal.Attendances = repoAttendance.getAll();
            if(repoSchedule.checkSechduleToday() != true)
            {
                ViewBag.CheckSchedule = "It appears that there is no schedule planned for today.";
            }
            return PartialView("AttendanceStudent", modal);
        }
        public IActionResult GetStudentsByTrack(int id)
        {
            var scheduleForTrack = repoSchedule.getAll().FirstOrDefault(a => a.TrackId == id && a.Date.Date == DateTime.Now.Date && (a.Type != ScheduleType.Funday&&a.Type!=ScheduleType.Holiday));

            if (scheduleForTrack == null)
            {
                ViewBag.CheckSchedleForTrack = "It appears that there is no schedule planned for today."; 
            }
            var Students = repoStudent.getAll();
            if (id == 0)
            {
                 Students = repoStudent.getAll();
            }
            else
            {
                 Students = repoStudent.getAll().Where(a => a.TrackId == id).ToList();
            }
            
            var studentId = repoStudentAttendance.GetAttendanceForToday();

            ViewModel.AttendanceViewModel obj = new ViewModel.AttendanceViewModel();
            obj.UserId = studentId;
            obj.Students = Students;
			obj.Attendances = repoAttendance.getAll();

            if (repoSchedule.checkSechduleTodayFoeTrack(id) != true)
            {
                ViewBag.CheckSchedule = "It appears that there is no schedule planned for today.";
            }

            return PartialView("_AttendanceStudentPartialView",obj);

        }
        public IActionResult AddAttendanceRecourd(int id , TimeOnly time)
        {
            StudentAttendance recourd;
            var std = repoStudent.getById(id);
            // get the recourd of the same day and the same student


            recourd = repoStudentAttendance.GetByUserIdAndDate(id);
   //         if(recourd == null)
   //         {
   //             repoAttendance.Add(new Models.Attendance()
   //             {
			//		Date = DateTime.Now,
			//		UserId = id,
			//		Status = AttendaneStatus.Absent,
			//		CheckIn = TimeOnly.FromDateTime(DateTime.Parse("0:00")),
			//		CheckOut = TimeOnly.FromDateTime(DateTime.Parse("0:00")),
			//	});
			//     recourd = repoStudentAttendance.getAll().FirstOrDefault(a => a.UserId == id && a.Date == (DateTime.Now));
			//}

            var count = repoStudentAttendance.CheckCountOfAbsentAndLateDays(id) ;
            var permission = repoStudentAttendance.HavePermission(id);

            if (repoStudentAttendance.IsLate(id, recourd?.SchduleId))
            {
                recourd.Status = AttendaneStatus.Late;
                if (count <= 1)
                {
                    recourd.currentDegree = std.StudentDegree;
                    recourd.minDegree = 0;

                } else if (count > 1 && count < 5 && permission)
                {
                    recourd.currentDegree = std.StudentDegree - 5;
                    std.StudentDegree = std.StudentDegree - 5;
					recourd.minDegree = 5;

				} else if (count >= 5 && count < 8 && permission)
                {
                    recourd.currentDegree = std.StudentDegree - 10;
                    std.StudentDegree = std.StudentDegree - 10;
					recourd.minDegree = 10;

				} else if (count >= 8 && count < 11 && permission)
                {
					recourd.currentDegree = std.StudentDegree - 15;
					std.StudentDegree = std.StudentDegree - 15;
					recourd.minDegree = 15;
				}
				else
                {
					recourd.currentDegree = std.StudentDegree - 25;
					std.StudentDegree = std.StudentDegree - 25;
					recourd.minDegree = 25;
				}
            }
            else
            {
				recourd.Status = AttendaneStatus.onTime;
			}
 
            recourd.CheckIn = TimeOnly.FromDateTime(DateTime.Now);
            repoStudentAttendance.SendWarningMsg(std.UserId, std.StudentDegree);
            repoStudentAttendance.Update(recourd);
            repoStudent.Update(std);

            return Ok();
        }
        public IActionResult DeleteAttendanceRecord(int id )
        {
			var std = repoStudent.getById(id);
			var recourd = repoStudentAttendance.getAll().FirstOrDefault(a => a.UserId == id && a.Date.Date == (DateTime.Now.Date));
			var count = repoStudentAttendance.CheckCountOfAbsentAndLateDays(id);
			var permission = repoStudentAttendance.HavePermission(id);

            if (recourd != null)
            {
				recourd.currentDegree = std.StudentDegree + recourd.minDegree;
				std.StudentDegree = std.StudentDegree + ((int?)recourd.minDegree ?? 0);
                recourd.CheckIn = TimeOnly.FromDateTime(DateTime.Parse("0:00"));
                recourd.CheckOut = TimeOnly.FromDateTime(DateTime.Parse("0:00"));
                recourd.minDegree = 0;
			}

            recourd.Status = AttendaneStatus.Absent;
            repoAttendance.Update(recourd);
			return Ok();
        }
        public IActionResult UpdateAttendanceRecourd(int id)
        {
            var obj = repoStudentAttendance.GetByUserIdAndDate(id);
            obj.CheckOut = TimeOnly.FromDateTime(DateTime.Now);
            
            repoStudentAttendance.UpdateByUserIdAndDate(id);
            return Ok();
        }

        
        //==============================================================================
        /*User*/
        //==============================================================================
        public IActionResult UserAttendance(string Type)
        {
            ViewModel.AttendanceViewModel modal = new ViewModel.AttendanceViewModel();
            
            modal.Attendances = repoAttendance.getAll();
            if (Type == "Instructor")
            {
                modal.UserId = repoAttendance.GetInstructorAttendanceForToday();
                modal.Users = repoUser.getAll().Where(a => a.UserType == "Instructor").ToList();
            }
            else
            {
                modal.UserId = repoAttendance.GetEmployeeAttendanceForToday();
                modal.Users = repoUser.getAll().Where(a => a.UserType == "Employee").ToList();
            }
           

            if (repoSchedule.checkSechduleToday() != true)
            {
                ViewBag.CheckSchedule = "It appears that there is no schedule planned for today.";
            }

            return PartialView("UserAttendance", modal);
        }
        public IActionResult AddEmployeeAttendance(int id)
        {
            var emp = repoUser.getById(id);
            var recourd = repoAttendance.getAll().FirstOrDefault(a => a.UserId == id && a.Date.Date == (DateTime.Now.Date));
           
            if(TimeOnly.FromDateTime(DateTime.Now).AddMinutes(15)< TimeOnly.FromDateTime(DateTime.Parse("9:00")))
            {
                recourd.Status = AttendaneStatus.onTime;
            }else if (TimeOnly.FromDateTime(DateTime.Now).AddMinutes(15) > TimeOnly.FromDateTime(DateTime.Parse("9:00")))
            {
                recourd.Status = AttendaneStatus.Late;
            }
            

            recourd.CheckIn = TimeOnly.FromDateTime(DateTime.Now);
           
            repoAttendance.Update(recourd);
            repoUser.Update(emp);

            return Ok();

        }
        public IActionResult UpdateUserAttendanceRecourd(int id)
        {
            var obj = repoAttendance.GetByUserIdAndDate(id);
            obj.CheckOut = TimeOnly.FromDateTime(DateTime.Now);

            repoStudentAttendance.UpdateByUserIdAndDate(id);
            return Ok();
        }
        public IActionResult DeleteUserAttendanceRecord(int id)
        {
            var emp = repoUser.getById(id);
            var recourd = repoAttendance.getAll().FirstOrDefault(a => a.UserId == id && a.Date.Date == (DateTime.Now.Date));
            

            if (recourd != null)
            {
                
                recourd.CheckIn = TimeOnly.FromDateTime(DateTime.Parse("0:00"));
                recourd.CheckOut = TimeOnly.FromDateTime(DateTime.Parse("0:00"));
              
            }

            recourd.Status = AttendaneStatus.Absent;
            repoAttendance.Update(recourd);
            return Ok();
        }
        public IActionResult AbsentForAll()
        {
            var attendances = repoStudentAttendance.getAll()
                .Where(a => a.Date.Date == DateTime.Now.Date)
                .ToList();

            foreach (var attendance in attendances)
            {
                var id = attendance.UserId;
                var std = repoStudent.getById(id);
                var count = repoStudentAttendance.CheckCountOfAbsentAndLateDays(id);
                var permission = repoStudentAttendance.HavePermission(id);

                if (count <= 1)
                {
                    attendance.currentDegree = std.StudentDegree;
                    attendance.minDegree = 0;

                }
                else if (count > 1 && count < 5 && permission)
                {
                    attendance.currentDegree = std.StudentDegree - 5;
                    std.StudentDegree = std.StudentDegree - 5;
                    attendance.minDegree = 5;

                }
                else if (count >= 5 && count < 8 && permission)
                {
                    attendance.currentDegree = std.StudentDegree - 10;
                    std.StudentDegree = std.StudentDegree - 10;
                    attendance.minDegree = 10;

                }
                else if (count >= 8 && count < 11 && permission)
                {
                    attendance.currentDegree = std.StudentDegree - 15;
                    std.StudentDegree = std.StudentDegree - 15;
                    attendance.minDegree = 15;
                }
                else
                {
                    attendance.currentDegree = std.StudentDegree - 25;
                    std.StudentDegree = std.StudentDegree - 25;
                    attendance.minDegree = 25;
                }


                attendance.CheckIn = TimeOnly.FromDateTime(DateTime.Now);
                repoStudentAttendance.SendWarningMsg(std.UserId, std.StudentDegree);
                repoStudentAttendance.Update(attendance);
                repoStudent.Update(std);
            }
            return Ok();



        }




    }
}
