using KEOPBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace KEOPBackend.Data
{
    public class AlertsDbContext : DbContext
    {
        public AlertsDbContext(DbContextOptions<AlertsDbContext> options)
            : base(options)
        {
        }

        public DbSet<Alerts> Alerts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Alerts>().HasKey(n => n.alert_id);
        }
    }
}
