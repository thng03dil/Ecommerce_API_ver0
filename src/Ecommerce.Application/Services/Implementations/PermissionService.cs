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

        public PermissionService( 
            IPermissionRepo permissionRepo,
            IRoleRepo roleRepo)
        {
            _permissionRepo = permissionRepo;
            _roleRepo = roleRepo;
        }
        public async Task<ApiResponse<PagedResponse<PermissionResponseDto>>> GetAllAsync(PaginationDto pagedto)
        {
            var (permissions, totalCount) = await _permissionRepo.GetAllAsync(pagedto);

            var data = permissions.Select(r => MapToResponseDto(r)).ToList();

            var pagedResponse = new PagedResponse<PermissionResponseDto>(
                data,
                pagedto.PageNumber,
                pagedto.PageSize,
                totalCount);

            return ApiResponse<PagedResponse<PermissionResponseDto>>.SuccessResponse(pagedResponse);

        }
        public async Task<ApiResponse<PermissionResponseDto>> GetByIdAsync(int id) 
        {
            var permission = await _permissionRepo.GetByIdAsync(id);
            if (permission == null)  throw new NotFoundException("Permission not found");
            
            return ApiResponse<PermissionResponseDto>.SuccessResponse(MapToResponseDto(permission));
        }

        public async Task<ApiResponse<PermissionResponseDto>> CreateAsync(PermissionCreateDto dto)
        {
            var entity = (dto.Entity ?? string.Empty).Trim().ToLowerInvariant();
            var action = (dto.Action ?? string.Empty).Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(entity) || string.IsNullOrWhiteSpace(action))
                throw new BusinessException("Entity and Action are required.");

            var name = $"{entity}.{action}";


            if (await _permissionRepo.ExistsByEntityActionAsync(entity, action))
                throw new BusinessException("Permission already exists (Entity + Action must be unique).");

            var permission = new Permission
            {
                Entity = entity,
                Action = action,
                Name = name,
                IsSystem = false,
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

            var item = MapToResponseDto(permission);
            return ApiResponse<PermissionResponseDto>.SuccessResponse(
                   item,
                    "Create data successfully"
                    );
        }

        public async Task<ApiResponse<PermissionResponseDto>> UpdateAsync(int id, PermissionUpdateDto dto)
        {
            var permission = await _permissionRepo.GetByIdAsync(id);
            if (permission == null)  throw new NotFoundException("Permission not found");
            
            var entity = (dto.Entity ?? string.Empty).Trim().ToLowerInvariant();
            var action = (dto.Action ?? string.Empty).Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(entity) || string.IsNullOrWhiteSpace(action))
                throw new BusinessException("Entity and Action are required.");

            if (await _permissionRepo.ExistsByEntityActionAsync(entity, action, excludePermissionId: id))
                throw new BusinessException("Permission already exists (Entity + Action must be unique).");

            permission.Entity = entity;
            permission.Action = action;
            permission.Name = $"{entity}.{action}";
            permission.IsSystem = dto.IsSystem;
            permission.Description = dto.Description?.Trim() ?? string.Empty;
            permission.UpdatedAt = DateTime.UtcNow;

            await _permissionRepo.UpdateAsync(permission);


            var item = MapToResponseDto(permission);
            return ApiResponse<PermissionResponseDto>.SuccessResponse(
                   item,
                   "Update data successfully"
                   );
        }

        public async Task<ApiResponse<PermissionResponseDto>> DeleteAsync(int id)
        {
            var permission = await _permissionRepo.GetByIdAsync(id);

            if (permission == null)  throw new NotFoundException("Permission not found");
            

            // Step 1: Protect system permissions
            if (permission.IsSystem)
                throw new BusinessException("Do not delete system permission");

            // Step 2: Allow delete even if Admin holds it, but block if any non-Admin role is using it.
            if (await _permissionRepo.IsAssignedToAnyNonAdminRoleAsync(permission.Id))
                throw new BusinessException("Permission is assigned by role user. Please remove permissions before deleting");

            // Step 3: Soft delete
            permission.IsDeleted = true;

            await _permissionRepo.SaveChangesAsync();

            // Step 4: Hard delete role-permission mappings to keep DB clean.
            await _permissionRepo.HardDeleteRolePermissionsByPermissionIdAsync(permission.Id);

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
            IsSystem = p.IsSystem,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
    }
}
