namespace TcmClinic.Api.SharedKernel;

/// <summary>
/// 標記整個實體排除於資料異動紀錄（SPEC-006 排除清單，FR-014）。被標記者其新增／修改／刪除
/// 「不」產生稽核紀錄。稽核紀錄實體本身亦恆被排除（避免對稽核表再稽核造成遞迴）。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class NotAuditedAttribute : Attribute;
