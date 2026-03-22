using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.Permission;
using Ecommerce.Application.DTOs.Role;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using System.Threading;

namespace Ecommerce.Application.Services.Implementations
{
    public class RoleService : IRoleService
    {
        private const string DefaultUserRoleName = "User";
        private static readonly TimeSpan RoleCacheTtl = TimeSpan.FromMinutes(30);
        private static readonly SemaphoreSlim _listLoadLock = new(1, 1);

        private readonly IRoleRepo _roleRepo;
        private readonly IPermissionRepo _permissionRepo;
        private readonly IUserRepo _userRepo;
        private readonly ICacheService _cacheService;
        private readonly IUnitOfWork _unitOfWork;

        public RoleService(
            IRoleRepo roleRepo,
            IPermissionRepo permissionRepo,
            IUserRepo userRepo,
            ICacheService cacheService,
            IUnitOfWork unitOfWork)
        {
            _roleRepo = roleRepo;
            _permissionRepo = permissionRepo;
            _userRepo = userRepo;
            _cacheService = cacheService;
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<PagedResponse<RoleResponseDto>>> GetAllAsync(PaginationDto pagedto)
        {
            var version = await _cacheService.GetVersionAsync(CacheKeyGenerator.RoleVersionKey());
            var cacheKey = CacheKeyGenerator.RoleList(version, pagedto.PageNumber, pagedto.PageSize);

            var pagedResponse = await _cacheService.GetAsync<PagedResponse<RoleResponseDto>>(cacheKey);
            if (pagedResponse != null)
                return ApiResponse<PagedResponse<RoleResponseDto>>.SuccessResponse(pagedResponse);

            await _listLoadLock.WaitAsync();
            try
            {
                version = await _cacheService.GetVersionAsync(CacheKeyGenerator.RoleVersionKey());
                cacheKey = CacheKeyGenerator.RoleList(version, pagedto.PageNumber, pagedto.PageSize);

                pagedResponse = await _cacheService.GetAsync<PagedResponse<RoleResponseDto>>(cacheKey);
                if (pagedResponse != null)
                    return ApiResponse<PagedResponse<RoleResponseDto>>.SuccessResponse(pagedResponse);

                var (roles, totalCount) = await _roleRepo.GetAllAsync(pagedto);
                var data = roles.Select(MapToResponseDto).ToList();
                pagedResponse = new PagedResponse<RoleResponseDto>(data, pagedto.PageNumber, pagedto.PageSize, totalCount);
                await _cacheService.SetAsync(cacheKey, pagedResponse, RoleCacheTtl);
            }
            finally
            {
                _listLoadLock.Release();
            }

            return ApiResponse<PagedResponse<RoleResponseDto>>.SuccessResponse(pagedResponse!);
        }

        public async Task<ApiResponse<RoleWithPermissionsDto>> GetByIdAsync(int id)
        {
            var cacheKey = CacheKeyGenerator.Role(id);

            var dto = await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                var role = await _roleRepo.GetByIdWithPermissionsAsync(id);
                if (role == null) return null;

                return new RoleWithPermissionsDto
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
            }, RoleCacheTtl);

            if (dto == null)
                throw new NotFoundException("Role not found");

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
                IsSystem = false,
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

            await _cacheService.IncrementAsync(CacheKeyGenerator.RoleVersionKey());
            if (dto.PermissionIds != null && dto.PermissionIds.Any())
                await _cacheService.IncrementAsync(CacheKeyGenerator.PermissionVersionKey());

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

            await _cacheService.RemoveAsync(CacheKeyGenerator.Role(id));
            await _cacheService.IncrementAsync(CacheKeyGenerator.RoleVersionKey());

            var updatedRole = await _roleRepo.GetByIdWithPermissionsAsync(id);

            if (updatedRole == null)
                throw new NotFoundException("Role not found after update");

            return ApiResponse<RoleResponseDto>.SuccessResponse(MapToResponseDto(updatedRole), "Updated successfully");
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

            await _cacheService.RemoveAsync(CacheKeyGenerator.Role(dto.RoleId));
            await _cacheService.IncrementAsync(CacheKeyGenerator.RoleVersionKey());
            await _cacheService.IncrementAsync(CacheKeyGenerator.PermissionVersionKey());

            return ApiResponse<bool>.SuccessResponse(true, "Permissions assigned successfully");
        }
        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            var role = await _roleRepo.GetByIdAsync(id);
            if (role == null) throw new NotFoundException("Role not found");

            if (role.IsSystem)
                throw new BusinessException("Cannot delete system role");

            var defaultUserRole = await _roleRepo.GetByNameRoleAsync(DefaultUserRoleName);
            if (defaultUserRole == null)
                throw new BusinessException("Default 'User' role not found. Cannot perform safe deletion.");

            if (role.Id == defaultUserRole.Id)
                throw new BusinessException("Cannot delete the default User role.");

            var affectedUserIds = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var ids = await _userRepo.ReassignUsersToRoleAsync(id, defaultUserRole.Id);
                role.IsDeleted = true;
                role.UpdatedAt = DateTime.UtcNow;
                return ids;
            });

            await _cacheService.RemoveAsync(CacheKeyGenerator.Role(id));
            await _cacheService.IncrementAsync(CacheKeyGenerator.RoleVersionKey());
            await _cacheService.IncrementAsync(CacheKeyGenerator.PermissionVersionKey());

            foreach (var userId in affectedUserIds)
                await _cacheService.RemoveAsync(CacheKeyGenerator.User(userId));

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
