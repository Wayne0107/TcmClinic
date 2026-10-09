using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TcmClinic.Api.Core.SessionAggregate;
using TcmClinic.Api.Core.UserAggregate;

namespace TcmClinic.Api.Infrastructure.Data.Config;

/// <summary>登入 session 表 SysUserSession（SPEC-004 FR-021～FR-021c，ADR-011）。</summary>
public sealed class UserSessionConfig : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("SysUserSession", t => t.HasComment(
            "登入 session：一次登入一筆，保存目前 refresh token 的雜湊，每次換新即輪替（ADR-011）"));

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasConversion(id => id.Value, value => UserSessionId.From(value))
            .ValueGeneratedOnAdd()
            .UseIdentityColumn()
            .HasSentinel(UserSessionId.From(0))
            .HasComment("主鍵：session ID（強型別，資料庫 IDENTITY 配號）");

        builder.Property(s => s.PublicId)
            .HasConversion(id => id.Value, value => UserSessionPublicId.From(value))
            .ValueGeneratedNever()
            .IsRequired()
            .HasComment("對外識別碼（UUIDv7），即 access token 的 sid claim");
        builder.HasIndex(s => s.PublicId)
            .IsUnique()
            .HasDatabaseName("IX_SysUserSession_PublicId");

        builder.Property(s => s.UserId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired()
            .HasComment("登入的使用者 ID（外鍵至 SysUser）");
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("IX_SysUserSession_UserId");

        builder.Property(s => s.TokenHash).HasMaxLength(64).IsRequired()
            .HasComment("目前 refresh token 的 SHA-256 雜湊（小寫十六進位；原值不落地）");
        builder.HasIndex(s => s.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_SysUserSession_TokenHash");

        builder.Property(s => s.PreviousTokenHash).HasMaxLength(64)
            .HasComment("前一個（已輪替掉的）refresh token 雜湊，供重用偵測；從未輪替為 NULL");
        builder.HasIndex(s => s.PreviousTokenHash)
            .HasDatabaseName("IX_SysUserSession_PreviousTokenHash");

        builder.Property(s => s.ExpiresAt).HasColumnType("datetime").IsRequired()
            .HasComment("到期時間（本地時間；建立或輪替時為當時＋8 小時）");
        builder.Property(s => s.LastRotatedAt).HasColumnType("datetime")
            .HasComment("最後一次輪替時間（本地時間；從未輪替為 NULL）");
        builder.Property(s => s.RevokedAt).HasColumnType("datetime")
            .HasComment("撤銷時間（本地時間；NULL 表示未撤銷）");
        builder.Property(s => s.RevokeReason).HasMaxLength(20)
            .HasComment("撤銷原因：Logout 登出、Reuse 重用已輪替的 refresh token、UserInvalid 帳號刪除或無角色");

        builder.Property(s => s.CreateDate).HasColumnType("datetime").IsRequired();
        builder.Property(s => s.CreateUser).HasMaxLength(50).IsRequired();
        builder.Property(s => s.ModifyDate).HasColumnType("datetime").IsRequired();
        builder.Property(s => s.ModifyUser).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Removed).HasDefaultValue(false).IsRequired();
        builder.HasAuditColumnComments();
    }
}
