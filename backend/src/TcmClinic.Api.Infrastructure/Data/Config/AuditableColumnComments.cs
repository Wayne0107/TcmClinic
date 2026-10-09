using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TcmClinic.Api.Infrastructure.Data.Config;

/// <summary>
/// 共用：為標準稽核欄與軟刪除欄集中設定 zh-TW 資料庫欄位描述（Principle XI —— 稽核欄描述
/// 「寫一次」，不逐一實體重複）。各 EntityConfiguration 於設定完稽核欄後呼叫一次即可。
/// </summary>
internal static class AuditableColumnComments
{
    /// <summary>
    /// 為 <c>CreateDate</c>/<c>CreateUser</c>/<c>ModifyDate</c>/<c>ModifyUser</c>/<c>Removed</c>
    /// 加上資料庫欄位描述（SQL Server <c>MS_Description</c>）。屬性名對應
    /// <see cref="TcmClinic.Api.SharedKernel.AuditableEntityBase{TId}"/>。
    /// </summary>
    /// <typeparam name="T">繼承 AuditableEntityBase 的實體型別。</typeparam>
    /// <param name="builder">目標實體的設定建構器。</param>
    /// <returns>同一個 <paramref name="builder"/>，以支援串接。</returns>
    public static EntityTypeBuilder<T> HasAuditColumnComments<T>(this EntityTypeBuilder<T> builder)
        where T : class
    {
        // 以字串屬性名設定，避免對每個實體型別各寫一次強型別 lambda（描述內容一致）。
        builder.Property("CreateDate").HasComment("建立時間（本地時區；INSERT 時設定一次，之後不變）");
        builder.Property("CreateUser").HasComment("建立者識別（使用者 ID 或帳號；INSERT 時設定一次）");
        builder.Property("ModifyDate").HasComment("最近更新時間（本地時區；每次 UPDATE 刷新）");
        builder.Property("ModifyUser").HasComment("最近更新者識別（每次 UPDATE 刷新）");
        builder.Property("Removed").HasComment("軟刪除旗標：1=已邏輯刪除，0=有效");
        return builder;
    }
}
