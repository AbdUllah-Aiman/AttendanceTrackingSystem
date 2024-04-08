﻿using AttendanceTrackingSystem.Models;
using AttendanceTrackingSystem.IRepository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AttendanceTrackingSystem.ViewModel;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AttendanceTrackingSystem.Pagination;
using System.Drawing.Printing;


[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IRepoEmployee _repoEmployee;

    public AdminController(IRepoEmployee repoEmployee)
    {
        _repoEmployee = repoEmployee;
    }

    // GET: Admin
    public IActionResult Employee(string searchString, int pageNumber = 1, int pageSize = 4)
    {
        var employees = _repoEmployee.getAll();

       if (!string.IsNullOrEmpty(searchString))
{
    employees = employees.Where(e => e.Name.Contains(searchString) || e.Email.Contains(searchString)).ToList();
}



        var employeeUsers = employees.Where(e => e.UserType == "Employee").AsQueryable();
        var model = PaginatedList<Employee>.Create(employeeUsers, pageNumber, pageSize);

        return View(model);
    }







    // GET: Admin/Details/5
    public ActionResult Details(int id)
    {
        var employee = _repoEmployee.getById(id);
        if (employee == null)
        {
            return NotFound();
        }

        var viewModel = new EmployeeViewModel
        {
            UserId = employee.UserId,
            Name = employee.Name,
            Email = employee.Email,
            Phone = employee.Phone,
            Password = employee.Password,
            ImgUrl = employee.ImgUrl,
            EmployeeSalary = employee.EmployeeSalary,
            EmployeeType = employee.EmployeeType,
            UserType = employee.UserType,
            IsApproved = employee.IsApproved
        };

        return View(viewModel);
    }

    // GET: Admin/Create
    public ActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(EmployeeViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            // Check if the email already exists
            var existingEmployee = _repoEmployee.GetByEmail(viewModel.Email);
            if (existingEmployee != null)
            {
                ModelState.AddModelError("Email", "Email address already exists.");
                return View(viewModel);
            }

            // Process the uploaded file if one exists
            if (viewModel.Photo != null && viewModel.Photo.Length > 0)
            {
                using (var stream = new MemoryStream())
                {
                    await viewModel.Photo.CopyToAsync(stream);
                    viewModel.ImgUrl = $"data:{viewModel.Photo.ContentType};base64,{Convert.ToBase64String(stream.ToArray())}";
                }
            }

            var employee = new Employee
            {
                Name = viewModel.Name,
                Email = viewModel.Email,
                Phone = viewModel.Phone,
                Password = viewModel.Password,
                ImgUrl = viewModel.ImgUrl,
                EmployeeSalary = viewModel.EmployeeSalary,
                EmployeeType = viewModel.EmployeeType,
                UserType = "Employee",
                IsApproved = Approve.Accepted
            };

            try
            {
                _repoEmployee.Add(employee);
                return RedirectToAction("Employee");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Unable to save changes due to an error: {ex.Message}");
            }
        }

        return View(viewModel);
    }











    // GET: Admin/Edit/5
    public ActionResult Edit(int id)
    {
        var employee = _repoEmployee.getById(id);
        if (employee == null)
        {
            return NotFound();
        }

        var viewModel = new EmployeeViewModel
        {
            UserId = employee.UserId,
            Name = employee.Name,
            Email = employee.Email,
            Phone = employee.Phone,
            Password = employee.Password,
            ImgUrl = employee.ImgUrl,
            EmployeeSalary = employee.EmployeeSalary,
            EmployeeType = employee.EmployeeType,
            UserType = "Employee",
            IsApproved = Approve.Accepted
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(EmployeeViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var existingEmployeeWithEmail = _repoEmployee.GetByEmail(viewModel.Email);
            if (existingEmployeeWithEmail != null && existingEmployeeWithEmail.UserId != viewModel.UserId)
            {
                ModelState.AddModelError("Email", "Email address already exists.");
                return View(viewModel);
            }

            var employee = _repoEmployee.getById(viewModel.UserId);
            if (employee == null)
            {
                return NotFound();
            }

            // Process the uploaded file if one exists
            if (viewModel.Photo != null && viewModel.Photo.Length > 0)
            {
                using (var stream = new MemoryStream())
                {
                    await viewModel.Photo.CopyToAsync(stream);
                    viewModel.ImgUrl = $"data:{viewModel.Photo.ContentType};base64,{Convert.ToBase64String(stream.ToArray())}";
                }
            }

            employee.Name = viewModel.Name;
            employee.Email = viewModel.Email;
            employee.Phone = viewModel.Phone;
            employee.Password = viewModel.Password;
            employee.ImgUrl = viewModel.ImgUrl;
            employee.EmployeeSalary = viewModel.EmployeeSalary;
            employee.EmployeeType = viewModel.EmployeeType;
            employee.UserType = "Employee";
            employee.IsApproved = Approve.Accepted;

            try
            {
                _repoEmployee.Update(employee);
                return RedirectToAction("Employee");
            }
            catch
            {
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }
        }

        return View(viewModel);
    }






    // GET: Admin/Delete/5
    public ActionResult Delete(int id)
    {
        var employee = _repoEmployee.getById(id);
        if (employee == null)
        {
            return NotFound();
        }

        var viewModel = new EmployeeViewModel
        {
            UserId = employee.UserId,
            Name = employee.Name,
            Email = employee.Email,
            Phone = employee.Phone,
            Password = employee.Password,
            ImgUrl = employee.ImgUrl,
            EmployeeSalary = employee.EmployeeSalary,
            EmployeeType = employee.EmployeeType,
            UserType = employee.UserType,
            IsApproved = employee.IsApproved
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult DeleteConfirmed(int id)
    {
        try
        {
            // Attempt to delete the employee from the database
            _repoEmployee.Delete(id);

            // Return JSON indicating successful deletion
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            Console.WriteLine($"Error deleting employee with ID {id}: {ex.Message}");

            // Return JSON indicating failure with error message
            return Json(new { success = false, errorMessage = "Failed to delete the employee." });
        }
    }




}
