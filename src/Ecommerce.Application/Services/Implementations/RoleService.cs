using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.Permission;
using Ecommerce.Application.DTOs.Role;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security;
using System.Text;

namespace Ecommerce.Application.Services.Implementations
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepo _roleRepo;
        private readonly IPermissionRepo _permissionRepo;
        private readonly ILogger<RoleService> _logger;
        public RoleService(
            IRoleRepo roleRepo,
            IPermissionRepo permissionRepo,
            ILogger<RoleService> logger)
        {
            _roleRepo = roleRepo;
            _permissionRepo = permissionRepo;
            _logger = logger;
        }
        public async Task<ApiResponse<PagedResponse<RoleResponseDto>>> GetAllAsync(PaginationDto pagedto)
        {
            _logger.LogInformation(
                      "Get roles request Page:{Page} Size:{Size}",
                      pagedto.PageNumber,
                      pagedto.PageSize);
            var (roles, totalCount) = await _roleRepo.GetAllAsync(pagedto);

            var data = roles.Select(r => MapToResponseDto(r)).ToList();

            var pagedResponse = new PagedResponse<RoleResponseDto>(
                data,
                totalCount,
                pagedto.PageNumber,
                pagedto.PageSize);

            return ApiResponse<PagedResponse<RoleResponseDto>>.SuccessResponse(pagedResponse);
        }
        public async Task<ApiResponse<RoleWithPermissionsDto>> GetByIdAsync(int id)
        {
            _logger.LogInformation("Get role by id {RoleId}", id);
            var role = await _roleRepo.GetByIdWithPermissionsAsync(id);
            if (role == null)
            {
                _logger.LogWarning("Update failed: permission not found {PermissionId}", id);

                throw new NotFoundException("Role not found");
            }

            var dto = new RoleWithPermissionsDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                Permissions = role.RolePermissions.Select(rp => new PermissionResponseDto
                {
                    Id = rp.Permission.Id,
                    Name = rp.Permission.Name,
                    Description = rp.Permission.Description
                }).ToList()
            };

            return ApiResponse<RoleWithPermissionsDto>.SuccessResponse(dto);
        }

        public async Task<ApiResponse<RoleResponseDto>> CreateAsync(RoleCreateDto dto)
        {
            _logger.LogInformation("Create role {Name}", dto.Name);
            if (await _roleRepo.ExistsByNameAsync(dto.Name))
            {
                _logger.LogWarning("Create fail: Role {Name} already exists", dto.Name);
                throw new BusinessException("Role name already exists");
            }
            var role = new Role
            {
                Name = dto.Name,
                Description = dto.Description
            };

            await _roleRepo.AddAsync(role);

            if (dto.PermissionIds != null && dto.PermissionIds.Any())
            {
                // Reuse existing business rules for assigning permissions.
                await AssignPermissionsAsync(new AssignPermissionsDto
                {
                    RoleId = role.Id,
                    PermissionIds = dto.PermissionIds
                });
            }

            return ApiResponse<RoleResponseDto>.SuccessResponse(MapToResponseDto(role), "Created successfully");
        }
        public async Task<ApiResponse<RoleResponseDto>> UpdateAsync(int id, RoleUpdateDto dto)
        {
            var role = await _roleRepo.GetByIdAsync(id);
            if (role == null) throw new NotFoundException("Role not found");

            if (!role.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase))
            {
                if (await _roleRepo.ExistsByNameAsync(dto.Name))
                    throw new BusinessException("New role name already exists");
            }

            role.Name = dto.Name;
            role.Description = dto.Description;
            role.UpdatedAt = DateTime.UtcNow;

            await _roleRepo.UpdateAsync(role);
            return ApiResponse<RoleResponseDto>.
                SuccessResponse(MapToResponseDto(role), "Updated successfully");
        }
        public async Task<ApiResponse<bool>> AssignPermissionsAsync(AssignPermissionsDto dto)
        {
            var role = await _roleRepo.GetByIdWithPermissionsAsync(dto.RoleId);
            if (role == null) throw new NotFoundException("Role not found");

            var allExist = await _permissionRepo.AllIdsExistAsync(dto.PermissionIds);
            if (!allExist)
            {
                throw new BusinessException("One or more Permission IDs do not exist.");
            }

            role.RolePermissions.Clear();

            var uniquePermissionIds = dto.PermissionIds.Distinct();

            foreach (var pId in uniquePermissionIds)
            {
                role.RolePermissions.Add(new RolePermission
                {
                    RoleId = dto.RoleId,
                    PermissionId = pId
                });
            }

            await _roleRepo.UpdateAsync(role);
            return ApiResponse<bool>.SuccessResponse(true, "Permissions assigned successfully");
        }
        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            var role = await _roleRepo.GetByIdAsync(id);
            if (role == null) throw new NotFoundException("Role not found");

            if (role.Name.ToLower() == "admin")
                throw new BusinessException("Cannot delete system Admin role");

            if (await _roleRepo.IsRoleInUseAsync(id))
                throw new BusinessException("Role is in use by active users. Please reassign them first.");

            role.IsDeleted = true;
            role.UpdatedAt = DateTime.UtcNow;

            await _roleRepo.UpdateAsync(role);
            return ApiResponse<bool>.SuccessResponse(true, "Role deleted successfully");
        }
        private RoleResponseDto MapToResponseDto(Role role) => new RoleResponseDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        };
    }
}
