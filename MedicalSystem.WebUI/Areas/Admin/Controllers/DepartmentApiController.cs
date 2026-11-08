using AspNetCoreGeneratedDocument;
using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.DTOs.Department;
using MedicalSystem.Models.Models;
using MedicalSystem.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalSystem.WebUI.Areas.Admin.Controllers
{
    [ApiExplorerSettings(GroupName = "v1")]
    [ApiController]
    [Authorize(Roles = SD.Role_Admin)]
    [Area("Admin")]
    [Route("api/[area]/[controller]")]
    public class DepartmentApiController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public DepartmentApiController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [AllowAnonymous]
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

            var dtoList = departments.Select(d => new GetAllDepartmentsDto
            {
                Id = d.Id,
                Name = d.Name,
                CreatedAt = d.CreatedAt
            });

            var response = new
            {
                Departments = dtoList,
                SearchString = searchString,
                CurrentSort = currentSort,
                NameSort = currentSort == "name" ? "name_desc" : "name",
                CreatedAtSort = currentSort == "created" ? "created_desc" : "created",
                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return Ok(response);
        }

        [HttpGet("GetDepartment/{id:int}")]
        public async Task<IActionResult> GetDepartment([FromRoute] int id)
        {
            var department = await _unitOfWork.Department.GetFirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
            {
                return NotFound(new { message = "Department not found." });
            }

            var dto = new GetDepartmentDto
            {
                Id = department.Id,
                Name = department.Name,
                Description = department.Description,
                CreatedAt = department.CreatedAt
            };

            return Ok(dto);
        }

        [HttpPost("CreateDepartment")]
        public async Task<IActionResult> CreateDepartment([FromBody] DepartmentDto dto)
        {
            if (await _unitOfWork.Department.IsNameExistsAsync(dto.Name))
            {
                return BadRequest(new { message = "This Name already exists." });
            }

            var department = new Department
            {
                Name = dto.Name,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Department.AddAsync(department);
            await _unitOfWork.SaveAsync();

            return Ok(new { message = "Department created successfully." });
        }

        [HttpPut("EditDepartment/{id:int}")]
        public async Task<IActionResult> EditDepartment([FromRoute] int id, [FromBody] DepartmentDto dto)
        {
            var department = await _unitOfWork.Department.GetFirstOrDefaultAsync(d => d.Id == id);
            if (department == null)
            {
                return NotFound(new { message = "Department not found." });
            }

            if (await _unitOfWork.Department.IsNameExistsAsync(dto.Name, id))
            {
                return BadRequest(new { message = "This Name already exists." });
            }

            department.Name = dto.Name;
            department.Description = dto.Description;

            await _unitOfWork.Department.UpdateAsync(department);
            await _unitOfWork.SaveAsync();

            return Ok(new { message = "Department updated successfully." });
        }

        [HttpDelete("DeleteDepartment/{id:int}")]
        public async Task<IActionResult> DeleteDepartment([FromRoute] int id)
        {
            var department = await _unitOfWork.Department.GetFirstOrDefaultAsync(d => d.Id == id);
            if (department == null)
            {
                return NotFound(new { message = "Department not found." });
            }

            _unitOfWork.Department.Remove(department);
            await _unitOfWork.SaveAsync();

            return Ok(new { message = "Department deleted successfully." });
        }
    }
}
