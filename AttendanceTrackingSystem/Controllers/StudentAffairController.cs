using AttendanceTrackingSystem.IRepository;
using AttendanceTrackingSystem.Repository;
using Microsoft.AspNetCore.Mvc;
using AttendanceTrackingSystem.Models;
using System.Net.Mime;
using System.IO;
using Microsoft.Extensions.Hosting.Internal;
using OfficeOpenXml;
using AttendanceTrackingSystem.ViewModel;
using System.ComponentModel.DataAnnotations;
using AttendanceTrackingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace AttendanceTrackingSystem.Controllers
{
    [Authorize]
    [Authorize(Roles = "StudentAffairs")]
    public class StudentAffairController : Controller
    {
        int pageSize = 10;
        private readonly IRepoStudent repoStudent;
        private readonly IRepoTrack repoTrack;
        private readonly IRepoAttendance repoAttendance;
        private readonly IRepoStudentAttendance repoStdAttendance;
        private readonly IRepoMsg repoMsg;
        private readonly IWebHostEnvironment _hostingEnvironment;
         private readonly IRepoPermission repoPermission;
        public StudentAffairController(IRepoStudent _repoStudent, IRepoTrack _repoTrack, IWebHostEnvironment hostingEnvironment,
            IRepoAttendance _repoAttendance,IRepoStudentAttendance _repoStdAttendance,IRepoPermission _repopermission , IRepoMsg _repoMsg)
        {
            repoStudent = _repoStudent;
            repoTrack = _repoTrack;
             repoPermission = _repopermission;
            repoMsg = _repoMsg;
            _hostingEnvironment = hostingEnvironment;
            repoAttendance = _repoAttendance;
            repoStdAttendance = _repoStdAttendance;

		}
		#region StudentsCrud

		public IActionResult Index(string? message, int? page, string? searchTerm)
		{
			TempData["SuccessMessage"] = message;

			var allStudents = repoStudent.getAll();


			if (!string.IsNullOrEmpty(searchTerm))
			{
				allStudents = allStudents.Where(s => s.Email.Contains(searchTerm.ToLower().Trim())).ToList();
			}

			if (!page.HasValue || page < 1)
			{
				page = 1;
			}

			int totalStudents = allStudents.Count();
			int totalPages = (int)Math.Ceiling((double)totalStudents / pageSize);

			if (page > totalPages)
			{
				page = totalPages;
			}

			var students = allStudents.Skip((page.Value - 1) * pageSize).Take(pageSize).ToList();

			ViewBag.TotalPages = totalPages;
			ViewBag.Page = page;
			ViewBag.SearchTerm = searchTerm;

			return View(students);
		}

		public IActionResult Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}
			var student = repoStudent.getById(id.Value);
			return PartialView("_Details", student);
		}

		[HttpGet]
		public async Task<IActionResult> Create(int? id)
		{

			if (id == null)
			{
				ViewBag.Header = "Add Student";
				ViewBag.Tracks = repoTrack.getAll();
				return PartialView("_CreateOrUpdatePartial", new Student());
			}
			else
			{
				ViewBag.Header = "Update Student";
				ViewBag.Tracks = repoTrack.getAll();
				var student = repoStudent.getById(id.Value);
				return PartialView("_CreateOrUpdatePartial", student);
			}

		}


		[HttpPost]
		/// <summary>
		/// Create a new student or update an existing one
		/// </summary>
		public IActionResult Create(Student student)
		{
			try
			{
				ModelState.Remove("Msgs");
				ModelState.Remove("UserType");
				ModelState.Remove("Track");

				student.UserType = "Student";
				student.IsApproved = Approve.Accepted;

				if (!ModelState.IsValid)
				{
					return BadRequest("Incorrect data input!");
				}

				if (student.UserId != 0)
				{
					if (student.Image != null && (student.Image.ContentType == "image/png" || student.Image.ContentType == "image/jpg" || student.Image.ContentType == "image/jpeg"))
					{
						SaveImageToDirectory(student);
						repoStudent.Update(student);
						return Ok(new { message = "Student has been updated successfully." });
					}
					else if (student.Image == null && repoStudent.IsImageExistedBefore(student.ImgUrl))
					{
						repoStudent.Update(student);
						return Ok(new { message = "Student has been updated successfully." });
					}
					else
						return BadRequest("Only PNG or JPEG image files are allowed.");
				}
				else
				{
					if (student.Image == null || student.Image.Length == 0)
						return BadRequest("You must include a file.");

					if (!(student.Image.ContentType == "image/png" || student.Image.ContentType == "image/jpg" || student.Image.ContentType == "image/jpeg"))
						return BadRequest("Only PNG or JPEG image files are allowed.");

					SaveImageToDirectory(student);
					repoStudent.Add(student);
					return Ok(new { message = "Student has been created successfully." });
				}
			}
			catch (ValidationException ex)
			{
				return BadRequest(ex.Message);
			}
			catch (Exception ex)
			{
				// Handle other unexpected exceptions
				return StatusCode(500, "An error occurred while processing the request.");
			}
		}

		[HttpPost]
		public IActionResult Delete(int id)
		{
			if (id != null)
			{
				repoStudent.Delete(id);
				return Ok(new { message = "Student has been deleted successfully." });

			}
			return BadRequest("No Student Found!");

		}
		[HttpPost]
		public IActionResult UploadStudents(IFormFile excelFile)
		{
			int studentAdded = 0;
			try
			{
				if (excelFile == null || excelFile.Length == 0)
				{
					return BadRequest("No file uploaded.");
				}
				else if (excelFile.ContentType != "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
				{
					return BadRequest("Only Excel files are allowed.");
				}

				ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
				using (var stream = new MemoryStream())
				{
					excelFile.CopyTo(stream);
					using (var package = new ExcelPackage(stream))
					{
						ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
						int rowCount = worksheet.Dimension.Rows;

						for (int row = 2; row <= rowCount; row++)
						{
							if (String.IsNullOrEmpty(worksheet.Cells[row, 1].Value?.ToString()))
							{
								break;
							}

							string name = worksheet.Cells[row, 1].Value?.ToString();
							string email = worksheet.Cells[row, 2].Value?.ToString();
							string phone = worksheet.Cells[row, 3].Value?.ToString();
							string password = worksheet.Cells[row, 4].Value?.ToString();
							int isApproved = int.Parse(worksheet.Cells[row, 5].Value?.ToString());
							int userType = int.Parse(worksheet.Cells[row, 6].Value?.ToString());
							string imgUrl = worksheet.Cells[row, 7].Value?.ToString();
							int studentDegree = int.Parse(worksheet.Cells[row, 8].Value?.ToString());
							string studentUniversity = worksheet.Cells[row, 9].Value?.ToString();
							string studentFaculity = worksheet.Cells[row, 10].Value?.ToString();
							int _studentGraduationYear = int.Parse(worksheet.Cells[row, 11].Value?.ToString());
							DateTime studentGraduationYear = new DateTime(_studentGraduationYear, 1, 1);
							//DateTime studentGraduationYear = Convert.ToDateTime( worksheet.Cells[row, 11].Value?.ToString());
							string studentSpecialization = worksheet.Cells[row, 12].Value?.ToString();
							int trackId = int.Parse(worksheet.Cells[row, 13].Value?.ToString());

							Student s = new Student
							{
								Name = name,
								Email = email,
								Phone = phone,
								Password = password,
								IsApproved = (Approve)isApproved,
								UserType = "Student",
								ImgUrl = imgUrl,
								StudentDegree = studentDegree,
								StudentUniversity = studentUniversity,
								StudentFaculity = studentFaculity,
								StudentGraduationYear = studentGraduationYear,
								StudentSpecialization = studentSpecialization,
								TrackId = trackId
							};

							repoStudent.Add(s);
							studentAdded++;
						}
					}
				}

				return Ok($"Students uploaded successfully.\n number of Students Added: {studentAdded}");
			}
			catch (Exception ex)
			{

				return BadRequest($"An error occurred while processing the file. Please try again later. \n number of Students Added: {studentAdded}");
			}
		}

		private void SaveImageToDirectory(Student student)
		{
			if (student.Image != null && (student.Image.ContentType == "image/png" || student.Image.ContentType == "image/jpg" || student.Image.ContentType == "image/jpeg"))
			{
				var uniqueFileName = CreateUniqueFileName(student.Image);
				student.ImgUrl = uniqueFileName;
				var filePath = getFileName(uniqueFileName);
				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					student.Image.CopyTo(stream);
				}
			}
		}
		#endregion

		#region Student Degrees
		[HttpGet]
		public IActionResult ViewStudentsDegrees(int? page, int? trackid, DateTime? date)
		{

			if (!page.HasValue || page < 1)
			{
				page = 1;
			}
			var studentAttendances = repoStdAttendance.getStudentAttendaces();
	

			if (trackid != null)
			{
				studentAttendances = studentAttendances.Where(s => s.StudentSchdule.Track.TrackId == trackid).ToList();
			}
			if (date != null)
			{
				studentAttendances = studentAttendances.Where(s => s.Date.Date == date.Value.Date).ToList();
			}


			int totalAttendances = studentAttendances.Count();
			int totalPages = (int)Math.Ceiling((double)totalAttendances / pageSize);

			if (page > totalPages)
			{
				page = totalPages;
			}

			var _studentAttendances = studentAttendances.Skip((page.Value - 1) * pageSize).Take(pageSize).ToList();

			ViewBag.TotalPages = totalPages;
			ViewBag.Page = page;
			ViewBag.TrackId = trackid;
			ViewBag.Date = date;
			ViewBag.TrackList = repoTrack.getAll();
			return View(_studentAttendances);
		}
		#endregion



		private string getFileName(string fileName)
		{
			return Path.Combine(_hostingEnvironment.ContentRootPath, "wwwroot", "Images", "Profile", fileName);
		}
		private string CreateUniqueFileName(IFormFile file)
		{
			var uniquePart = Guid.NewGuid().ToString().Substring(0, 8); // Get the first 8 characters of the GUID
			var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName);
			var extension = Path.GetExtension(file.FileName);
			return $"{fileNameWithoutExtension}_{uniquePart}{extension}";
		}
		private async Task<IFormFile> GetFileFromPath(string name)
		{
			string fullPath = Path.Combine(_hostingEnvironment.ContentRootPath, "wwwroot", "Images", "Profile", name);
			if (System.IO.File.Exists(fullPath))
			{
				byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);

				using (var memoryStream = new MemoryStream(fileBytes))
				{
					return new FormFile(memoryStream, 0, fileBytes.Length, name, Path.GetFileName(fullPath));
				}
			}
			return null;
		}

		[HttpGet]
		public IActionResult Students()
		{
			var model = new StudentAttendanceViewModel
			{
				SelectedDate = DateTime.Today,
				Tracks = repoTrack.getAll(),
				StudentAttendances = new List<Models.Attendance>(),
				Permissions = repoPermission.getAll()
			};
			return View(model);
		}

		[HttpPost]
		public IActionResult Students(StudentAttendanceViewModel model)
		{
			try
			{
				Console.WriteLine("Entered Students action method.");

				if (model == null)
				{
					Console.WriteLine("Model is null. Initializing with default values.");
					model = new StudentAttendanceViewModel
					{
						SelectedDate = DateTime.Today,
						Tracks = repoTrack.getAll(),
						Permissions = repoPermission.getAll() // Fetch permissions here
					};
				}
				else
				{
					Console.WriteLine("Model is not null.");
					model.Tracks = repoTrack.getAll();
					model.Permissions = repoPermission.getAll(); // Fetch permissions here
				}

				if (ModelState.IsValid)
				{
					Console.WriteLine("ModelState is valid. Fetching student attendances.");
					var studentAttendances = repoStudent.GetStudentAttendance(model.SelectedDate, model.SelectedTrackId);
					model.StudentAttendances = studentAttendances;

					// Grouping attendances by status
					model.StudentAttendanceDictionary = model.StudentAttendances.GroupBy(a => a.Status)
						.ToDictionary(g => g.Key, g => g.ToList());
				}
				else
				{
					// Output ModelState errors to the console
					foreach (var state in ModelState)
					{
						foreach (var error in state.Value.Errors)
						{
							Console.WriteLine($"Error in {state.Key}: {error.ErrorMessage}");
						}
					}
				}

				Console.WriteLine("Returning the view with the model.");
				return View(model);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"An error occurred: {ex.Message}");
				throw; // Rethrow the exception
			}
		}






		//handle pending student
		public IActionResult ApproveOrRejectPendingStudents()
		{

			var model = repoStudent.GetPendingStudents();
			return View(model);
		}

		[HttpPost]
		public IActionResult ApproveOrReject(int studentId, string action)
		{



			if (action == "approve")
			{
				repoStudent.ApproveStudent(studentId);
				var message = new Msg
				{
					UserId = studentId,
					Title = "Welcome Message",
					Description = "Welcome to our system!",
					Date = DateTime.Now,
					IsRead = false
				};

				repoMsg.Add(message);

			}
			else if (action == "reject")
			{

				repoStudent.Delete(studentId);

			}

			var hasPendingStudents = repoStudent.GetPendingStudents().Count() > 0;
			if (!hasPendingStudents)
			{

				return RedirectToAction("Home", "Home");
			}
			return RedirectToAction("ApproveOrRejectPendingStudents");
		}

	}
}











