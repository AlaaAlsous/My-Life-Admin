using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using My_Life_Admin.Models;

namespace My_Life_Admin.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(u =>
        {
            u.Property(x => x.Name).HasMaxLength(200);
            u.Property(x => x.Email).HasMaxLength(256);
            u.Property(x => x.PasswordHash).HasMaxLength(500);
            u.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Document>(d =>
        {
            d.Property(x => x.Title).HasMaxLength(200);
            d.Property(x => x.FileName).HasMaxLength(255);
            d.Property(x => x.Category).HasMaxLength(100);
            d.Property(x => x.Description).HasMaxLength(1000);
            d.Property(x => x.FilePath).HasMaxLength(500);
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Category).HasMaxLength(100);
        });

        modelBuilder.Entity<Event>()
            .HasOne(e => e.Document)
            .WithMany(d => d.Events)
            .HasForeignKey(e => e.DocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Reminder>()
            .HasOne(r => r.Event)
            .WithMany(e => e.Reminders)
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Reminder>()
            .HasOne(r => r.Document)
            .WithMany(d => d.Reminders)
            .HasForeignKey(r => r.DocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<UserSettings>()
            .HasOne(s => s.User)
            .WithOne(u => u.Settings)
            .HasForeignKey<UserSettings>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChatMessage>(c =>
        {
            c.Property(x => x.Content).IsRequired().HasMaxLength(4000);
            c.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            c.HasIndex(x => new { x.UserId, x.CreatedAt });
            c.HasOne(x => x.User)
                .WithMany(u => u.ChatMessages)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
