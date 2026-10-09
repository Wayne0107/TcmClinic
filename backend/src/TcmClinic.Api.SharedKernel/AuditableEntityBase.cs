namespace TcmClinic.Api.SharedKernel;

/// <summary>
/// 具稽核欄位與軟刪除的實體基底（Principle XI）。稽核欄位與 Removed 由攔截器管理，
/// 應用／用例層不得手動設定。
/// </summary>
public abstract class AuditableEntityBase<TId> : EntityBase<TId>, IAuditable, ISoftDelete
{
    public DateTime CreateDate { get; set; }
    public string CreateUser { get; set; } = string.Empty;
    public DateTime ModifyDate { get; set; }
    public string ModifyUser { get; set; } = string.Empty;
    public bool Removed { get; set; }
}
