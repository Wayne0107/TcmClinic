namespace TcmClinic.Api.SharedKernel;

/// <summary>
/// 標記實體屬性為憑證／機密（如密碼雜湊、salt、token、cookie、連線字串）。資料異動紀錄
/// （SPEC-006）於產生改前／改後快照時，被標記的屬性值「必須」以遮罩取代，機密永不落入
/// 稽核紀錄（Principle XVII、FR-004）。一般識別性個資（帳號、姓名）不標記、照實記錄。
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SensitiveDataAttribute : Attribute;
