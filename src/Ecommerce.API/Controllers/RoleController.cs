using Ecommerce.Application.Authorization;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.DTOs.Permission;
using Ecommerce.Application.DTOs.Role;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RoleController : Controller
    {

        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        [Permission(Permissions.ViewRole)]
        public async Task<IActionResult> GetAll([FromQuery] PaginationDto pagination)
        {
            var result = await _roleService.GetAllAsync(pagination);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Permission(Permissions.ViewByIdRole)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _roleService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        [Permission(Permissions.CreateRole)]
        public async Task<IActionResult> Create([FromBody] RoleCreateDto dto)
        {
            var result = await _roleService.CreateAsync(dto);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Permission(Permissions.UpdateRole)]
        public async Task<IActionResult> Update(int id, [FromBody] RoleUpdateDto dto)
        {
            var result = await _roleService.UpdateAsync(id, dto);
            return Ok(result);
        }

        [HttpPost("assign-permissions")]
        [Permission(Permissions.UpdateRole)] 
        public async Task<IActionResult> AssignPermissions([FromBody] AssignPermissionsDto dto)
        {
            var result = await _roleService.AssignPermissionsAsync(dto);
            return Ok(result);
        }

        // 6. Xóa Role
        [HttpDelete("{id}")]
        [Permission(Permissions.DeleteRole)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _roleService.DeleteAsync(id);
            return Ok(result);
        }
    }
}
    
