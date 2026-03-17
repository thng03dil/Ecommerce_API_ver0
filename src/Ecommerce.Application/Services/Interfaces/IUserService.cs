using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.Role;
using Ecommerce.Application.DTOs.User;
using Ecommerce.Domain.Common.Filters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse<PagedResponse<UserResponseDto>>> GetAllAsync( PaginationDto pagination);
        Task<ApiResponse<UserResponseDto?>> GetByIdAsync(int id);
        Task<ApiResponse<UserResponseDto>> UpdateAsync(int id, AdminUpdateUserDto dto, int adminId);
        Task<ApiResponse<UserResponseDto>> DeleteAsync(int id,  int adminId);

    }
}
