using BusinessCardManager.Models;
using Microsoft.EntityFrameworkCore;

namespace BusinessCardManager.data
{
    public class BusinessCardDbContext  : DbContext
    {

        
            public BusinessCardDbContext(DbContextOptions<BusinessCardDbContext> options) : base(options)
            {
            }

 
            public DbSet<BusinessCard> BusinessCards { get; set; }

  
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BusinessCard>(entity =>
            {
                entity.HasKey(e => e.Id); 
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(b => b.Name).IsRequired().HasMaxLength(1000);
                entity.Property(b => b.Email).IsRequired().HasMaxLength(1000);
                entity.Property(b => b.Gender).IsRequired().HasMaxLength(1000);
                entity.Property(b => b.Address).IsRequired().HasMaxLength(1000);
               
            });
        }
        }
}
