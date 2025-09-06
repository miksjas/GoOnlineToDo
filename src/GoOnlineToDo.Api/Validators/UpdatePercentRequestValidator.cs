using FluentValidation;
using GoOnlineToDo.Application.Contracts;

namespace GoOnlineToDo.Api.Validators;

public class UpdatePercentRequestValidator : AbstractValidator<UpdatePercentRequest>
{
    public UpdatePercentRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("ID must be greater than 0");

        RuleFor(x => x.PercentComplete)
          .InclusiveBetween(0, 100)
          .WithMessage("Percent complete must be between 0 and 100");
    }
}