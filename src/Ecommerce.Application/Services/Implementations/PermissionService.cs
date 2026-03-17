using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.Permission;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Services.Implementations
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionRepo _permissionRepo;
        private readonly IRoleRepo _roleRepo;

        private readonly ILogger<PermissionService> _logger;
        public PermissionService( 
            IPermissionRepo permissionRepo,
            IRoleRepo roleRepo,
            ILogger<PermissionService> logger)
        {
            _permissionRepo = permissionRepo;
            _roleRepo = roleRepo;
            _logger = logger;
        }
        public async Task<ApiResponse<PagedResponse<PermissionResponseDto>>> GetAllAsync(PaginationDto pagedto)
        {
            _logger.LogInformation(
                       "Get permissions request Page:{Page} Size:{Size}",
                       pagedto.PageNumber,
                       pagedto.PageSize);
            var (permissions, totalCount) = await _permissionRepo.GetAllAsync(pagedto);

            var data = permissions.Select(r => MapToResponseDto(r)).ToList();

            var pagedResponse = new PagedResponse<PermissionResponseDto>(
                data,
                totalCount,
                pagedto.PageNumber,
                pagedto.PageSize);

            return ApiResponse<PagedResponse<PermissionResponseDto>>.SuccessResponse(pagedResponse);

        }
        public async Task<ApiResponse<PermissionResponseDto>> GetByIdAsync(int id) 
        {
            _logger.LogInformation("Get permission by id {PermissionId}", id);
            var permission = await _permissionRepo.GetByIdAsync(id);
            if (permission == null)
            {
                _logger.LogWarning("Update failed: permission not found {PermissionId}", id);
                throw new NotFoundException("Permission not found");
            }
            return ApiResponse<PermissionResponseDto>.SuccessResponse(MapToResponseDto(permission));
        }

        public async Task<ApiResponse<PermissionResponseDto>> CreateAsync(PermissionCreateDto dto)
        {
            var entity = (dto.Entity ?? string.Empty).Trim().ToLowerInvariant();
            var action = (dto.Action ?? string.Empty).Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(entity) || string.IsNullOrWhiteSpace(action))
                throw new BusinessException("Entity and Action are required.");

            var name = $"{entity}.{action}";

            _logger.LogInformation("Create permission {PermissionName}", name);

            if (await _permissionRepo.ExistsByEntityActionAsync(entity, action))
                throw new BusinessException("Permission already exists (Entity + Action must be unique).");

            var permission = new Permission
            {
                Entity = entity,
                Action = action,
                Name = name,
                Description = dto.Description?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };
            await _permissionRepo.AddAsync(permission);

            // Auto-assign new permission to Admin role (highest role).
            var adminRole = await _roleRepo.GetByNameRoleAsync("Admin");
            if (adminRole != null)
            {
                var alreadyAssigned = await _permissionRepo.RolePermissionExistsAsync(adminRole.Id, permission.Id);
                if (!alreadyAssigned)
                {
                    await _permissionRepo.AddRolePermissionAsync(new RolePermission
                    {
                        RoleId = adminRole.Id,
                        PermissionId = permission.Id
                    });
                }
            }

            _logger.LogInformation("Permission created {PermissionId}", permission.Id);

            var item = MapToResponseDto(permission);
            return ApiResponse<PermissionResponseDto>.SuccessResponse(
                   item,
                    "Create data successfully"
                    );
        }

        public async Task<ApiResponse<PermissionResponseDto>> UpdateAsync(int id, PermissionUpdateDto dto)
        {
            _logger.LogInformation("Update permission {PermissionId}", id);
            var permission = await _permissionRepo.GetByIdAsync(id);
            if (permission == null)
            {
                _logger.LogWarning("Update failed: permission not found {PermissionId}", id);
                throw new NotFoundException("Permission not found");
            }
            var entity = (dto.Entity ?? string.Empty).Trim().ToLowerInvariant();
            var action = (dto.Action ?? string.Empty).Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(entity) || string.IsNullOrWhiteSpace(action))
                throw new BusinessException("Entity and Action are required.");

            if (await _permissionRepo.ExistsByEntityActionAsync(entity, action, excludePermissionId: id))
                throw new BusinessException("Permission already exists (Entity + Action must be unique).");

            permission.Entity = entity;
            permission.Action = action;
            permission.Name = $"{entity}.{action}";
            permission.Description = dto.Description?.Trim() ?? string.Empty;
            permission.UpdatedAt = DateTime.UtcNow;

            await _permissionRepo.UpdateAsync(permission);

            _logger.LogInformation("Permission updated {PermissionId}", permission.Id);

            var item = MapToResponseDto(permission);
            return ApiResponse<PermissionResponseDto>.SuccessResponse(
                   item,
                   "Update data successfully"
                   );
        }

        public async Task<ApiResponse<PermissionResponseDto>> DeleteAsync(int id)
        {
            _logger.LogInformation("Delete permission {PermissionId}", id);
            var permission = await _permissionRepo.GetByIdAsync(id);

            if (permission == null)
            {
                _logger.LogWarning("Delete failed: permission not found {PermissionId}", id);
                throw new NotFoundException("Permission not found");
            }

            // Step 1: Protect system permissions
            if (permission.IsSystem)
                throw new BusinessException("Không thể xóa quyền hệ thống");

            // Step 2: Allow delete even if Admin holds it, but block if any non-Admin role is using it.
            if (await _permissionRepo.IsAssignedToAnyNonAdminRoleAsync(permission.Id))
                throw new BusinessException("Quyền này đang được gán cho các vai trò người dùng. Hãy gỡ quyền trước khi xóa");

            // Step 3: Soft delete
            permission.IsDeleted = true;

            await _permissionRepo.SaveChangesAsync();

            // Step 4: Hard delete role-permission mappings to keep DB clean.
            await _permissionRepo.HardDeleteRolePermissionsByPermissionIdAsync(permission.Id);

            _logger.LogInformation("Permission deleted {PermissionId}", permission.Id);

            var item = MapToResponseDto(permission);
            return ApiResponse<PermissionResponseDto>.SuccessResponse(
                     item,
                    "Delete data successfully"
            );
        }
        private PermissionResponseDto MapToResponseDto(Permission p) => new PermissionResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            Entity = p.Entity,
            Action = p.Action,
            Description = p.Description,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
    }
}
