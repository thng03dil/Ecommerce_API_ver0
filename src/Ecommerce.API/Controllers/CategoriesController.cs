using Ecommerce.Application.Authorization;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.DTOs.CategoryDtos;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common.Filters;
using Ecommerce.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace Ecommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : Controller
    {
        private readonly ICategoryService _service;

        public CategoriesController(ICategoryService service)
        { 
            _service = service;
        }

        [Permission(Permissions.ViewCategory)]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] CategoryFilterDto filter , [FromQuery] PaginationDto pagination)
        {
            var result = await _service.GetAllAsync(filter, pagination);
            return Ok(result);
        }

        [Permission(Permissions.ViewByIdCategory)]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
        [Permission(Permissions.CreateCategory)]
        [HttpPost]                                                                                              
        public async Task<IActionResult> Create(CategoryCreateDto dto)
        {
          
            var result = await _service.CreateAsync(dto);
            return Ok(result);
        }
        [Permission(Permissions.UpdateCategory)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryUpdateDto dto)
        {

            var result = await _service.UpdateAsync(id, dto);

            return Ok(result);
        }
        [Permission(Permissions.DeleteCategory)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return Ok(result);
        }
    }
}
