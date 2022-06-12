using LSE.Stocks.Application.Repositories;
using LSE.Stocks.Domain.Models.Shares;
using MediatR;

namespace LSE.Stocks.Application.Services.Shares.Commands.SaveTrade;

internal class SaveTradeCommandHandler : IRequestHandler<SaveTradeCommand>
{
    private readonly ITradeRepository _tradeRepository;

    public SaveTradeCommandHandler(ITradeRepository tradeRepository)
        => _tradeRepository = tradeRepository;

    public async Task<Unit> Handle(SaveTradeCommand saveTradeCommand, CancellationToken cancellationToken)
    {
        await _tradeRepository.SaveTradeAsync(MapToTrade(saveTradeCommand));

        return Unit.Value;
    }

    private static Trade MapToTrade(SaveTradeCommand saveTradeCommand) 
        => new (saveTradeCommand.TickerSymbol, saveTradeCommand.Price,
            saveTradeCommand.Count, saveTradeCommand.BrokerId);
}
