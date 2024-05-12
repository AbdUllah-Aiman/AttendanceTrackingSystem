using AttendanceTrackingSystem.IRepository;
using AttendanceTrackingSystem.Models;
using AttendanceTrackingSystem.Repository;
using AttendanceTrackingSystem.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AttendanceTrackingSystem.Controllers
{
    [Authorize]
	[Authorize(Roles = "Student,0")]
	public class StudentController : Controller
    {
        IRepoAttendance repoAttendance;
        IRepoStudentAttendance repoStudentAttendance;
        IRepoStudent repoStudent;
        IRepoMsg repoMsg;
        IRepoPermission repoPermission;
        IRepoAccount repoAccount;
    
        private readonly ILogger<StudentController> _logger;

        public StudentController(ILogger<StudentController> logger,IRepoStudentAttendance _repoStudentAttendance,IRepoAccount _repoAccount,IRepoPermission _repoPermission, IRepoAttendance _repoAttendance, IRepoStudent _repoStudent,IRepoMsg _repoMsg)
        {
            repoAttendance = _repoAttendance;
            repoStudent = _repoStudent;
            repoMsg = _repoMsg;
            repoPermission= _repoPermission; 
            repoAccount= _repoAccount;
            repoStudentAttendance = _repoStudentAttendance;
            _logger = logger;

        }


        public IActionResult Home(int id, DateTime? endDate)
        {
            try
            {
                id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var startDate = DateTime.Today.AddMonths(-1);
                endDate ??= DateTime.Today;
                var model = repoStudent.getById(id);
                if (model != null)
                {
                    ViewBag.stdDegree = model.StudentDegree;
                }

                var userAttendance = repoStudentAttendance.GetStudentAttendance(id, startDate, endDate.Value);
                var scheduleDates = repoStudentAttendance.GetStudentScheduleDates(id, startDate, endDate.Value);
                int absentCount = scheduleDates.Count(date => !userAttendance.Any(a => a.Date.Date == date.Date));
                int lateCount = userAttendance.Count(a => a.CheckIn > new TimeOnly(9, 0));
                int onTimeCount = userAttendance.Count(a => a.CheckIn <= new TimeOnly(9, 0));
                List<Msg> userMessages = repoMsg.getAll(id);
                List<Permission> userPermissions = repoPermission.getAllById(id);
                var attendanceSummaryViewModel = new AttendanceSummaryViewModel
                {
                    UserId = id,
                    LateCount = lateCount,
                    AbsentCount = absentCount,
                    OnTimeCount = onTimeCount,
                    AttendanceStudentData = userAttendance,
                    StartDate = startDate,
                    userMessages = userMessages,
                    UserPermissions = userPermissions,
                };
                var studentSchedule = repoStudent.GetFutureStudentSchedule(id);
                var lateOrAbsentDates = repoAttendance.GetLateOrAbsentDates(id);
                attendanceSummaryViewModel.StudentSchedule = studentSchedule;
                ViewBag.TrackName = repoStudent.GetTrackNameByUserId(id);
                ViewBag.Supervisor = repoStudent.GetSupervisorByStudentId(id);
                ViewBag.LateOrAbsentDates = lateOrAbsentDates;
                return View(attendanceSummaryViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in Home action");
                return View("Error");
            }
        }





        public IActionResult MakePermission(int Id)
        {
            if(Id==0 )
				Id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            ViewBag.userId = Id;
            //var super= repoStudent.GetSupervisorByStudentId(Id);
            var super = 2;
            if(super != null)
            {

				TempData["SuperVisorId"] = repoStudent.GetSupervisorByStudentId(Id).UserId;

			}
			return View();
        }
        [HttpPost]

		public IActionResult MakePermission(Permission permission)
		{
			permission.UserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
			ModelState.Remove("User");
			ModelState.Remove("UserId");
			if (ModelState.IsValid)
			{
				try
				{
					//int SId = (TempData["SuperVisorId"] as int?) ?? -1;
                    int SId = 2;
					if (SId != -1)
					{
						string scheduleCheckResult = repoPermission.CheckSchedule(permission.UserId, permission.Date);

						if (scheduleCheckResult != "Schedule found")
						{
							ModelState.AddModelError("", scheduleCheckResult);
							return View(permission);
						}

						bool permissionExists = repoPermission.CheckPermission(permission.UserId, permission.Date);

						if (permissionExists)
						{
							ModelState.AddModelError("", "Permission already exists for this user and date.");
							return View(permission);
						}

						var message = new Msg
						{
							UserId = SId,
							Title = "Permission Request",
							Description = "New permission request from student " + permission.UserId.ToString(),
							Date = DateTime.Now,
							IsRead = false
						};

						repoPermission.Add(permission);
						repoMsg.Add(message);
					}
					else
					{
						ModelState.AddModelError("", "SupervisorId is null. Unable to send permission request.");
						return View(permission);
					}

					return RedirectToAction("Home");
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", "An error occurred while processing the permission request.");
					return View(permission);
				}
			}
			else
			{
				return View(permission);
			}
		}



		[HttpPost]
        public IActionResult DeletePermission(int permissionId)
        {
            try
            {
                repoPermission.Delete(permissionId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Handle any exceptions here
                return Json(new { success = false, message = "Failed to delete permission: " + ex.Message });
            }
        }





        public IActionResult Index()
        {
            return View();
        }
    }
}


