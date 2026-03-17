using Ecommerce.Application.Authorization;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.DTOs.User;
using Ecommerce.Application.Extensions;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Permission(Permissions.ViewUser)]
        public async Task<IActionResult> GetAll([FromQuery] PaginationDto pagedto)
        {
            var result = await _userService.GetAllAsync(pagedto);
            return Ok(result);
        }
        [HttpGet("{id}")]
        [Permission(Permissions.ViewByIdUser)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _userService.GetByIdAsync(id);
            return Ok(result);
        }

        // 3. Cập nhật thông tin (Chỉ Admin mới có quyền cập nhật người khác hoặc chính user đó tùy logic Service)
        [HttpPut("{id}")]
        [Permission(Permissions.UpdateUser)]
        public async Task<IActionResult> Update(int id, [FromBody] AdminUpdateUserDto dto)
        {
            int adminId = User.GetUserId();
            var result = await _userService.UpdateAsync(id, dto, adminId);
            return Ok(result);
        }


        // 5. Xóa người dùng (Soft Delete)
        [HttpDelete("{id}")]
        [Permission(Permissions.DeleteUser)]
        public async Task<IActionResult> Delete(int id)
        {
            int adminId = User.GetUserId();
            var result = await _userService.DeleteAsync(id, adminId);
            return Ok(result);
        }
    }
}
