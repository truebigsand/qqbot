using ChangXingGeRevived.Models;
using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore;
namespace ChangXingGeRevived.Databases;

public class BotDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<ExceptionRecord> ExceptionRecords { get; init; }
    public DbSet<CommandRecord> CommandRecords { get; init; }
    public DbSet<MessageRecord> MessageRecords { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ExceptionRecord>().ToTable("exceptions");
        modelBuilder.Entity<CommandRecord>().ToTable("commands");
        modelBuilder.Entity<MessageRecord>().ToTable("messages");
    }
}
