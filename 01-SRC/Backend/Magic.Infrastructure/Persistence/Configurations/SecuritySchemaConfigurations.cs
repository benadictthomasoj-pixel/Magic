using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Magic.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<UserRecord>
{
    public void Configure(EntityTypeBuilder<UserRecord> builder)
    {
        builder.ToTable("users", "security");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Username).HasMaxLength(100).IsRequired();
        builder.Property(x => x.NormalizedUsername).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.NormalizedEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PasswordSalt).HasMaxLength(200).IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.NormalizedUsername }).IsUnique();
        builder.HasIndex(x => new { x.CompanyId, x.NormalizedEmail }).IsUnique();
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<RoleRecord>
{
    public void Configure(EntityTypeBuilder<RoleRecord> builder)
    {
        builder.ToTable("roles", "security");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<PermissionRecord>
{
    public void Configure(EntityTypeBuilder<PermissionRecord> builder)
    {
        builder.ToTable("permissions", "security");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Module).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRoleRecord>
{
    public void Configure(EntityTypeBuilder<UserRoleRecord> builder)
    {
        builder.ToTable("user_roles", "security");
        builder.HasKey(x => new { x.UserId, x.RoleId });

        builder.HasOne(x => x.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermissionRecord>
{
    public void Configure(EntityTypeBuilder<RolePermissionRecord> builder)
    {
        builder.ToTable("role_permissions", "security");
        builder.HasKey(x => new { x.RoleId, x.PermissionId });

        builder.HasOne(x => x.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class UserBranchAssignmentConfiguration : IEntityTypeConfiguration<UserBranchAssignmentRecord>
{
    public void Configure(EntityTypeBuilder<UserBranchAssignmentRecord> builder)
    {
        builder.ToTable("user_branch_assignments", "security");
        builder.HasKey(x => new { x.UserId, x.BranchId });

        builder.HasOne(x => x.User)
            .WithMany(u => u.BranchAssignments)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.CompanyId, x.BranchId });
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshTokenRecord>
{
    public void Configure(EntityTypeBuilder<RefreshTokenRecord> builder)
    {
        builder.ToTable("refresh_tokens", "security");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash).HasMaxLength(256).IsRequired();
        builder.Property(x => x.RevocationReason).HasMaxLength(200);
        builder.Property(x => x.ReplacedByTokenHash).HasMaxLength(256);
        builder.Property(x => x.CreatedByIp).HasMaxLength(100);

        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.TokenFamily);
        builder.HasIndex(x => new { x.UserId, x.IsRevoked });

        builder.HasOne(x => x.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TerminalConfiguration : IEntityTypeConfiguration<TerminalRecord>
{
    public void Configure(EntityTypeBuilder<TerminalRecord> builder)
    {
        builder.ToTable("terminals", "security");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TerminalCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.MachineName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OperatingSystem).HasMaxLength(100);

        builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.TerminalCode }).IsUnique();
    }
}

public sealed class SecurityAuditEventConfiguration : IEntityTypeConfiguration<SecurityAuditEventRecord>
{
    public void Configure(EntityTypeBuilder<SecurityAuditEventRecord> builder)
    {
        builder.ToTable("audit_events", "security");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Username).HasMaxLength(100);
        builder.Property(x => x.IpAddress).HasMaxLength(100);

        builder.HasIndex(x => new { x.CompanyId, x.TimestampUtc });
        builder.HasIndex(x => x.EventType);
    }
}
