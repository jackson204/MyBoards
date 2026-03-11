using Microsoft.EntityFrameworkCore;

namespace MyBoards.Entities;

public class MyBoardsContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MyBoardsDb;Trusted_Connection=True;");
        base.OnConfiguring(optionsBuilder);
    }

    public DbSet<WorkItem> WorkItems { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<Tag> Tags { get; set; }

    public DbSet<Comment> Comments { get; set; }

    public DbSet<Address> Addresses { get; set; }
}
