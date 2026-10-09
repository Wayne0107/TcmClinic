using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TcmClinic.Api.Core.UserAggregate;

namespace TcmClinic.Api.Infrastructure.Data.Config;

public class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("SysUser", t => t.HasComment("後台使用者（操作者帳號）"));

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .ValueGeneratedOnAdd()
            .UseIdentityColumn()
            .HasSentinel(UserId.From(0))
            .HasComment("主鍵：使用者 ID（強型別，資料庫 IDENTITY 配號）");

        builder.Property(u => u.PublicId)
            .HasConversion(id => id.Value, value => UserPublicId.From(value))
            .ValueGeneratedNever()
            .IsRequired()
            .HasComment("對外識別碼（UUIDv7，API 一律使用此值，不公開整數主鍵；SPEC-064）");
        builder.HasIndex(u => u.PublicId)
            .IsUnique()
            .HasDatabaseName("IX_SysUser_PublicId");

        builder.Property(u => u.Account)
            .HasConversion(a => a.Value, value => UserAccount.From(value))
            .HasMaxLength(50)
            .IsRequired()
            .HasComment("登入帳號（未刪除列唯一；小寫正規化）");

        builder.Property(u => u.DisplayName)
            .HasConversion(n => n.Value, value => UserName.From(value))
            .HasMaxLength(100)
            .IsRequired()
            .HasComment("使用者顯示名稱");

        builder.Property(u => u.PasswordHash).HasMaxLength(88).IsRequired()
            .HasComment("密碼雜湊（加鹽；MUST NOT 記錄於日誌，Principle XVII）");
        builder.Property(u => u.PasswordSalt).HasMaxLength(32).IsRequired()
            .HasComment("密碼雜湊用鹽值");

        // SPEC-013：個人資料維護的聯絡資料與密碼效期（皆可空，既有列不回填，設計決策 R5/R6）。
        builder.Property(u => u.Phone)
            .HasConversion(
                p => p.HasValue ? p.Value.Value : null,
                value => value != null ? PhoneNumber.From(value) : (PhoneNumber?)null)
            .HasMaxLength(PhoneNumber.MaxLength)
            .HasComment("聯絡電話（選填；數字、空白與 + - ( ) #）");

        builder.Property(u => u.Email)
            .HasConversion(
                e => e.HasValue ? e.Value.Value : null,
                value => value != null ? EmailAddress.From(value) : (EmailAddress?)null)
            .HasMaxLength(EmailAddress.MaxLength)
            .HasComment("電子郵件（選填）");

        builder.Property(u => u.PasswordChangedAt)
            .HasColumnType("datetime")
            .HasComment("上次修改密碼時間（本地時間；NULL 表示上線前建立、以 CreateDate 代替）");

        builder.Property(u => u.CreateDate).HasColumnType("datetime").IsRequired();
        builder.Property(u => u.CreateUser).HasMaxLength(50).IsRequired();
        builder.Property(u => u.ModifyDate).HasColumnType("datetime").IsRequired();
        builder.Property(u => u.ModifyUser).HasMaxLength(50).IsRequired();
        builder.Property(u => u.Removed).HasDefaultValue(false).IsRequired();

        builder.HasAuditColumnComments();

        // 帳號於未刪除列唯一（Principle XI 篩選唯一索引），軟刪除後可重用（US5 情境 3）。
        builder.HasIndex(u => u.Account)
            .IsUnique()
            .HasFilter("[Removed] = 0");

        // Cascade：清除/刪除使用者時，其角色關聯一併移除；經稽核攔截器轉為軟刪除（Removed=1）。
        // 避免必填關聯被切斷（severed）而拋錯。角色端（UserRoleConfig）維持 Restrict，僅單一連鎖路徑。
        builder.HasMany(u => u.UserRoles)
            .WithOne()
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.UserRoles)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
