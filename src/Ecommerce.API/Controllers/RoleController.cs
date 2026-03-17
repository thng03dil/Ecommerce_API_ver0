using Ecommerce.Application.Authorization;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.DTOs.Role;
using Ecommerce.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RoleController : BaseController
    {

        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        [Permission("role.view")]
        public async Task<IActionResult> GetAll([FromQuery] PaginationDto pagination)
        {
            var result = await _roleService.GetAllAsync(pagination);
            return ApiSuccess(result);
        }

        [HttpGet("{id}")]
        [Permission("role.viewbyid")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _roleService.GetByIdAsync(id);
            return ApiSuccess(result);
        }

        [HttpPost]
        [Permission("role.create")]
        public async Task<IActionResult> Create([FromBody] RoleCreateDto dto)
        {
            var result = await _roleService.CreateAsync(dto);
            return ApiSuccess(result);
        }

        [HttpPut("{id}")]
        [Permission("role.update")]
        public async Task<IActionResult> Update(int id, [FromBody] RoleUpdateDto dto)
        {
            var result = await _roleService.UpdateAsync(id, dto);
            return ApiSuccess(result);
        }

        [HttpPost("assign-permissions")]
        [Permission("role.update")] 
        public async Task<IActionResult> AssignPermissions([FromBody] AssignPermissionsDto dto)
        {
            var result = await _roleService.AssignPermissionsAsync(dto);
            return ApiSuccess(result);
        }

        [HttpDelete("{id}")]
        [Permission("role.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _roleService.DeleteAsync(id);
            return ApiSuccess(result);
        }
    }
}
    
