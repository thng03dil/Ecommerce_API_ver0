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

        private readonly ILogger<AuthService> _logger;
        public UserService(
            IUserRepo userRepo,
            ILogger<AuthService> logger
            )
        {
            _userRepo = userRepo;
            _logger = logger;
        }

        public async Task<ApiResponse<PagedResponse<UserResponseDto>>> GetAllAsync( PaginationDto pagination) 
        {
            _logger.LogInformation(
                        "Get users request Page:{Page} Size:{Size}",
                        pagination.PageNumber,
                        pagination.PageSize);

            var (users, totalItems) = await _userRepo.GetAllAsync(pagination);

            var data = users.Select(c => MapToResponseDto(c)).ToList();

            var pagedData = new PagedResponse<UserResponseDto>(data, pagination.PageNumber, pagination.PageSize, totalItems);

            _logger.LogInformation(
                        "Get categories success Count:{Count}",
                        totalItems);
            return ApiResponse<PagedResponse<UserResponseDto>>.SuccessResponse(pagedData, "Get data successfully");
        }
        public async Task<ApiResponse<UserResponseDto?>> GetByIdAsync(int id) 
        {
            _logger.LogInformation("Get user by id {UserId}", id);
            var user = await _userRepo.GetByIdAsync(id);

            if (user == null)
            {
                _logger.LogWarning("user not found {UserId}", id);
                throw new NotFoundException("User not found");
            }
            var item = MapToResponseDto(user);
            return ApiResponse<UserResponseDto?>.SuccessResponse(
                     item,
                     "Get data successfully"
                    );
        }
        public async Task<ApiResponse<UserResponseDto>> UpdateAsync(int id, AdminUpdateUserDto dto, int adminId) 
        {
            _logger.LogInformation("Update user {UserId} by Admin {AdminId}", id, adminId);

            if (id == adminId )
            {
                _logger.LogWarning("Admin {AdminId} tried to soft-delete themselves.", id);
                throw new BusinessException("You cannot delete your own admin account.");
            }
            var user = await _userRepo.GetByIdForUpdateAsync(id);
            if (user == null)
            {
                _logger.LogWarning("Update failed: user not found {UserId}", id);
                throw new NotFoundException("User not found");
            }
            user.RoleId = dto.RoleId;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepo.UpdateAsync(user);

            _logger.LogInformation("User updated {userId}", user.Id);

            var item = MapToResponseDto(user);
            return ApiResponse<UserResponseDto>.SuccessResponse(
                   item,
                   "Update data successfully"
                   );
        }
        public async Task<ApiResponse<UserResponseDto>> DeleteAsync(int id, int adminId) 
        {
            _logger.LogInformation("Delete user {UserId} by Admin {AdminId}", id, adminId);

            if (id == adminId )
            {
                _logger.LogWarning("Admin {AdminId} tried to soft-delete themselves.", id);
                throw new BusinessException("You cannot delete your own admin account.");
            }

            var user = await _userRepo.GetByIdForUpdateAsync(id);
            if (user == null)
            {
                _logger.LogWarning("Delete failed:: user not found {UserId}", id);
                throw new NotFoundException("User not found");
            }
            user.IsDeleted = true;

            await _userRepo.SaveChangesAsync();

            _logger.LogInformation("User deleted {userId}", user.Id);

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
