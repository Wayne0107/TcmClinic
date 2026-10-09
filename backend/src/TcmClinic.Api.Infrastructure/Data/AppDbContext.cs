using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TcmClinic.Api.Core.Auditing;
using TcmClinic.Api.Core.MenuAggregate;
using TcmClinic.Api.Core.RoleAggregate;
using TcmClinic.Api.Core.SessionAggregate;
using TcmClinic.Api.Core.UserAggregate;
using TcmClinic.Api.SharedKernel;

namespace TcmClinic.Api.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();

    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    public DbSet<RoleMenuVisibility> RoleMenuVisibilities => Set<RoleMenuVisibility>();

    public DbSet<User> Users => Set<User>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<AccessLog> AccessLogs => Set<AccessLog>();

    /// <summary>使用者操作紀錄（SPEC-013，append-only）。</summary>
    public DbSet<UserLog> UserLogs => Set<UserLog>();

    /// <summary>登入 session（SPEC-004 FR-021～FR-021c，ADR-011）。</summary>
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // 全域查詢過濾器：所有軟刪除實體預設排除 Removed=1（Principle XI）。
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var body = Expression.Not(
                    Expression.Property(parameter, nameof(ISoftDelete.Removed)));
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(Expression.Lambda(body, parameter));
            }
        }
    }
}
