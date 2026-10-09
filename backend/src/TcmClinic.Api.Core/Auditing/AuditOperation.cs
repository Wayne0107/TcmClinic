namespace TcmClinic.Api.Core.Auditing;

/// <summary>
/// 資料異動操作類型（Principle II：以 SmartEnum 取代 plain enum）。以 <see cref="SmartEnum{T}.Name"/>
/// 字串持久化，便於查詢可讀與日後擴充。Insert=新增、Update=修改、Delete=刪除（含軟刪除轉態）。
/// </summary>
public sealed class AuditOperation : SmartEnum<AuditOperation>
{
    public static readonly AuditOperation Insert = new(nameof(Insert), 1);
    public static readonly AuditOperation Update = new(nameof(Update), 2);
    public static readonly AuditOperation Delete = new(nameof(Delete), 3);

    private AuditOperation(string name, int value) : base(name, value)
    {
    }
}
