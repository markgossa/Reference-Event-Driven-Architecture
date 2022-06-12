using FluentValidation;

namespace LSE.Stocks.Application.Services.Shares.Queries.GetSharePrice;

public class GetSharePriceQueryValidator : AbstractValidator<GetSharePriceQuery>
{
    public GetSharePriceQueryValidator()
    {
        RuleFor(v => v.TickerSymbol).MaximumLength(20).WithMessage("Ticker Symbol max length: 20");
        RuleFor(v => v.TickerSymbol).NotEmpty().WithMessage("Ticker Symbol cannot be empty");
    }
}
