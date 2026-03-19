using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.Permission;
using Ecommerce.Application.DTOs.Role;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Microsoft.Extensions.Logging;


namespace Ecommerce.Application.Services.Implementations
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepo _roleRepo;
        private readonly IPermissionRepo _permissionRepo;
        public RoleService(
            IRoleRepo roleRepo,
            IPermissionRepo permissionRepo)
        {
            _roleRepo = roleRepo;
            _permissionRepo = permissionRepo;
        }
        public async Task<ApiResponse<PagedResponse<RoleResponseDto>>> GetAllAsync(PaginationDto pagedto)
        {
            var (roles, totalCount) = await _roleRepo.GetAllAsync(pagedto);

            var data = roles.Select(r => MapToResponseDto(r)).ToList();

            var pagedResponse = new PagedResponse<RoleResponseDto>(
                data,
                pagedto.PageNumber,
                pagedto.PageSize,
                totalCount);

            return ApiResponse<PagedResponse<RoleResponseDto>>.SuccessResponse(pagedResponse);
        }
        public async Task<ApiResponse<RoleWithPermissionsDto>> GetByIdAsync(int id)
        {
            var role = await _roleRepo.GetByIdWithPermissionsAsync(id);
            if (role == null)
                throw new NotFoundException("Role not found");
            

            var dto = new RoleWithPermissionsDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                Permissions = role.RolePermissions
                            .Where(rp => rp.Permission != null)
                            .Select(rp => new PermissionResponseDto
                            {
                                Id = rp.Permission!.Id,
                                Name = rp.Permission.Name,
                                Entity = rp.Permission.Entity,
                                Action = rp.Permission.Action,
                                Description = rp.Permission.Description
                            })
                            .ToList()
            };

            return ApiResponse<RoleWithPermissionsDto>.SuccessResponse(dto);
        }

        public async Task<ApiResponse<RoleResponseDto>> CreateAsync(RoleCreateDto dto)
        {
            if (await _roleRepo.ExistsByNameAsync(dto.Name))
              throw new BusinessException("Role name already exists");
            
            if (dto.PermissionIds != null && dto.PermissionIds.Any())
            {
                var allExist = await _permissionRepo.AllIdsExistAsync(dto.PermissionIds);
                if (!allExist)
                {
                    throw new BusinessException("One or more Permission IDs do not exist.");
                }
            }
            var role = new Role
            {
                Name = dto.Name,
                Description = dto.Description,
                RolePermissions = dto.PermissionIds?
                    .Distinct()
                    .Select(pId => new RolePermission
                    {
                        PermissionId = pId
                    }).ToList() ?? new List<RolePermission>()
            };

            await _roleRepo.AddAsync(role);

            var createdRole = await _roleRepo.GetByIdWithPermissionsAsync(role.Id);
            if (createdRole == null)
            {
                throw new Exception("Role just created but not found"); 
            }

            return ApiResponse<RoleResponseDto>.SuccessResponse(MapToResponseDto(createdRole), "Created successfully");
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

            var updatedRole = await _roleRepo.GetByIdWithPermissionsAsync(id);

            if (updatedRole == null)
                throw new NotFoundException("Role not found after update");

            return ApiResponse<RoleResponseDto>.
                SuccessResponse(MapToResponseDto(updatedRole), "Updated successfully");
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
            Permissions = role.RolePermissions
                        .Where(rp => rp.Permission != null)
                        .Select(rp => rp.Permission.Name)
                        .ToList(),
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        };
    }
}
