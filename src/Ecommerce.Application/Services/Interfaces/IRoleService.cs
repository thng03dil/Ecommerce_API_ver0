using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.Permission;
using Ecommerce.Application.DTOs.Role;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Services.Interfaces
{
    public interface IRoleService
    {
        Task<ApiResponse<PagedResponse<RoleResponseDto>>> GetAllAsync(PaginationDto pagedto);
        Task<ApiResponse<RoleWithPermissionsDto>> GetByIdAsync(int id);
        Task<ApiResponse<RoleResponseDto>> CreateAsync(RoleCreateDto dto);
        Task<ApiResponse<RoleResponseDto>> UpdateAsync(int id, RoleUpdateDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int id);
        Task<ApiResponse<bool>> AssignPermissionsAsync(AssignPermissionsDto dto);
    }
}
