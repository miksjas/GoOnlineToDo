using FluentValidation;

namespace GoOnlineToDo.Api.Validators;

public record IdRequest(int Id);

public class IdValidator : AbstractValidator<IdRequest>
{
    public IdValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("ID must be greater than 0");
    }
}