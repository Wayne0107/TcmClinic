namespace TcmClinic.Api.Core.Auditing;

/// <summary>
/// 資料訪問類型（Principle II：以 SmartEnum 取代 plain enum）。以 <see cref="SmartEnum{T}.Name"/>
/// 字串持久化，便於查詢可讀與日後擴充。Read=查詢、Export=匯出、Download=下載。
/// </summary>
public sealed class AccessType : SmartEnum<AccessType>
{
    public static readonly AccessType Read = new(nameof(Read), 1);
    public static readonly AccessType Export = new(nameof(Export), 2);
    public static readonly AccessType Download = new(nameof(Download), 3);

    private AccessType(string name, int value) : base(name, value)
    {
    }
}
