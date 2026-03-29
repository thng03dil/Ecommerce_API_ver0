using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.DTOs.CategoryDtos;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace Ecommerce.API.Controllers
{
   
    public class CategoryController : BaseController
    {
        private readonly ICategoryService _service;

        public CategoryController(ICategoryService service)
        { 
            _service = service;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] CategoryFilterDto filter , [FromQuery] PaginationDto pagination)
        {
            var result = await _service.GetAllAsync(filter, pagination);
            return OkResponse(result);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return OkResponse(result);
        }
        [Authorize(Policy = "category.create")]
        [HttpPost]                                                                                              
        public async Task<IActionResult> Create(CategoryCreateDto dto)
        {
          
            var result = await _service.CreateAsync(dto);
            return OkResponse(result);
        }
        [Authorize(Policy = "category.update")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryUpdateDto dto)
        {

            var result = await _service.UpdateAsync(id, dto);

            return OkResponse(result);
        }
        [Authorize(Policy = "category.delete")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return OkResponse(result);
        }
    }
}
