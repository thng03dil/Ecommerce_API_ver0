using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.Permission;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using System.Threading;

namespace Ecommerce.Application.Services.Implementations
{
    public class PermissionService : IPermissionService
    {
        private static readonly TimeSpan PermissionCacheTtl = TimeSpan.FromMinutes(30);
        private static readonly SemaphoreSlim _listLoadLock = new(1, 1);
        private static readonly SemaphoreSlim _itemLoadLock = new(1, 1);
        private static readonly SemaphoreSlim _writeLock = new(1, 1);

        private readonly IPermissionRepo _permissionRepo;
        private readonly ICacheService _cacheService;

        public PermissionService(
            IPermissionRepo permissionRepo,
            ICacheService cacheService)
        {
            _permissionRepo = permissionRepo;
            _cacheService = cacheService;
        }

        public async Task<ApiResponse<PagedResponse<PermissionResponseDto>>> GetAllAsync(PaginationDto pagedto)
        {
            var version = await _cacheService.GetVersionAsync(CacheKeyGenerator.PermissionVersionKey());
            var cacheKey = CacheKeyGenerator.PermissionList(version, pagedto.PageNumber, pagedto.PageSize);

            var pagedResponse = await _cacheService.GetAsync<PagedResponse<PermissionResponseDto>>(cacheKey);
            if (pagedResponse != null)
                return ApiResponse<PagedResponse<PermissionResponseDto>>.SuccessResponse(pagedResponse);

            await _listLoadLock.WaitAsync();
            try
            {
                version = await _cacheService.GetVersionAsync(CacheKeyGenerator.PermissionVersionKey());
                cacheKey = CacheKeyGenerator.PermissionList(version, pagedto.PageNumber, pagedto.PageSize);

                pagedResponse = await _cacheService.GetAsync<PagedResponse<PermissionResponseDto>>(cacheKey);
                if (pagedResponse != null)
                    return ApiResponse<PagedResponse<PermissionResponseDto>>.SuccessResponse(pagedResponse);

                var (permissions, totalCount) = await _permissionRepo.GetAllAsync(pagedto);
                var data = permissions.Select(r => MapToResponseDto(r)).ToList();
                pagedResponse = new PagedResponse<PermissionResponseDto>(
                    data,
                    pagedto.PageNumber,
                    pagedto.PageSize,
                    totalCount);
                await _cacheService.SetAsync(cacheKey, pagedResponse, PermissionCacheTtl);
            }
            finally
            {
                _listLoadLock.Release();
            }

            return ApiResponse<PagedResponse<PermissionResponseDto>>.SuccessResponse(pagedResponse!);
        }

        public async Task<ApiResponse<PermissionResponseDto>> GetByIdAsync(int id)
        {
            var cacheKey = CacheKeyGenerator.Permission(id);

            // Check 1: Cache
            var cached = await _cacheService.GetAsync<PermissionResponseDto>(cacheKey);
            if (cached != null) return ApiResponse<PermissionResponseDto>.SuccessResponse(cached);

            await _itemLoadLock.WaitAsync();
            try
            {
                // Check 2: Double-check
                cached = await _cacheService.GetAsync<PermissionResponseDto>(cacheKey);
                if (cached != null) return ApiResponse<PermissionResponseDto>.SuccessResponse(cached);

                var permission = await _permissionRepo.GetByIdAsync(id);
                if (permission == null) throw new NotFoundException("Permission not found");

                var dto = MapToResponseDto(permission);
                await _cacheService.SetAsync(cacheKey, dto, PermissionCacheTtl);
                return ApiResponse<PermissionResponseDto>.SuccessResponse(dto);
            }
            finally { _itemLoadLock.Release(); }
        }

        public async Task<ApiResponse<PermissionResponseDto>> CreateAsync(PermissionCreateDto dto)
        {
            await _writeLock.WaitAsync();
            try
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
                    Description = dto.Description?.Trim() ?? string.Empty,
                    CreatedAt = DateTime.UtcNow
                };
                await _permissionRepo.AddAsync(permission);
                await _permissionRepo.SaveChangesAsync();

                await _cacheService.IncrementAsync(CacheKeyGenerator.PermissionVersionKey());

                var item = MapToResponseDto(permission);
                return ApiResponse<PermissionResponseDto>.SuccessResponse(
                       item,
                        "Create data successfully"
                        );
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public async Task<ApiResponse<PermissionResponseDto>> UpdateAsync(int id, PermissionUpdateDto dto)
        {
            await _writeLock.WaitAsync();
            try
            {
                var permission = await _permissionRepo.GetByIdAsync(id);
                if (permission == null) throw new NotFoundException("Permission not found");

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

                await _cacheService.RemoveAsync(CacheKeyGenerator.Permission(id));
                await _cacheService.IncrementAsync(CacheKeyGenerator.PermissionVersionKey());
                await _cacheService.IncrementAsync(CacheKeyGenerator.RoleVersionKey());
                await _cacheService.RemoveByPrefixAsync(CacheKeyGenerator.RolePermissionCachePrefix());

                var item = MapToResponseDto(permission);
                return ApiResponse<PermissionResponseDto>.SuccessResponse(
                       item,
                       "Update data successfully"
                       );
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public async Task<ApiResponse<PermissionResponseDto>> DeleteAsync(int id)
        {
            await _writeLock.WaitAsync();
            try
            {
                var permission = await _permissionRepo.GetByIdAsync(id);

                if (permission == null) throw new NotFoundException("Permission not found");

                await _permissionRepo.HardDeleteRolePermissionsByPermissionIdAsync(permission.Id);

                permission.IsDeleted = true;
                await _permissionRepo.SaveChangesAsync();

                await _cacheService.RemoveAsync(CacheKeyGenerator.Permission(id));
                await _cacheService.IncrementAsync(CacheKeyGenerator.PermissionVersionKey());
                await _cacheService.IncrementAsync(CacheKeyGenerator.RoleVersionKey());
                await _cacheService.RemoveByPrefixAsync(CacheKeyGenerator.RolePermissionCachePrefix());

                var item = MapToResponseDto(permission);
                return ApiResponse<PermissionResponseDto>.SuccessResponse(
                         item,
                        "Delete data successfully"
                );
            }
            finally
            {
                _writeLock.Release();
            }
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
