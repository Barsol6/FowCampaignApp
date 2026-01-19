using FluentValidation;
using FowCampaign.Api.DTO;

namespace FowCampaign.Api.Modules.Account;

public class RegisterValidator : AbstractValidator<RegisterDto>
{
    public RegisterValidator ()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
            .MaximumLength(20).WithMessage("Username must not exceed 20 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters.");
        
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match.");
    }
}