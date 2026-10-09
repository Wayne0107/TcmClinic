using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TcmClinic.Api.Core.RoleAggregate;

namespace TcmClinic.Api.Infrastructure.Data.Config;

public class RoleConfig : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        // 表名單數（DATABASE-SCHEMA §8；2026-09-30 由 SysRoles 更名，後台選單與角色權限管理 設計決策 R8）。
        builder.ToTable("SysRole", t => t.HasComment("角色（一組職務權限集合）"));

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => RoleId.From(value))
            .ValueGeneratedOnAdd()
            .UseIdentityColumn()
            .HasSentinel(RoleId.From(0)) // From(0) 視為「尚未配號」，存檔時由資料庫 IDENTITY 指派
            .HasComment("主鍵：角色 ID（強型別，資料庫 IDENTITY 配號）");

        builder.Property(r => r.PublicId)
            .HasConversion(id => id.Value, value => RolePublicId.From(value))
            .ValueGeneratedNever()
            .IsRequired()
            .HasComment("對外識別碼（UUIDv7，API 一律使用此值，不公開整數主鍵；SPEC-064）");
        builder.HasIndex(r => r.PublicId)
            .IsUnique()
            .HasDatabaseName("IX_SysRole_PublicId");

        builder.Property(r => r.Code)
            .HasConversion(c => c.Value, value => RoleCode.From(value))
            .HasMaxLength(50)
            .IsRequired()
            .HasComment("角色代碼（未刪除列唯一；SUPERADMIN 為內建保護角色）");

        builder.Property(r => r.Name)
            .HasConversion(n => n.Value, value => RoleName.From(value))
            .HasMaxLength(20)
            .IsRequired()
            .HasComment("角色顯示名稱");

        builder.Property(r => r.Description)
            .HasMaxLength(200)
            .HasComment("角色說明（可空）");

        // 角色種類（012）：以 SmartEnum.Name 字串持久化；既有列由 Migration 回填預設 Custom。
        builder.Property(r => r.RoleType)
            .HasConversion(t => t.Name, name => RoleType.FromName(name, false))
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(RoleType.Custom)
            .HasComment("角色種類：System 系統管理員類（不可刪除）／Custom 一般使用者類");

        // 樂觀併發權杖（012）：設定權限儲存時比對，防止靜默覆蓋（FR-015）。
        builder.Property(r => r.RowVersion)
            .IsRowVersion()
            .HasComment("樂觀併發權杖：防止並行設定權限時靜默覆蓋");

        builder.Property(r => r.CreateDate).HasColumnType("datetime").IsRequired();
        builder.Property(r => r.CreateUser).HasMaxLength(50).IsRequired();
        builder.Property(r => r.ModifyDate).HasColumnType("datetime").IsRequired();
        builder.Property(r => r.ModifyUser).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Removed).HasDefaultValue(false).IsRequired();

        builder.HasAuditColumnComments();

        // RoleCode 於未刪除列唯一（Principle XI 篩選唯一索引）。
        builder.HasIndex(r => r.Code)
            .IsUnique()
            .HasFilter("[Removed] = 0");
    }
}
