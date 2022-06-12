﻿using AspNet.CorrelationIdGenerator;
using Common.Messaging.Folder;
using Common.Messaging.Folder.Models;
using Microsoft.Extensions.Logging;
using WebBff.Application.Infrastructure;
using WebBff.Domain.Models;

namespace WebBff.Infrastructure;
public class MessageBusOutbox : IMessageBusOutbox
{
    private readonly IMessageOutbox<Booking> _messageOutbox;
    private readonly ICorrelationIdGenerator _correlationIdGenerator;
    private readonly ILogger<MessageBusOutbox> _logger;

    public MessageBusOutbox(IMessageOutbox<Booking> messageOutbox, 
        ICorrelationIdGenerator correlationIdGenerator, ILogger<MessageBusOutbox> logger)
    {
        _messageOutbox = messageOutbox;
        _correlationIdGenerator = correlationIdGenerator;
        _logger = logger;
    }

    public async Task PublishBookingCreatedAsync(Booking booking)
    {
        var correlationId = _correlationIdGenerator.Get();
        try
        {
            await _messageOutbox.AddAsync(new Message<Booking>(correlationId,
            booking));
        }
        catch (Exception ex)
        {
            LogError(correlationId, ex);
            throw;
        }
    }

    private void LogError(string correlationId, Exception ex) 
        => _logger.LogError(ex, "CorrelationId: {correlationId} - " +
            "Failed adding message to Outbox", correlationId);
}
