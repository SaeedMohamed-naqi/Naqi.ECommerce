// src/Core/Naqi.ECommerce.Application/Common/Exceptions/ValidationException.cs
//
// Wraps FluentValidation's ValidationFailure list into a single exception
// with errors grouped by property name - convenient for returning a
// structured 400 response (both from the Api's JSON controllers and from
// the Dashboard's ModelState-based views).

using FluentValidation.Results;

namespace Naqi.ECommerce.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }
}