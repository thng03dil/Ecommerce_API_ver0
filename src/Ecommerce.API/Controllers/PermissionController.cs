using Ecommerce.Application.Authorization;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.DTOs.Permission;
using Ecommerce.Application.DTOs.ProductDtos;
using Ecommerce.Application.Services.Implementations;
using Ecommerce.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PermissionController : BaseController
    {
        private readonly IPermissionService _permissionService;

        public PermissionController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        [HttpGet]
        [Permission("permission.view")]
        public async Task<IActionResult> GetAll([FromQuery] PaginationDto pagination)
        {
            var result = await _permissionService.GetAllAsync(pagination);
            return ApiSuccess(result);
        }

        [HttpGet("{id}")]
        [Permission("permission.viewbyid")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _permissionService.GetByIdAsync(id);
            return ApiSuccess(result);
        }
        [Permission("permission.create")]
        [HttpPost]
        public async Task<IActionResult> Create(PermissionCreateDto dto)
        {
            var result = await _permissionService.CreateAsync(dto);
            return ApiSuccess(result);
        }

        [Permission("permission.update")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, PermissionUpdateDto dto)
        {
            var result = await _permissionService.UpdateAsync(id, dto);

            return ApiSuccess(result);
        }
        [Permission("permission.delete")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _permissionService.DeleteAsync(id);
            return ApiSuccess(result);
        }


    }
}
