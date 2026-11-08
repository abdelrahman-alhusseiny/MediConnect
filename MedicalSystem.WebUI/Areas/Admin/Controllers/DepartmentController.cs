using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.Models;
using MedicalSystem.Models.ViewModels;
using MedicalSystem.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MedicalSystem.WebUI.Areas.Admin.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = SD.Role_Admin)]
    [Area("Admin")]
    [Route("Admin/Department")]
    public class DepartmentController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public DepartmentController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("GetAllDepartments")]
        public async Task<IActionResult> GetAllDepartments(
            [FromQuery] string searchString = "",
            [FromQuery] string currentSort = "",
            [FromQuery] int currentPage = 1)
        {
            int pageSize = 6;
            var (departments, totalItems) =
                await _unitOfWork.Department.GetFilteredDepartmentsAsync(
                    searchString, currentSort, currentPage, pageSize);

            var departmentRows = new List<DepartmentRowVM>();
            foreach (var d in departments)
            {
                departmentRows.Add(new DepartmentRowVM
                {
                    Department = d,
                });
            }

            var vm = new DepartmentIndexVM
            {
                Departments = departmentRows,

                SearchString = searchString,

                CurrentSort = currentSort,
                NameSort = currentSort == "name" ? "name_desc" : "name",
                CreatedAtSort = currentSort == "created" ? "created_desc" : "created",

                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return View("Areas/Admin/Views/Department/GetAllDepartments.cshtml", vm);
        }

        [HttpGet("GetDepartment/{id:int}")]
        public async Task<IActionResult> GetDepartment(
            [FromRoute] int id, [FromQuery] DepartmentVM vm)
        {
            var department = await _unitOfWork.Department.GetFirstOrDefaultAsync(
                d => d.Id == id);
            if (department == null)
            {
                return NotFound();
            }

            vm.Department = department;

            return View("Areas/Admin/Views/Department/GetDepartment.cshtml", vm);
        }

        [HttpGet("CreateDepartment")]
        public IActionResult CreateDepartment([FromQuery] DepartmentVM vm)
        {
            return View("Areas/Admin/Views/Department/CreateDepartment.cshtml", vm);
        }

        [HttpPost("CreateDepartment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDepartmentConfirmed([FromForm] DepartmentVM vm)
        {
            if (await _unitOfWork.Department.IsNameExistsAsync(vm.Department.Name))
            {
                ModelState.AddModelError("Department.Name", "This Name is already exist");
            }

            if (!ModelState.IsValid)
            {
                return View("Areas/Admin/Views/Department/CreateDepartment.cshtml", vm);
            }

            await _unitOfWork.Department.AddAsync(vm.Department);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Department created successfully.";
            return RedirectToAction("GetAllDepartments", "Department", new { area = "Admin" });
        }

        [HttpGet("EditDepartment/{id:int}")]
        public async Task<IActionResult> EditDepartment(
            [FromRoute] int id, [FromQuery] DepartmentVM vm)
        {
            var department = await _unitOfWork.Department.GetFirstOrDefaultAsync(
                d => d.Id == id);
            if (department == null)
            {
                return NotFound();
            }

            vm.Department = department;

            return View("Areas/Admin/Views/Department/EditDepartment.cshtml", vm);
        }

        [HttpPost("EditDepartment/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDepartmentConfirmed([FromForm] DepartmentVM vm)
        {
            if (await _unitOfWork.Department.IsNameExistsAsync(
                vm.Department.Name, vm.Department.Id))
            {
                ModelState.AddModelError("Department.Name", "This Name is already exist");
            }

            if (!ModelState.IsValid)
            {
                return View("Areas/Admin/Views/Department/EditDepartment.cshtml", vm);
            }

            await _unitOfWork.Department.UpdateAsync(vm.Department);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Department updated successfully.";
            return RedirectToAction("GetAllDepartments", "Department", new
            {
                area = "Admin",
                searchString = vm.SearchString,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpGet("DeleteDepartment/{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id, [FromQuery] DepartmentVM vm)
        {
            var department = await _unitOfWork.Department.GetFirstOrDefaultAsync(
                d => d.Id == id);
            if (department == null)
            {
                return NotFound();
            }

            vm.Department = department;

            return View("Areas/Admin/Views/Department/DeleteDepartment.cshtml", vm);
        }

        [HttpPost("DeleteDepartment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDepartmentConfirmed([FromForm] DepartmentVM vm)
        {
            var department = await _unitOfWork.Department.GetFirstOrDefaultAsync(
                d => d.Id == vm.Department.Id);
            if (department == null)
            {
                return NotFound();
            }

            _unitOfWork.Department.Remove(department);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Department deleted successfully.";
            return RedirectToAction("GetAllDepartments", "Department", new
            {
                area = "Admin",
                searchString = vm.SearchString,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }
    }
}
