using System.ComponentModel.DataAnnotations.Schema;

namespace TcmClinic.Api.SharedKernel;

/// <summary>
/// 讓實體能登記領域事件；由 Infrastructure 的 SaveChangesInterceptor 於儲存後收集派送。
/// </summary>
public abstract class HasDomainEventsBase
{
    private readonly List<DomainEventBase> _domainEvents = [];

    [NotMapped]
    public IReadOnlyCollection<DomainEventBase> DomainEvents => _domainEvents.AsReadOnly();

    protected void RegisterDomainEvent(DomainEventBase domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
