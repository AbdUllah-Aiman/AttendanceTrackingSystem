﻿using AttendanceTrackingSystem.IRepository;
using AttendanceTrackingSystem.Models;
using AttendanceTrackingSystem.Repository;
using AttendanceTrackingSystem.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Metrics;

namespace AttendanceTrackingSystem.Controllers
{
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
            return View();
        }
        public IActionResult AttendanceStudent()
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



            ViewModel.AttendanceViewModel modal = new ViewModel.AttendanceViewModel();
            modal.Students = repoStudent.getAll();
            modal.Tracks = repoTrack.getAll();
            modal.UserId = repoStudentAttendance.GetAttendanceForToday();
            modal.Attendances = repoAttendance.getAll();
            if(repoSchedule.checkSechduleToday() != true)
            {
                ViewBag.CheckSchedule = "It appears that there is no schedule planned for today.";
            }

            return View (modal);
        }
        public IActionResult GetStudentsByTrack(int id)
        {
            var Students = repoStudent.getAll().Where(a=>a.TrackId == id).ToList();
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
            var std = repoStudent.getById(id);
            var recourd = repoStudentAttendance.getAll().FirstOrDefault(a=>a.UserId==id && a.Date.Date == (DateTime.Now.Date));
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
        public IActionResult Attendance(string Type)
        {
            ViewModel.AttendanceViewModel modal = new ViewModel.AttendanceViewModel();
            modal.UserId = repoAttendance.GetEmployeeAttendanceForToday();
            modal.Attendances = repoAttendance.getAll();
            modal.Users = repoUser.getAll().Where( a=>a.UserType == Type).ToList();

            return PartialView("UserAttendance", modal);
        }





    }
}