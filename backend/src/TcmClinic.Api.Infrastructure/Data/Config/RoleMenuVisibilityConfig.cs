using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TcmClinic.Api.Core.MenuAggregate;
using TcmClinic.Api.Core.RoleAggregate;

namespace TcmClinic.Api.Infrastructure.Data.Config;

public class RoleMenuVisibilityConfig : IEntityTypeConfiguration<RoleMenuVisibility>
{
    public void Configure(EntityTypeBuilder<RoleMenuVisibility> builder)
    {
        // 表名單數（DATABASE-SCHEMA §8；2026-09-30 由 SysRoleTasks 更名，設計決策 R8）。
        builder.ToTable("SysRoleTask", t => t.HasComment("角色↔選單可見性對應（哪個角色可看到哪個選單）"));

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasConversion(id => id.Value, value => RoleMenuVisibilityId.From(value))
            .ValueGeneratedOnAdd()
            .UseIdentityColumn()
            .HasSentinel(RoleMenuVisibilityId.From(0))
            .HasComment("主鍵：角色選單可見性 ID（強型別，資料庫 IDENTITY 配號）");

        builder.Property(v => v.RoleId)
            .HasColumnName("SysRoleId")
            .HasConversion(id => id.Value, value => RoleId.From(value))
            .IsRequired()
            .HasComment("角色 ID（外鍵至 SysRole）");

        builder.Property(v => v.MenuItemId)
            .HasColumnName("SysTaskId")
            .HasConversion(id => id.Value, value => MenuItemId.From(value))
            .IsRequired()
            .HasComment("選單項目 ID（外鍵至 SysTask）");

        builder.Property(v => v.CreateDate).HasColumnType("datetime").IsRequired();
        builder.Property(v => v.CreateUser).HasMaxLength(50).IsRequired();
        builder.Property(v => v.ModifyDate).HasColumnType("datetime").IsRequired();
        builder.Property(v => v.ModifyUser).HasMaxLength(50).IsRequired();
        builder.Property(v => v.Removed).HasDefaultValue(false).IsRequired();

        builder.HasAuditColumnComments();

        // (SysRoleId, SysTaskId) 於未刪除列唯一（Principle XI 篩選唯一索引）。
        builder.HasIndex(v => new { v.RoleId, v.MenuItemId })
            .IsUnique()
            .HasFilter("[Removed] = 0");

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(v => v.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<MenuItem>()
            .WithMany()
            .HasForeignKey(v => v.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
