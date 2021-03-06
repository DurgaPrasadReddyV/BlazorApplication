using System.Security.Claims;

namespace BlazorApp.Application.Identity.Interfaces;

public interface ICurrentUser
{
    string? Name { get; }

    Guid GetUserId();

    string? GetUserEmail();

    bool IsAuthenticated();

    bool IsInRole(string role);

    IEnumerable<Claim>? GetUserClaims();

    void SetUser(ClaimsPrincipal user);

    void SetUserJob(string userId);
}