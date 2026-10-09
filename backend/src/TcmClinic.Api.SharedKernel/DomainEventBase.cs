using Mediator;

namespace TcmClinic.Api.SharedKernel;

/// <summary>
/// 領域事件基底；透過 Mediator 派送。時間採本地時間（Principle XI）。
/// </summary>
public abstract class DomainEventBase : INotification
{
    public DateTime DateOccurred { get; protected set; } = DateTime.Now;
}
