using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.DTOs.ProductDtos;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers
{
    public class ProductController : BaseController
    {
        private readonly IProductService _service;

        public ProductController(IProductService service)
        {
            _service = service; 
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ProductFilterDto filter,[FromQuery] PaginationDto pagination)
        {
            var result = await _service.GetAllAsync(filter,pagination);
            return OkResponse(result);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {                                                                                            
            var product = await _service.GetByIdAsync(id);
            if (product == null)
                return NotFound();

            return OkResponse(product);
        }

        [Authorize(Policy = "product.create")]
        [HttpPost]
        public async Task<IActionResult> Create(ProductCreateDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return OkResponse(result);
        }

        [Authorize(Policy = "product.update")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, ProductUpdateDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);

            return OkResponse(result); 
        }
        [Authorize(Policy = "product.delete")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return OkResponse(result);
        }
    }
}

