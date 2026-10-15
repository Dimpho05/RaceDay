using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Events -> EventTypes and Events -> Users (the Organiser)
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasOne<EventType>()
                  .WithMany()
                  .HasForeignKey(e => e.EventTypeID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(e => e.OrganiserID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Categories -> Events
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasOne<Event>()
                  .WithMany()
                  .HasForeignKey(c => c.EventID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Enrolments -> Users (the Participant), Events and Categories
        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(e => e.ParticipantID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Event>()
                  .WithMany()
                  .HasForeignKey(e => e.EventID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Category>()
                  .WithMany()
                  .HasForeignKey(e => e.CategoryID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Results -> Enrolments
        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasOne<Enrolment>()
                  .WithMany()
                  .HasForeignKey(r => r.EnrolmentID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Unique constraints ----------
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<EventType>()
            .HasIndex(t => t.TypeName)
            .IsUnique();

        // A participant can only enrol once per event
        modelBuilder.Entity<Enrolment>()
            .HasIndex(e => new { e.ParticipantID, e.EventID })
            .IsUnique();

        // One result per enrolment
        modelBuilder.Entity<Result>()
            .HasIndex(r => r.EnrolmentID)
            .IsUnique();

        // ---------- Check constraint ----------
        modelBuilder.Entity<User>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_Users_Role",
                "[Role] IN ('Organiser', 'Participant')"));

        // ---------- Default values ----------
        modelBuilder.Entity<User>()
            .Property(u => u.CreateAt)
            .HasDefaultValueSql("GETDATE()");

        modelBuilder.Entity<Event>()
            .Property(e => e.CreateAt)
            .HasDefaultValueSql("GETDATE()");

        modelBuilder.Entity<Enrolment>()
            .Property(e => e.EnrolmentDate)
            .HasDefaultValueSql("GETDATE()");

        modelBuilder.Entity<Enrolment>()
            .Property(e => e.EnrolmentStatus)
            .HasDefaultValue("Active");

        modelBuilder.Entity<Result>()
            .Property(r => r.ResultDate)
            .HasDefaultValueSql("GETDATE()");
    }
}