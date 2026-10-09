using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.UserAggregate;

namespace TcmClinic.Api.Infrastructure.Data.Config;

public class UserRoleConfig : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        // 表名單數（DATABASE-SCHEMA §8；2026-09-30 由 SysUserRoles 更名，使用者管理與登入 設計決策 R9）。
        builder.ToTable("SysUserRole", t => t.HasComment("使用者↔角色指派（多對多）"));

        builder.HasKey(ur => ur.Id);

        builder.Property(ur => ur.Id)
            .HasConversion(id => id.Value, value => UserRoleId.From(value))
            .ValueGeneratedOnAdd()
            .UseIdentityColumn()
            .HasSentinel(UserRoleId.From(0))
            .HasComment("主鍵：使用者角色指派 ID（強型別，資料庫 IDENTITY 配號）");

        builder.Property(ur => ur.UserId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired()
            .HasComment("使用者 ID（外鍵至 SysUser）");

        builder.Property(ur => ur.RoleId)
            .HasConversion(id => id.Value, value => RoleId.From(value))
            .IsRequired()
            .HasComment("角色 ID（外鍵至 SysRole）");

        builder.Property(ur => ur.CreateDate).HasColumnType("datetime").IsRequired();
        builder.Property(ur => ur.CreateUser).HasMaxLength(50).IsRequired();
        builder.Property(ur => ur.ModifyDate).HasColumnType("datetime").IsRequired();
        builder.Property(ur => ur.ModifyUser).HasMaxLength(50).IsRequired();
        builder.Property(ur => ur.Removed).HasDefaultValue(false).IsRequired();

        builder.HasAuditColumnComments();

        // 顯式外鍵至角色（Principle XI）；使用者刪除採軟刪除，故 Restrict。
        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // 同一使用者不重複指派同一角色（未刪除範圍）。
        builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
            .IsUnique()
            .HasFilter("[Removed] = 0");
    }
}
