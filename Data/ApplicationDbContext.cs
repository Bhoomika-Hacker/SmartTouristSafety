using Microsoft.EntityFrameworkCore;
using SmartTouristSafety.Models;

namespace SmartTouristSafety.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Tourist> Tourists => Set<Tourist>();
        public DbSet<DigitalIdentity> DigitalIdentities => Set<DigitalIdentity>();
        public DbSet<GeoFenceZone> GeoFenceZones => Set<GeoFenceZone>();
        public DbSet<LocationLog> LocationLogs => Set<LocationLog>();
        public DbSet<Incident> Incidents => Set<Incident>();
        public DbSet<AlertLog> AlertLogs => Set<AlertLog>();
        public DbSet<AppUser> AppUsers => Set<AppUser>();
        public DbSet<PoliceStation> PoliceStations => Set<PoliceStation>();
        public DbSet<AreaReport> AreaReports => Set<AreaReport>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Tourist>()
                .HasOne(t => t.DigitalIdentity)
                .WithOne(d => d.Tourist!)
                .HasForeignKey<DigitalIdentity>(d => d.TouristId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Tourist>()
                .HasMany(t => t.LocationLogs)
                .WithOne(l => l.Tourist)
                .HasForeignKey(l => l.TouristId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Tourist>()
                .HasMany(t => t.Incidents)
                .WithOne(i => i.Tourist)
                .HasForeignKey(i => i.TouristId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Tourist>()
                .HasMany(t => t.AlertLogs)
                .WithOne(a => a.Tourist)
                .HasForeignKey(a => a.TouristId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LocationLog>()
                .HasOne(l => l.Zone)
                .WithMany()
                .HasForeignKey(l => l.ZoneId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AlertLog>()
                .HasOne(a => a.Incident)
                .WithMany()
                .HasForeignKey(a => a.IncidentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AlertLog>()
                .HasOne(a => a.Zone)
                .WithMany()
                .HasForeignKey(a => a.ZoneId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<AppUser>()
                .HasOne(u => u.Tourist)
                .WithMany()
                .HasForeignKey(u => u.TouristId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Tourist>()
                .HasIndex(t => t.PassportOrIdNumber)
                .IsUnique();

            modelBuilder.Entity<AreaReport>()
                .HasIndex(r => new { r.SourceName, r.ExternalReference })
                .IsUnique();
        }
    }
}
