using AttendanceTrackingSystem.IRepository;
using AttendanceTrackingSystem.Repository;
using AttendanceTrackingSystem.ViewModel;
using AttendanceTrackingSystem.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace AttendanceTrackingSystem.Controllers
{
	public class AccountController : Controller
	{
		IRepoAccount repoAccount;
		IRepoTrack repoTrack;
		IRepoStudent repoStudent;
		IRepoInstructor repoInstructor;
		IRepoEmployee repoEmployee;
		public AccountController(IRepoAccount _repoAccount, IRepoTrack _repoTrack, IRepoStudent _repoStudent, IRepoInstructor _repoInstructor, IRepoEmployee _repoEmployee)
		{
			repoAccount = _repoAccount;
			repoTrack = _repoTrack;
			repoStudent = _repoStudent;
			repoInstructor = _repoInstructor;
			repoEmployee = _repoEmployee;
		}
		public IActionResult Index()
		{
			return View();
		}
		public IActionResult Login()
		{
			return View();
		}
		[HttpPost]
		public IActionResult Login(LoginViewModel model)
		{
			
            if (ModelState.IsValid)
			{
				User user = repoAccount.GetUser(model.Email, model.Password);
				if (user != null)
				{
                    if (user.IsApproved == Approve.Accepted)
                    {
    					Claim claim1 = new Claim(ClaimTypes.Name, user.Name);
    					Claim claim2;
    					if (user.UserType != "Employee")
    					{
    						if (repoInstructor.IsSuperisor(user.UserId))

                                claim2 = new Claim(ClaimTypes.Role, "Supervisor");
                            else
                                claim2 = new Claim(ClaimTypes.Role, user.UserType);
                        }
                        else
                        {
                            claim2 = new Claim(ClaimTypes.Role, repoAccount.GetEmployeeType(user.UserId));
                        }
                        Claim claim3 = new Claim(ClaimTypes.Email, user.Email);
                        Claim claim4 = new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString());
                       // Claim claim5 = new Claim(ClaimTypes.Uri, user.ImgUrl?.ToString() ?? "");
                        ClaimsIdentity identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                        identity.AddClaim(claim1);
                        identity.AddClaim(claim2);
                        identity.AddClaim(claim3);
                        identity.AddClaim(claim4);
                       // identity.AddClaim(claim5);
                        ClaimsPrincipal principal = new ClaimsPrincipal(identity);
                         HttpContext.SignInAsync(principal);
                        TempData["role"] = claim2.Value;
                        ViewBag.ImgUrl = user.ImgUrl ?? "~images/profile/user.png";
						// if the user.imgurl doesn't start with /images/profile/ then it's a full url
						TempData["img"] = setImgUrl(user?.ImgUrl);
						
                       
                        EditProfileViewModel model1 = new EditProfileViewModel
                        {
                            Name = user.Name,
                            Email = user.Email,
                            Phone = user.Phone,
                           // ImgUrl = user.ImgUrl ?? "/images/user.png",
                            OldPassword = user.Password
                        };
						var a = user.UserType;
                        if (user.UserType == "Employee")
                            ViewBag.role = repoAccount.GetEmployeeType(user.UserId).ToString();
                        else
                            ViewBag.role = user.UserType;

                        switch (user.UserType)
                        {
                            case "Student":
							case "0":
                                return RedirectToAction("Home", "Student");
                            case "Instructor":
                            case "Supervisor":
                                return RedirectToAction("Home", "Home");
                            case "Employee":
                                switch (repoAccount.GetEmployeeType(user.UserId))
                                {
                                    case "Admin":
                                        return  RedirectToAction("Home", "Admin");
                                        //return Redirect("~/Admin/Home");

                                    case "Security":
                                    case "StudentAffairs":

                                        return RedirectToAction("Home", "Home");


                                    default:
                                        return RedirectToAction("Index", "Home"); // Default fallback
                                }
                            // Add more cases for other user types as needed...
                            default:
                                return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        TempData["ErrorMessageApprove"] = "Your account is not approved yet. Please try again later.";
                        return RedirectToAction("Login");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Invalid Email Or password");
                }
            }

			return View();
		}
		private string setImgUrl(string? imgUrl)
		{
			if (imgUrl != null)
			{
				if (!imgUrl.StartsWith("/images/profile/"))
				{
					return "/images/profile/" + imgUrl;
				}
				else
				{
					return imgUrl;
				}
			}
			else
			{
				return "/images/profile/user.png";
			}
		}

		public async Task<IActionResult> Logout()
		{
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return RedirectToAction("Login");
		}
		[Authorize]
		public IActionResult Profile()
		{
			int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
			string role = User.FindFirst(ClaimTypes.Role)?.Value.ToString();
			User model = repoAccount.GetUserByid(userId);
			EditProfileViewModel model1 = new EditProfileViewModel
			{
				Name = model.Name,
				Email = model.Email,
				Phone = model.Phone,
				ImgUrl = model.ImgUrl ?? "/images/profile/user.png",
				OldPassword = model.Password
			};
			ViewBag.img = model1.ImgUrl;
		
			switch (role)
			{
				case "Student":
					Student student = repoStudent.getById(userId);

					StudentEditProfileViewModel studentEditProfileViewModel = new StudentEditProfileViewModel
					{
						Name = model.Name,
						Email = model.Email,
						Phone = model.Phone,
						ImgUrl = setImgUrl(model.ImgUrl),
						OldPassword = model.Password,	
						StudentDegree = student.StudentDegree,
						StudentUniversity = student.StudentUniversity,
						StudentFaculity = student.StudentFaculity,
						StudentGraduationYear = student.StudentGraduationYear,
						StudentSpecialization = student.StudentSpecialization,
						TrackId = student.TrackId,
						TrackName = repoTrack.getById(student.TrackId).Name,
						TrackSupervisor = repoTrack.getTrackSupervisor(student.TrackId).Name
					};
					return View(studentEditProfileViewModel);
				case "Instructor":
				case "Supervisor":
					Instructor instructor = repoInstructor.getById(userId);
					InstructorEditProfileViewModel instructorEditProfileViewModel = new InstructorEditProfileViewModel
					{
						Name = model.Name,
						Email = model.Email,
						Phone = model.Phone,
						ImgUrl = setImgUrl(model.ImgUrl),
						OldPassword = model.Password,
						InstructorSalary = instructor.InstructorSalary,
						TrackNames = string.Join(", ", instructor.Tracks.Select(t => t.Name)),
						SupervisedTrack = instructor.Tracks.FirstOrDefault(t => t.SupervisorId == userId)?.Name
					};
					return View(instructorEditProfileViewModel);
				case "StudentAffairs":
				case "Security":
				case "Admin":
					Employee employee = repoEmployee.getById(userId);
					EmployeeEditProfileViewModel employeeEditProfileViewModel = new EmployeeEditProfileViewModel
					{
						Name = model.Name,
						Email = model.Email,
						Phone = model.Phone,
						ImgUrl = setImgUrl(model.ImgUrl),
						OldPassword = model.Password,
						EmployeeSalary = employee.EmployeeSalary,
						EmployeeType = employee.EmployeeType
					};
					return View(employeeEditProfileViewModel);
				// Add more cases for other user types as needed...
				default:
					return View(model1);
			}
		}
		[Authorize]
		[HttpPost]
		public IActionResult updateImage(IFormFile ImgUrl)
		{
			int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
			string imgPath = string.Empty;
			if (ImgUrl != null)
			{
				imgPath = $"{userId}.{ImgUrl.FileName.Split(".").Last()}";
				using (var fs = new FileStream(("wwwroot/images/profile/" + imgPath), FileMode.Create))
				{
					ImgUrl.CopyTo(fs);
				}
			}
			if (imgPath != string.Empty)
			{
				repoAccount.UpdateImage( imgPath, userId);
				TempData.Peek("img");
				TempData["img"] = "/images/profile/" + imgPath;
			}
			//var cacheBuster = DateTime.UtcNow.Ticks;
			//Response.Headers["Cache-Control"] = "no-cache, max-age=0";
			//TempData["CacheBuster"] = cacheBuster;
			return RedirectToAction("Profile");
		}
		[Authorize]
		[HttpPost]
		public IActionResult Profile(EditProfileViewModel model)
		{
			int c = 0;
			foreach (var key in ModelState.Keys)
			{
				foreach (var error in ModelState[key].Errors)
				{
					Console.WriteLine($"{key}: {error.ErrorMessage}");
					c++;
				}
			}
			if (c <= 1)
			{
				repoAccount.SaveEdit(model);
			}
			return RedirectToAction("Profile");
			//return RedirectToAction("Error", "Home");
		}

		[HttpGet]
		public IActionResult CheckPassword(string OldPassword)
		{
			int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
			User user = repoAccount.GetUserByid(userId);
			return Json(user.Password == OldPassword);

		}


		public IActionResult Signup()

		{

			var activeTracksWithStudentCount = repoTrack.GetActiveTracksWithStudentCount();
			ViewBag.ActiveTracks = activeTracksWithStudentCount;

			//ViewBag.Tracks = repoTrack.GetActiveTracks();


			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Signup(Student newStudent, IFormFile ImgUrl)
		{
			            var activeTracksWithStudentCount = repoTrack.GetActiveTracksWithStudentCount();


			try
			{
				if (ImgUrl == null)
					ModelState.Remove("ImgUrl");

				ModelState.Remove("Track");
				var tenYearsAgo = DateTime.Today.AddYears(-10);
				if (newStudent.StudentGraduationYear > DateTime.Today || newStudent.StudentGraduationYear < tenYearsAgo)
				{
					ModelState.AddModelError("StudentGraduationYear", "Please enter a date within the last 10 years.");
				}


                if (ModelState.IsValid)
                {
                    if (ImgUrl != null)
                    {
                        // get unique will be a 32-character long string composed of hexadecimal characters (0-9 and a-f)
                        string uniqueFileName = Guid.NewGuid().ToString("N");

                        
                        string fileExtension = Path.GetExtension(ImgUrl.FileName);

                       
                        string imageName = uniqueFileName + fileExtension;

                        string imagePath = Path.Combine("wwwroot/Images/Profile", imageName);

                        using (var fs = new FileStream(imagePath, FileMode.Create))
                        {
                            await ImgUrl.CopyToAsync(fs);
                        }

                   
                        newStudent.ImgUrl = imageName;
                    }



                    repoStudent.Add(newStudent);
                    return RedirectToAction("Login");
                }

              ViewBag.ActiveTracks = activeTracksWithStudentCount;




                return View(newStudent);

            }
            catch (Exception ex)
            {
                if (ex.InnerException is SqlException sqlException)
                {
                    if (sqlException.Number == 2601) 
                    {
                        ModelState.AddModelError("Email", "The email address is already in use.");
                      
                    }
                    else
                    {
                        ModelState.AddModelError("", ex.Message);
                        
                    }
                 
                   
                }
                else
                {
                    ModelState.AddModelError("", ex.Message);
                    
                }
                ViewBag.ActiveTracks = activeTracksWithStudentCount;
                return View(newStudent);
            }
        
        }

        public IActionResult AccessError()
        {
            return View("NotFound");
        }
        public IActionResult notFoundd()
        {
            return View();
        }

    }
}
