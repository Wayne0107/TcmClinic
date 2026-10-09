using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TcmClinic.Api.Core.Auditing;

namespace TcmClinic.Api.Infrastructure.Data.Config;

/// <summary>
/// SysUserLog 綱要（append-only 使用者操作紀錄，SPEC-013）。依 DATABASE-SCHEMA §6 例外僅主鍵採 bigint IDENTITY；
/// §1 五個標準欄位照常具備（CreateUser／CreateDate 記操作者與時間，Modify* 同值、Removed 恆為 0），
/// 並套用軟刪除全域過濾器（設計決策 R8，2026-09-30 修訂）。每欄與資料表皆附 zh-TW 描述；永久保留，供「使用者存取記錄」查詢。
/// </summary>
public class UserLogConfig : IEntityTypeConfiguration<UserLog>
{
    public void Configure(EntityTypeBuilder<UserLog> builder)
    {
        builder.ToTable("SysUserLog", t => t.HasComment(
            "使用者操作紀錄（append-only）：記錄重要業務操作的成功／失敗，供『使用者存取記錄』查詢"));

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .ValueGeneratedOnAdd() // bigint IDENTITY（append-only 稽核表例外）
            .HasComment("主鍵：操作紀錄流水號（資料庫產生）");

        builder.Property(l => l.FunctionId)
            .HasMaxLength(50).IsRequired()
            .HasComment("功能代碼（如 PersonalData）");

        builder.Property(l => l.Action)
            .HasMaxLength(50).IsRequired()
            .HasComment("動作（如 Modify）");

        builder.Property(l => l.Status)
            .HasMaxLength(20).IsRequired()
            .HasComment("結果：Success=成功／Failure=失敗");

        builder.Property(l => l.Content)
            .HasMaxLength(500).IsRequired()
            .HasComment("操作內容訊息（不得含密碼）");

        builder.Property(l => l.ClientIp)
            .HasMaxLength(45)
            .HasComment("來源用戶端 IP 位址（IPv4/IPv6；背景作業或無法取得時為 null）");

        builder.Property(l => l.CreateDate).HasColumnType("datetime").IsRequired();
        builder.Property(l => l.CreateUser).HasMaxLength(50).IsRequired();
        builder.Property(l => l.ModifyDate).HasColumnType("datetime").IsRequired();
        builder.Property(l => l.ModifyUser).HasMaxLength(50).IsRequired();
        builder.Property(l => l.Removed).HasDefaultValue(false).IsRequired();

        builder.HasAuditColumnComments();

        // 本表語意：建立者即操作者、建立時間即操作時間，覆寫共用描述使 schema 自我說明。
        builder.Property(l => l.CreateDate).HasComment("操作時間（本地時區；INSERT 時設定一次，列不會被更新）");
        builder.Property(l => l.CreateUser).HasComment("操作者帳號（無登入者時為 system／anonymous）");

        // 依操作者 + 期間查詢（使用者存取記錄）。
        builder.HasIndex(l => new { l.CreateUser, l.CreateDate });

        // 依功能 + 期間查詢（使用者存取記錄）。
        builder.HasIndex(l => new { l.FunctionId, l.CreateDate });
    }
}
