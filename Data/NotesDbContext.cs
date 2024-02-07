using KEOPBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace KEOPBackend.Data
{
    public class NotesDbContext: DbContext
    {
        public NotesDbContext(DbContextOptions<NotesDbContext> options) 
            : base(options)
        {
        }

        public DbSet<Notes> Notes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Notes>().HasKey(n => n.note_id);
        }
    }
}
