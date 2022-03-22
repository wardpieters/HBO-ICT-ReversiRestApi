using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ReversiRestApi.Model;

namespace ReversiRestApi.DAL
{
    public class ReversiContext : DbContext
    {
        public ReversiContext(DbContextOptions<ReversiContext> options) : base(options) { }
        public DbSet<Spel> Spellen { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<Spel>()
                .Property(s => s.Bord)
                .HasConversion(
                    bord => JsonConvert.SerializeObject(bord),
                    bord => JsonConvert.DeserializeObject<Kleur[,]>(bord));
            
            base.OnModelCreating(modelBuilder);
        }
    }
}