using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TcmClinic.Api.Core.Auditing;

namespace TcmClinic.Api.Infrastructure.Data.Config;

/// <summary>
/// SysAccessLog 綱要（append-only 稽核表）。依 DATABASE-SCHEMA §6 例外僅主鍵採 bigint IDENTITY；
/// §1 五個標準欄位照常具備（CreateUser／CreateDate 記訪問者與時間，Modify* 同值、Removed 恆為 0），
/// 並套用軟刪除全域過濾器（設計決策 R7，2026-09-30 修訂）。
/// 每欄與資料表皆附 zh-TW 描述（Principle XI／FR-003）。永久保留，不設 TTL/自動清理（FR-006）。
/// </summary>
public class AccessLogConfig : IEntityTypeConfiguration<AccessLog>
{
    public void Configure(EntityTypeBuilder<AccessLog> builder)
    {
        builder.ToTable("SysAccessLog", t => t.HasComment(
            "資料訪問紀錄（append-only）：記錄使用者對敏感資料的讀取/匯出/下載事件，僅記被訪問的敏感欄位名稱、不記機密值"));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedOnAdd() // bigint IDENTITY（append-only 稽核表例外，DATABASE-SCHEMA §6）
            .HasComment("主鍵：訪問紀錄流水號（資料庫產生）");

        builder.Property(a => a.AccessType)
            .HasMaxLength(50).IsRequired()
            .HasComment("訪問類型：Read=查詢／Export=匯出／Download=下載");

        builder.Property(a => a.EntityName)
            .HasMaxLength(128).IsRequired()
            .HasComment("被訪問的實體型別名（如 User）");

        builder.Property(a => a.PrimaryKey)
            .HasMaxLength(256)
            .HasComment("被訪問資料的主鍵字串（單筆讀取如 123；匯出多筆時為 null）");

        builder.Property(a => a.RecordCount)
            .IsRequired()
            .HasComment("本次訪問涉及的資料筆數（單筆讀取通常為 1，匯出/下載為實際筆數）");

        builder.Property(a => a.ClientIp)
            .HasMaxLength(45)
            .HasComment("來源用戶端 IP 位址（IPv4/IPv6；背景作業或無法取得時為 null）");

        builder.Property(a => a.InvolvesSensitiveData)
            .IsRequired()
            .HasComment("是否涉及敏感資料（被訪問實體含 @SensitiveData 標記欄位）");

        builder.Property(a => a.SensitiveFieldsAccessed)
            .HasComment("被訪問的敏感欄位名稱清單 JSON（如 [\"PasswordHash\",\"PasswordSalt\"]；僅記名不記值）");

        builder.Property(a => a.Purpose)
            .HasMaxLength(500)
            .HasComment("訪問目的或原因說明（選填，供稽核追溯）");

        builder.Property(a => a.CreateDate).HasColumnType("datetime").IsRequired();
        builder.Property(a => a.CreateUser).HasMaxLength(50).IsRequired();
        builder.Property(a => a.ModifyDate).HasColumnType("datetime").IsRequired();
        builder.Property(a => a.ModifyUser).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Removed).HasDefaultValue(false).IsRequired();

        builder.HasAuditColumnComments();

        // 本表語意：建立者即訪問者、建立時間即訪問發生時間，覆寫共用描述使 schema 自我說明。
        builder.Property(a => a.CreateDate).HasComment("訪問發生時間（本地時區；INSERT 時設定一次，列不會被更新）");
        builder.Property(a => a.CreateUser).HasComment("訪問者識別（帳號或系統標識）");

        // 查特定資料的訪問歷史（FR-011）。
        builder.HasIndex(a => new { a.EntityName, a.PrimaryKey, a.CreateDate });

        // 監控敏感資料訪問（FR-011）。
        builder.HasIndex(a => new { a.InvolvesSensitiveData, a.CreateDate });

        // 依訪問者 + 期間查詢（FR-011）。
        builder.HasIndex(a => new { a.CreateUser, a.CreateDate });
    }
}
