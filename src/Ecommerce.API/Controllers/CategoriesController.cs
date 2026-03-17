using Ecommerce.Application.Authorization;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.DTOs.CategoryDtos;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace Ecommerce.API.Controllers
{
   
    public class CategoriesController : BaseController
    {
        private readonly ICategoryService _service;

        public CategoriesController(ICategoryService service)
        { 
            _service = service;
        }

        [Permission("category.view")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] CategoryFilterDto filter , [FromQuery] PaginationDto pagination)
        {
            var result = await _service.GetAllAsync(filter, pagination);
            return ApiSuccess(result);
        }

        [Permission("category.viewbyid")]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return ApiSuccess(result);
        }
        [Permission("category.create")]
        [HttpPost]                                                                                              
        public async Task<IActionResult> Create(CategoryCreateDto dto)
        {
          
            var result = await _service.CreateAsync(dto);
            return ApiSuccess(result);
        }
        [Permission("category.update")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryUpdateDto dto)
        {

            var result = await _service.UpdateAsync(id, dto);

            return ApiSuccess(result);
        }
        [Permission("categories.delete")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return ApiSuccess(result);
        }
    }
}
