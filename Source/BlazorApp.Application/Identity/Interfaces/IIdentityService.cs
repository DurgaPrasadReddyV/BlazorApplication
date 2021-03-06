using System.Security.Claims;
using BlazorApp.Application.Wrapper;
using BlazorApp.Shared.Identity;

namespace BlazorApp.Application.Identity.Interfaces;

public interface IIdentityService
{
    Task<IResult<string>> RegisterAsync(RegisterUserRequest request, string origin);

    Task<IResult<string>> ConfirmEmailAsync(string userId, string code);

    Task<IResult<string>> ConfirmPhoneNumberAsync(string userId, string code);

    Task<IResult> ForgotPasswordAsync(ForgotPasswordRequest request, string origin);

    Task<IResult> ResetPasswordAsync(ResetPasswordRequest request);

    Task<IResult> UpdateProfileAsync(UpdateProfileRequest request, string userId);

    Task<IResult> ChangePasswordAsync(ChangePasswordRequest request, string userId);
}