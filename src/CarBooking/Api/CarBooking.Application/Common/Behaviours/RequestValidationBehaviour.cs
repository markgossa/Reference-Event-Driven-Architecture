using FluentValidation;
using MediatR;

namespace LSE.Stocks.Application.Common.Behaviours;

public class RequestValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public RequestValidationBehaviour(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, 
        RequestHandlerDelegate<TResponse> next)
    {
        var context = new ValidationContext<TRequest>(request);

        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(r => r != null)
            .ToList();

        return failures.Count > 0
            ? throw new ValidationException(failures)
            : next();
    }
}
