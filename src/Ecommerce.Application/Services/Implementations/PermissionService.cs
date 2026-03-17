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

        private readonly ILogger<PermissionService> _logger;
        public PermissionService( 
            IPermissionRepo permissionRepo,
            ILogger<PermissionService> logger)
        {
            _permissionRepo = permissionRepo;
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
            var dto = new PermissionResponseDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description,
                
            };

            return ApiResponse<PermissionResponseDto>.SuccessResponse(dto);
        }

        public async Task<ApiResponse<PermissionResponseDto>> CreateAsync(PermissionCreateDto dto)
        {
            _logger.LogInformation("Create permission {Name}", dto.Name);
            var permission = new Permission
            {
                Name = dto.Name,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };
            await _permissionRepo.AddAsync(permission);

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
            permission.Name = dto.Name;
            permission.Description = dto.Description;
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

            permission.IsDeleted = true;

            await _permissionRepo.SaveChangesAsync();

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
            Description = p.Description,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
    }
}
