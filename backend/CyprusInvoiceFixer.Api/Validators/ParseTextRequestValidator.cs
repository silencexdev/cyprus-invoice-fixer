using CyprusInvoiceFixer.Api.Controllers;
using FluentValidation;

namespace CyprusInvoiceFixer.Api.Validators;

public class ParseTextRequestValidator : AbstractValidator<ParseTextRequest>
{
    public ParseTextRequestValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Text is required.")
            .MinimumLength(10).WithMessage("Please provide more invoice text (min 10 characters).")
            .MaximumLength(50000).WithMessage("Input text is too long (max 50,000 characters).");
    }
}
