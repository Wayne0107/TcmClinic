using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TcmClinic.Api.Core.MenuAggregate;

namespace TcmClinic.Api.Infrastructure.Data.Config;

public class MenuItemConfig : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        // 表名單數（DATABASE-SCHEMA §8；2026-09-30 由 SysTasks 更名，設計決策 R8）。
        builder.ToTable("SysTask", t => t.HasComment("後台側邊選單項目（樹節點）"));

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasConversion(id => id.Value, value => MenuItemId.From(value))
            .ValueGeneratedOnAdd()
            .UseIdentityColumn()
            .HasSentinel(MenuItemId.From(0)) // From(0) 視為「尚未配號」，存檔時由資料庫 IDENTITY 指派
            .HasComment("主鍵：選單項目 ID（強型別，資料庫 IDENTITY 配號）");

        builder.Property(m => m.PublicId)
            .HasConversion(id => id.Value, value => MenuItemPublicId.From(value))
            .ValueGeneratedNever()
            .IsRequired()
            .HasComment("對外識別碼（UUIDv7，API 一律使用此值，不公開整數主鍵；SPEC-064）");
        builder.HasIndex(m => m.PublicId)
            .IsUnique()
            .HasDatabaseName("IX_SysTask_PublicId");

        // ParentId：自參考外鍵，NULL＝頂層（DATABASE-SCHEMA §5；2026-09-30 取代 0 哨兵，設計決策 R8）。
        // Restrict：仍有子選單的父選單不得實體刪除；一般刪除為軟刪除且有子項時由用例阻擋（FR-004）。循環由領域層阻擋（FR-018）。
        builder.Property(m => m.ParentId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (int?)null,
                value => value.HasValue ? MenuItemId.From(value.Value) : (MenuItemId?)null)
            .HasComment("父選單 ID（外鍵至 SysTask；NULL 表示頂層）");

        builder.HasOne<MenuItem>()
            .WithMany()
            .HasForeignKey(m => m.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(m => m.Name)
            .HasConversion(n => n.Value, value => MenuItemName.From(value))
            .HasMaxLength(30)
            .IsRequired()
            .HasComment("選單顯示名稱（同一父層下唯一）");

        builder.Property(m => m.Route).HasMaxLength(200)
            .HasComment("對應的前端內部路由；純容器父項可為空");
        builder.Property(m => m.Icon).HasMaxLength(50)
            .HasComment("選單圖示識別字（可空）");
        builder.Property(m => m.DisplayOrder).IsRequired()
            .HasComment("同層顯示排序（小到大）");
        builder.Property(m => m.IsEnabled).HasDefaultValue(true).IsRequired()
            .HasComment("是否啟用；停用則不顯示於任何使用者側邊選單");

        builder.Property(m => m.CreateDate).HasColumnType("datetime").IsRequired();
        builder.Property(m => m.CreateUser).HasMaxLength(50).IsRequired();
        builder.Property(m => m.ModifyDate).HasColumnType("datetime").IsRequired();
        builder.Property(m => m.ModifyUser).HasMaxLength(50).IsRequired();
        builder.Property(m => m.Removed).HasDefaultValue(false).IsRequired();

        builder.HasAuditColumnComments();

        // 同一父層下名稱唯一（未刪除列，FR-007）。自訂篩選取代 EF 對可空欄位預設加的 IS NOT NULL，
        // SQL Server 唯一索引將多個 NULL 視為相同值，故頂層名稱同樣唯一。
        builder.HasIndex(m => new { m.ParentId, m.Name })
            .IsUnique()
            .HasFilter("[Removed] = 0");
    }
}
