using Microsoft.AspNetCore.Identity;

namespace BlazorApp.CommonInfrastructure.Identity.Models;

public partial class BlazorAppIdentityUser : IdentityUser
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime RefreshTokenExpiryTime { get; set; }

    public string? ObjectId { get; set; }
}