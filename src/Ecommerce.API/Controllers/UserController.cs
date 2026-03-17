using Ecommerce.Application.Authorization;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.DTOs.User;
using Ecommerce.Application.Extensions;
using Ecommerce.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : BaseController
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Permission("user.read")]
        public async Task<IActionResult> GetAll([FromQuery] PaginationDto pagedto)
        {
            var result = await _userService.GetAllAsync(pagedto);
            return OkResponse(result);
        }
        [HttpGet("{id}")]
        [Permission("user.read")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _userService.GetByIdAsync(id);
            return OkResponse(result);
        }

        [HttpPut("{id}")]
        [Permission("user.update")]
        public async Task<IActionResult> Update(int id, [FromBody] AdminUpdateUserDto dto)
        {
            int adminId = User.GetUserId();
            var result = await _userService.UpdateAsync(id, dto, adminId);
            return OkResponse(result);
        }


        [HttpDelete("{id}")]
        [Permission("user.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            int adminId = User.GetUserId();
            var result = await _userService.DeleteAsync(id, adminId);
            return OkResponse(result);
        }
    }
}
