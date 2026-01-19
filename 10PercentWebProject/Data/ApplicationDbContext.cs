using _10PercentWebProject.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace _10PercentWebProject.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        // ADD THIS PARAMETERLESS CONSTRUCTOR FOR MIGRATIONS
        public ApplicationDbContext() { }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
/*        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Migration ke time yeh use hoga
                optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=MedicineDB;Trusted_Connection=True;");
            }
        }*/
        public DbSet<Medicine> Medicines { get; set; }

    }
}