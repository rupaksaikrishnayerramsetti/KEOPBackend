using KEOPBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace KEOPBackend.Data
{
    public class SpentAnalysisDbContext : DbContext
    {
        public SpentAnalysisDbContext(DbContextOptions<SpentAnalysisDbContext> options)
            :base(options)
        {
        }
        public DbSet<SpentAnalysis> SpentAnalyses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SpentAnalysis>().HasKey(s => s.spent_id);
        }
    }
}
