using BlazorApp.Application.Identity.Interfaces;
using BlazorApp.Application.Wrapper;
using BlazorApp.Domain.Identity;
using BlazorApp.CommonInfrastructure.Identity.Models;
using BlazorApp.CommonInfrastructure.Persistence.Contexts;
using BlazorApp.Shared.Identity;
using Microsoft.EntityFrameworkCore;
using Mapster;

namespace BlazorApp.CommonInfrastructure.Identity.Services;

public class RoleClaimsService : IRoleClaimsService
{
    private readonly IdentityDbContext _db;

    public RoleClaimsService(IdentityDbContext context)
    {
        _db = context;
    }

    public async Task<bool> HasPermissionAsync(string userId, string permission)
    {
        var userRoles = await _db.UserRoles.Where(a => a.UserId == userId).Select(a => a.RoleId).ToListAsync();
        var applicationRoles = await _db.Roles.Where(a => userRoles.Contains(a.Id)).ToListAsync();
        var roles = applicationRoles.Adapt<List<RoleDto>>();

        if (roles is not null)
        {
            foreach (var role in roles)
            {
                if (_db.RoleClaims.Any(a => a.ClaimType == ClaimTypes.Permission && a.ClaimValue == permission && a.RoleId == role.Id))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public async Task<Result<List<RoleClaimResponse>>> GetAllAsync()
    {
        var roleClaims = await _db.RoleClaims.ToListAsync();
        var roleClaimsResponse = roleClaims.Adapt<List<RoleClaimResponse>>();
        return await Result<List<RoleClaimResponse>>.SuccessAsync(roleClaimsResponse);
    }

    public Task<int> GetCountAsync() =>
        _db.RoleClaims.CountAsync();

    public async Task<Result<RoleClaimResponse>> GetByIdAsync(int id)
    {
        var roleClaim = await _db.RoleClaims
            .SingleOrDefaultAsync(x => x.Id == id);
        if (roleClaim is null)
        {
            return await Result<RoleClaimResponse>.FailAsync("Role not found.");
        }

        var roleClaimResponse = roleClaim.Adapt<RoleClaimResponse>();
        return await Result<RoleClaimResponse>.SuccessAsync(roleClaimResponse);
    }

    public async Task<Result<List<RoleClaimResponse>>> GetAllByRoleIdAsync(string roleId)
    {
        var roleClaims = await _db.RoleClaims
            .Where(x => x.RoleId == roleId)
            .ToListAsync();
        var roleClaimsResponse = roleClaims.Adapt<List<RoleClaimResponse>>();
        return await Result<List<RoleClaimResponse>>.SuccessAsync(roleClaimsResponse);
    }

    public async Task<Result<string>> SaveAsync(RoleClaimRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RoleId))
        {
            return await Result<string>.FailAsync("Role is required.");
        }

        if (request.Id == 0)
        {
            var existingRoleClaim =
                await _db.RoleClaims
                    .SingleOrDefaultAsync(x =>
                        x.RoleId == request.RoleId && x.ClaimType == request.Type && x.ClaimValue == request.Value);
            if (existingRoleClaim is not null)
            {
                return await Result<string>.FailAsync("Similar Role Claim already exists.");
            }

            var roleClaim = request.Adapt<BlazorAppIdentityRoleClaim>();
            await _db.RoleClaims.AddAsync(roleClaim);
            await _db.SaveChangesAsync();
            return await Result<string>.SuccessAsync(string.Format("Role Claim {0} created.", request.Value));
        }
        else
        {
            var existingRoleClaim =
                await _db.RoleClaims
                    .SingleOrDefaultAsync(x => x.Id == request.Id);
            if (existingRoleClaim is null)
            {
                return await Result<string>.SuccessAsync("Role Claim does not exist.");
            }
            else
            {
                existingRoleClaim.ClaimType = request.Type;
                existingRoleClaim.ClaimValue = request.Value;
                existingRoleClaim.Group = request.Group;
                existingRoleClaim.Description = request.Description;
                existingRoleClaim.RoleId = request.RoleId;
                _db.RoleClaims.Update(existingRoleClaim);
                await _db.SaveChangesAsync();
                return await Result<string>.SuccessAsync(string.Format("Role Claim {0} for Role updated.", request.Value));
            }
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        var existingRoleClaim = await _db.RoleClaims
            .FirstOrDefaultAsync(x => x.Id == id);
        if (existingRoleClaim is not null)
        {
            _db.RoleClaims.Remove(existingRoleClaim);
            await _db.SaveChangesAsync();
            return await Result<string>.SuccessAsync(string.Format("Role Claim {0} for Role deleted.", existingRoleClaim.ClaimValue));
        }
        else
        {
            return await Result<string>.FailAsync("Role Claim does not exist.");
        }
    }
}