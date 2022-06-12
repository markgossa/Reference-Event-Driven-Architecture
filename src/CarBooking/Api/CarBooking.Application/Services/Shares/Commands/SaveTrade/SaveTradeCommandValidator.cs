using FluentValidation;

namespace LSE.Stocks.Application.Services.Shares.Commands.SaveTrade;

public class SaveTradeCommandValidator : AbstractValidator<SaveTradeCommand>
{
    public SaveTradeCommandValidator()
    {
        RuleFor(v => v.TickerSymbol).MaximumLength(20).WithMessage("Ticker Symbol max length: 20");
        RuleFor(v => v.TickerSymbol).NotEmpty().WithMessage("Ticker Symbol cannot be empty");
        RuleFor(v => v.Price).GreaterThan(0).WithMessage("Price must be greater than £0");
        RuleFor(v => v.Count).GreaterThan(0).WithMessage("Count must be greater than 0");
    }
}
