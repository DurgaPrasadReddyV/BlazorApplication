using BlazorApp.Application.Wrapper;
using BlazorApp.Shared.Identity;

namespace BlazorApp.Application.Identity.Interfaces;

public interface IUserService
{
    Task<PaginatedResult<UserDetailsDto>> SearchAsync(UserListFilter filter);

    Task<Result<List<UserDetailsDto>>> GetAllAsync();

    Task<int> GetCountAsync();

    Task<IResult<UserDetailsDto>> GetAsync(string userId);

    Task<IResult<UserRolesResponse>> GetRolesAsync(string userId);

    Task<IResult<string>> AssignRolesAsync(string userId, UserRolesRequest request);

    Task<Result<List<PermissionDto>>> GetPermissionsAsync(string id);

    Task<IResult> ToggleUserStatusAsync(ToggleUserStatusRequest request);
}