using FluentValidation;
using GoOnlineToDo.Application.Contracts;

namespace GoOnlineToDo.Api.Validators;

public class UpdateTodoRequestValidator : AbstractValidator<UpdateTodoRequest>
{
    public UpdateTodoRequestValidator()
    {
        RuleFor(x => x.Title)
          .NotEmpty()
          .WithMessage("Title is required")
          .MaximumLength(200)
          .WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Description)
          .MaximumLength(1000)
          .WithMessage("Description cannot exceed 1000 characters")
          .When(x => x.Description != null);

        RuleFor(x => x.DueDate)
          .GreaterThanOrEqualTo(DateTime.Today)
          .WithMessage("Expiry date cannot be in the past");

        RuleFor(x => x.PercentComplete)
          .InclusiveBetween(0, 100)
          .WithMessage("Percent complete must be between 0 and 100");
    }
}