using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;

namespace EventManagementSystem.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Registrar> Registrars { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Food> Foods { get; set; }
        public DbSet<Equipment> Equipment { get; set; }
        public DbSet<Flower> Flowers { get; set; }
        public DbSet<Light> Lights { get; set; }
        public DbSet<BookingService> BookingServices { get; set; }
        public DbSet<VenueReviewModel> VenueReviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Configure decimal properties for Booking
            modelBuilder.Entity<Booking>()
                .Property(b => b.CleaningFee)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Booking>()
                .Property(b => b.ServiceFee)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Booking>()
                .Property(b => b.TotalPrice)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Booking>()
                .Property(b => b.AdditionalServicesCost)
                .HasPrecision(18, 2);

            // Configure decimal properties for Equipment
            modelBuilder.Entity<Equipment>()
                .Property(e => e.PricePerDay)
                .HasPrecision(18, 2);

            // Configure decimal properties for Flower
            modelBuilder.Entity<Flower>()
                .Property(f => f.Price)
                .HasPrecision(18, 2);

            // Configure decimal properties for Food
            modelBuilder.Entity<Food>()
                .Property(f => f.PricePerPerson)
                .HasPrecision(18, 2);

            // Configure decimal properties for Light
            modelBuilder.Entity<Light>()
                .Property(l => l.PricePerDay)
                .HasPrecision(18, 2);

            // Configure decimal properties for Registrar
            modelBuilder.Entity<Registrar>()
                .Property(r => r.TotalRevenue)
                .HasPrecision(18, 2);

            // Configure decimal properties for Venue
            modelBuilder.Entity<Venue>()
                .Property(v => v.CleaningFee)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Venue>()
                .Property(v => v.PricePerHour)
                .HasPrecision(18, 2);

            // Configure AdditionalImages property as nullable
            modelBuilder.Entity<Venue>()
                .Property(v => v.AdditionalImages)
                .IsRequired(false)
                .HasDefaultValue("");

            // Configure decimal properties for BookingService
            modelBuilder.Entity<BookingService>()
                .Property(bs => bs.ServicePrice)
                .HasPrecision(18, 2);

            // Define relationship between Booking and BookingService
            modelBuilder.Entity<BookingService>()
                .HasOne<Booking>()
                .WithMany(b => b.Services)
                .HasForeignKey(bs => bs.BookingId);

            // Configure VenueReview entity and relationships
            modelBuilder.Entity<VenueReviewModel>(entity =>
            {
                entity.ToTable("VenueReviews");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasOne(e => e.Venue)
                    .WithMany(v => v.Reviews)
                    .HasForeignKey(e => e.VenueId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed admin user
            modelBuilder.Entity<User>().HasData(
                new User
                {
            Id = 1,
            Email = "admin@example.com",
            Username = "admin",
            Password = "admin123", 
            FirstName = "Admin",
            LastName = "User",
            PhoneNumber = "1234567890",
            ProfilePicture = "", 
            CreatedAt = DateTime.SpecifyKind(new DateTime(2025, 1, 1, 0, 0, 0), DateTimeKind.Utc),
            Role = UserRole.Admin
        }
            );
        }
    }
} 