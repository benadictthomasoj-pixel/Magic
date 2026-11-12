using FluentValidation;
using Magic.Contracts.Api;
using Magic.Domain.Common;

namespace Magic.Application.Validation;

/// <summary>
/// Exception carrying field-level FluentValidation failures translated to RFC 7807 Problem Details.
/// </summary>
public sealed class ApplicationValidationException : DomainException
{
    public ApplicationValidationException(IReadOnlyList<FieldValidationError> errors)
        : base("APP_VALIDATION_FAILED", "One or more validation errors occurred.", 400)
    {
        Errors = errors;
    }

    public IReadOnlyList<FieldValidationError> Errors { get; }
}

/// <summary>
/// Shared helper executing FluentValidation validators uniformly across application use cases.
/// </summary>
public static class ValidationRunner
{
    public static async Task ValidateAndThrowAsync<T>(
        IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(instance);

        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors
            .Select(failure => new FieldValidationError(failure.PropertyName, failure.ErrorMessage))
            .ToArray();

        throw new ApplicationValidationException(errors);
    }
}

