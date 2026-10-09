namespace TcmClinic.Api.SharedKernel;

/// <summary>
/// 實體基底；TId 使用 Vogen 強型別 ID（Principle VII）。
/// </summary>
public abstract class EntityBase<TId> : HasDomainEventsBase
{
    public TId Id { get; set; } = default!;
}
