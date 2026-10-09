using Mediator;
using Microsoft.Extensions.Logging;

namespace TcmClinic.Api.SharedKernel;

/// <summary>
/// Mediator pipeline behavior：記錄每個訊息的處理前後（Chain of Responsibility，Principle III）。
/// </summary>
public sealed class LoggingBehavior<TMessage, TResponse>(
    ILogger<LoggingBehavior<TMessage, TResponse>> logger)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
{
    private readonly ILogger<LoggingBehavior<TMessage, TResponse>> _logger = logger;

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("處理中 {MessageType}", typeof(TMessage).Name);
        var response = await next(message, cancellationToken);
        _logger.LogInformation("已處理 {MessageType}", typeof(TMessage).Name);
        return response;
    }
}
