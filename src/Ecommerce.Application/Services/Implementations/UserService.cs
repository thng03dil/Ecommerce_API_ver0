using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.CategoryDtos;
using Ecommerce.Application.DTOs.User;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepo _userRepo;
        public UserService(  IUserRepo userRepo
            )
        {
            _userRepo = userRepo;
        }

        public async Task<ApiResponse<PagedResponse<UserResponseDto>>> GetAllAsync( PaginationDto pagination) 
        {
            var (users, totalItems) = await _userRepo.GetAllAsync(pagination);

            var data = users.Select(c => MapToResponseDto(c)).ToList();

            var pagedData = new PagedResponse<UserResponseDto>(data, pagination.PageNumber, pagination.PageSize, totalItems);

            return ApiResponse<PagedResponse<UserResponseDto>>.SuccessResponse(pagedData, "Get data successfully");
        }
        public async Task<ApiResponse<UserResponseDto?>> GetByIdAsync(int id) 
        {
            var user = await _userRepo.GetByIdAsync(id);

            if (user == null)   throw new NotFoundException("User not found");
            
            var item = MapToResponseDto(user);
            return ApiResponse<UserResponseDto?>.SuccessResponse(
                     item,
                     "Get data successfully"
                    );
        }
        public async Task<ApiResponse<UserResponseDto>> UpdateAsync(int id, AdminUpdateUserDto dto, int adminId) 
        {
            if (id == adminId )  throw new BusinessException("You cannot delete your own admin account.");
            
            var user = await _userRepo.GetByIdForUpdateAsync(id);
            if (user == null)  throw new NotFoundException("User not found");
            
            user.RoleId = dto.RoleId;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepo.UpdateAsync(user);

            var item = MapToResponseDto(user);
            return ApiResponse<UserResponseDto>.SuccessResponse(
                   item,
                   "Update data successfully"
                   );
        }
        public async Task<ApiResponse<UserResponseDto>> DeleteAsync(int id, int adminId) 
        {
            if (id == adminId )  throw new BusinessException("You cannot delete your own admin account.");
            

            var user = await _userRepo.GetByIdForUpdateAsync(id);
            if (user == null) throw new NotFoundException("User not found");
            
            user.IsDeleted = true;

            await _userRepo.SaveChangesAsync();

            var item = MapToResponseDto(user);
            return ApiResponse<UserResponseDto>.SuccessResponse(
                     item,
                    "Delete data successfully"
            );
        }
        private static UserResponseDto MapToResponseDto(User u) => new()
        {
            Id = u.Id,
            Email = u.Email,
            RoleName = u.Role.Name,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        };
    }
}
