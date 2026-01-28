using Microsoft.EntityFrameworkCore;

namespace HMACServerApp.Models
{
    public class HMACDbContext : DbContext
    {
        public HMACDbContext(DbContextOptions<HMACDbContext> options) : base(options){

        }

        public DbSet<Employee> Employees { get; set; }

        public DbSet<ClientSecret> ClientSecrets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ClientSecret>().HasData(
                new ClientSecret { Id = 1 , ClientId = "WebAppClient" , SecretKey = "a1b2c3d4e5f6g7h8i9j0"},
                new ClientSecret { Id = 2 , ClientId = "MobileAppClient" , SecretKey = "z9y8x7w6v5u4t3s2r1q0"},
                new ClientSecret { Id = 3 , ClientId = "DesktopClient" , SecretKey = "m1n2b3v4c5x6z7i8k9j0"}
                );

            modelBuilder.Entity<Employee>().HasData(
                new Employee { Id = 1, Name = "Alice Johnson", Position = "Software Engineer", Salary = 75000m },
                new Employee { Id = 2, Name = "Bob Smith", Position = "Project Manager", Salary = 85000m }
                );
        }
    }
}
