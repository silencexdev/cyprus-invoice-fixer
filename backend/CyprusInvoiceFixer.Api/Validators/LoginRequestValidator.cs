using CyprusInvoiceFixer.Api.Controllers;
using FluentValidation;

namespace CyprusInvoiceFixer.Api.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
