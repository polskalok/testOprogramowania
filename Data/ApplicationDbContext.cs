using Microsoft.EntityFrameworkCore;
using przychodnia.Models;
using System.Reflection.Emit;

namespace przychodnia.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // To mapuje Twoją tabelę z projekt.db na obiekty w C#
        public DbSet<Uzytkownik> Uzytkownicy { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Jeśli Twoja tabela w projekt.db nazywa się inaczej niż "Uzytkownicy" (np. "Users"), 
            // odkomentuj linię poniżej i wpisz właściwą nazwę:
            // modelBuilder.Entity<Uzytkownik>().ToTable("Uzytkownicy");
        }
    }
}