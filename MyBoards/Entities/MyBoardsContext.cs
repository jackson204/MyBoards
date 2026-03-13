using Microsoft.EntityFrameworkCore;

namespace MyBoards.Entities;

public class MyBoardsContext : DbContext
{
    public MyBoardsContext(DbContextOptions<MyBoardsContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkItem>(e=>
        {
            e.Property(item => item.State).IsRequired();
            e.Property(item => item.Area).HasColumnType("nvarchar(200)");
            e.Property(item => item.IterationPath).HasColumnName("Iteration_Path").HasColumnType("nvarchar(200)");
            e.Property(item => item.Efford).HasColumnType("decimal(5,2)");
            e.Property(item => item.EndDate).HasPrecision(3);
            e.Property(item => item.Activity).HasMaxLength(200);
            e.Property(item => item.RemainingWork).HasPrecision(14, 2);
        });
          
           
        base.OnModelCreating(modelBuilder);
        
    }

    public DbSet<WorkItem> WorkItems { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<Tag> Tags { get; set; }

    public DbSet<Comment> Comments { get; set; }

    public DbSet<Address> Addresses { get; set; }
}
