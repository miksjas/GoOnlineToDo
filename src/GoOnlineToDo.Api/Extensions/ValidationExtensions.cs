using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GoOnlineToDo.Api.Extensions;

public static class ValidationExtensions
{
    public static async Task<Results<ValidationProblem, TResult>> ValidateAsync<T, TResult>(
        this T request,
        IValidator<T> validator,
        Func<Task<TResult>> onValid)
        where TResult : IResult
    {
        var validationResult = await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ErrorMessage).ToArray()
                );

            return TypedResults.ValidationProblem(errors);
        }

        return await onValid();
    }
}