using System.Linq.Dynamic.Core;
using BlazorApp.Application.Identity.Interfaces;
using BlazorApp.Application.Wrapper;
using BlazorApp.Domain.Common.Contracts;
using BlazorApp.Domain.Identity;
using BlazorApp.CommonInfrastructure.Identity.Models;
using BlazorApp.CommonInfrastructure.Mapping;
using BlazorApp.CommonInfrastructure.Persistence;
using BlazorApp.CommonInfrastructure.Persistence.Contexts;
using BlazorApp.Shared.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mapster;

namespace BlazorApp.CommonInfrastructure.Identity.Services;

public class UserService : IUserService
{
    private readonly UserManager<BlazorAppIdentityUser> _userManager;
    private readonly RoleManager<BlazorAppIdentityRole> _roleManager;
    private readonly IdentityDbContext _context;

    public UserService(
        UserManager<BlazorAppIdentityUser> userManager,
        RoleManager<BlazorAppIdentityRole> roleManager,
        IdentityDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<PaginatedResult<UserDetailsDto>> SearchAsync(UserListFilter filter)
    {
        var filters = new Filters<BlazorAppIdentityUser>();
        filters.Add(filter.IsActive.HasValue, x => x.IsActive == filter.IsActive);

        var query = _userManager.Users.ApplyFilter(filters);
        if (filter.AdvancedSearch is not null)
            query = query.AdvancedSearch(filter.AdvancedSearch);
        string? ordering = new OrderByConverter().ConvertBack(filter.OrderBy);
        query = !string.IsNullOrWhiteSpace(ordering) ? query.OrderBy(ordering) : query.OrderBy(a => a.Id);

        return await query.ToMappedPaginatedResultAsync<BlazorAppIdentityUser, UserDetailsDto>(filter.PageNumber, filter.PageSize);
    }

    public async Task<Result<List<UserDetailsDto>>> GetAllAsync()
    {
        var users = await _userManager.Users.AsNoTracking().ToListAsync();
        var result = users.Adapt<List<UserDetailsDto>>();
        return await Result<List<UserDetailsDto>>.SuccessAsync(result);
    }

    public async Task<IResult<UserDetailsDto>> GetAsync(string userId)
    {
        var user = await _userManager.Users.AsNoTracking().Where(u => u.Id == userId).FirstOrDefaultAsync();
        if (user is null)
        {
            return await Result<UserDetailsDto>.FailAsync("User Not Found.");
        }

        var result = user.Adapt<UserDetailsDto>();
        return await Result<UserDetailsDto>.SuccessAsync(result);
    }

    public async Task<IResult<UserRolesResponse>> GetRolesAsync(string userId)
    {
        var viewModel = new List<UserRoleDto>();
        var user = await _userManager.FindByIdAsync(userId);
        var roles = await _roleManager.Roles.AsNoTracking().ToListAsync();
        foreach (var role in roles)
        {
            var userRolesViewModel = new UserRoleDto
            {
                RoleId = role.Id,
                RoleName = role.Name,
                Description = role.Description
            };
            userRolesViewModel.Enabled = await _userManager.IsInRoleAsync(user, role.Name);

            viewModel.Add(userRolesViewModel);
        }

        var result = new UserRolesResponse { UserRoles = viewModel };
        return await Result<UserRolesResponse>.SuccessAsync(result);
    }

    public async Task<IResult<string>> AssignRolesAsync(string userId, UserRolesRequest request)
    {
        var user = await _userManager.Users.Where(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null)
        {
            return await Result<string>.FailAsync("User Not Found.");
        }

        if (request == null)
        {
            return await Result<string>.FailAsync("Invalid Request.");
        }

        var adminRole = request.UserRoles.Find(a => !a.Enabled && a.RoleName == DefaultRoles.Admin);
        if (adminRole != null)
        {
            request.UserRoles.Remove(adminRole);
        }

        foreach (var userRole in request.UserRoles)
        {
            // Check if Role Exists
            if (await _roleManager.FindByNameAsync(userRole.RoleName) != null)
            {
                if (userRole.Enabled)
                {
                    if (!await _userManager.IsInRoleAsync(user, userRole.RoleName))
                    {
                        await _userManager.AddToRoleAsync(user, userRole.RoleName);
                    }
                }
                else
                {
                    await _userManager.RemoveFromRoleAsync(user, userRole.RoleName);
                }
            }
        }

        return await Result<string>.SuccessAsync(userId, string.Format("User Roles Updated Successfully."));
    }

    public async Task<Result<List<PermissionDto>>> GetPermissionsAsync(string userId)
    {
        var userPermissions = new List<PermissionDto>();
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return await Result<List<PermissionDto>>.FailAsync("User Not Found.");
        }

        var roleNames = await _userManager.GetRolesAsync(user);
        foreach (var role in _roleManager.Roles.Where(r => roleNames.Contains(r.Name)).ToList())
        {
            var permissions = await _context.RoleClaims.Where(a => a.RoleId == role.Id && a.ClaimType == ClaimTypes.Permission).ToListAsync();
            var permissionResponse = permissions.Adapt<List<PermissionDto>>();
            userPermissions.AddRange(permissionResponse);
        }

        return await Result<List<PermissionDto>>.SuccessAsync(userPermissions.Distinct().ToList());
    }

    public async Task<int> GetCountAsync()
    {
        return await _userManager.Users.AsNoTracking().CountAsync();
    }

    public async Task<IResult> ToggleUserStatusAsync(ToggleUserStatusRequest request)
    {
        var user = await _userManager.Users.Where(u => u.Id == request.UserId).FirstOrDefaultAsync();
        if (user == null) return await Result<List<PermissionDto>>.FailAsync("User Not Found.");
        bool isAdmin = await _userManager.IsInRoleAsync(user, DefaultRoles.Admin);
        if (isAdmin)
        {
            return await Result.FailAsync("Administrators Profile's Status cannot be toggled");
        }

        if (user != null)
        {
            user.IsActive = request.ActivateUser;
            var identityResult = await _userManager.UpdateAsync(user);
        }

        return await Result.SuccessAsync();
    }
}