using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TcmClinic.Api.Core.Auditing;

namespace TcmClinic.Api.Infrastructure.Data.Config;

/// <summary>
/// SysAuditLog 綱要（append-only 稽核表）。依 DATABASE-SCHEMA §6 例外僅主鍵採 bigint IDENTITY；
/// §1 五個標準欄位照常具備（CreateUser／CreateDate 記操作者與時間，Modify* 同值、Removed 恆為 0），
/// 並套用軟刪除全域過濾器（設計決策 R12，2026-09-30 修訂）。
/// 每欄與資料表皆附 zh-TW 描述（Principle XI）。永久保留，不設 TTL/自動清理（FR-016）。
/// </summary>
public class AuditLogConfig : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("SysAuditLog", t => t.HasComment(
            "資料異動紀錄（append-only）：記錄各資料表寫入的改前/改後整列快照、操作者與時間"));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedOnAdd() // bigint IDENTITY（append-only 稽核表例外，DATABASE-SCHEMA §6）
            .HasComment("主鍵：異動紀錄流水號（資料庫產生）");

        builder.Property(a => a.EntityName)
            .HasMaxLength(128).IsRequired()
            .HasComment("受異動的實體型別名（如 User）");

        builder.Property(a => a.TableName)
            .HasMaxLength(128).IsRequired()
            .HasComment("受異動的資料表名（如 SysUser）");

        builder.Property(a => a.PrimaryKey)
            .HasMaxLength(256).IsRequired()
            .HasComment("目標資料列主鍵字串（複合鍵以 k=v;k=v 表示）");

        builder.Property(a => a.Operation)
            .HasMaxLength(10).IsRequired()
            .HasComment("操作類型：Insert=新增／Update=修改／Delete=刪除（含軟刪除轉態）");

        builder.Property(a => a.OldValues)
            .HasComment("改前整列快照 JSON（Insert 為 null；憑證/機密已遮罩）");

        builder.Property(a => a.NewValues)
            .HasComment("改後整列快照 JSON（Delete 為 null；憑證/機密已遮罩）");

        builder.Property(a => a.CreateDate).HasColumnType("datetime").IsRequired();
        builder.Property(a => a.CreateUser).HasMaxLength(50).IsRequired();
        builder.Property(a => a.ModifyDate).HasColumnType("datetime").IsRequired();
        builder.Property(a => a.ModifyUser).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Removed).HasDefaultValue(false).IsRequired();

        builder.HasAuditColumnComments();

        // 本表語意：建立者即操作者、建立時間即異動發生時間，覆寫共用描述使 schema 自我說明。
        builder.Property(a => a.CreateDate).HasComment("異動發生時間（本地時區；INSERT 時設定一次，列不會被更新）");
        builder.Property(a => a.CreateUser).HasComment("操作者識別（帳號或系統標識）");

        // 查特定資料的異動歷史（FR-011）。
        builder.HasIndex(a => new { a.EntityName, a.PrimaryKey, a.CreateDate });

        // 依操作者 + 期間查詢（FR-011）。
        builder.HasIndex(a => new { a.CreateUser, a.CreateDate });
    }
}
