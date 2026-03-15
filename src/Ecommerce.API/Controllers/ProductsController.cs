using Microsoft.AspNetCore.Mvc;

using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Application.DTOs.ProductDtos;
using Ecommerce.Domain.Common.Filters;
using Ecommerce.Application.Common.Pagination;
using Microsoft.AspNetCore.Authorization;

namespace Ecommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : Controller
    {
        private readonly IProductService _service;

        public ProductsController(IProductService service)
        {
            _service = service; 
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ProductFilterDto filter,[FromQuery] PaginationDto pagination)
        {
            var result = await _service.GetAllAsync(filter,pagination);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {                                                                                            
            var product = await _service.GetByIdAsync(id);
            if (product == null)
                return NotFound();

            return Ok(product);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(ProductCreateDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return Ok(result);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, ProductUpdateDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);

            return Ok(result); 
        }
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return Ok(result);
        }
    }
}

