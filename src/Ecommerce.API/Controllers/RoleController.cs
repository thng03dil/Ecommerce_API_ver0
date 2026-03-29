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
        [Authorize(Policy = "role.read")]
        public async Task<IActionResult> GetAll([FromQuery] PaginationDto pagination)
        {
            var result = await _roleService.GetAllAsync(pagination);
            return OkResponse(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "role.read")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _roleService.GetByIdAsync(id);
            return OkResponse(result);
        }

        [HttpPost]
        [Authorize(Policy = "role.create")]
        public async Task<IActionResult> Create([FromBody] RoleCreateDto dto)
        {
            var result = await _roleService.CreateAsync(dto);
            return OkResponse(result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "role.update")]
        public async Task<IActionResult> Update(int id, [FromBody] RoleUpdateDto dto)
        {
            var result = await _roleService.UpdateAsync(id, dto);
            return OkResponse(result);
        }

        [HttpPost("assign-permissions")]
        [Authorize(Policy = "role.update")]
        public async Task<IActionResult> AssignPermissions([FromBody] AssignPermissionsDto dto)
        {
            var result = await _roleService.AssignPermissionsAsync(dto);
            return OkResponse(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "role.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _roleService.DeleteAsync(id);
            return OkResponse(result);
        }
    }
}
    
