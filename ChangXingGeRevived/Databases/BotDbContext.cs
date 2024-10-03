using ChangXingGeRevived.Models;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace ChangXingGeRevived.Databases;

public class BotDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<ExceptionRecord> ExceptionRecords { get; init; }
    public DbSet<CommandRecord> CommandRecords { get; init; }
    public DbSet<MessageRecord> MessageRecords { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ExceptionRecord>().ToCollection("exceptions");
        modelBuilder.Entity<CommandRecord>().ToCollection("commands");
        modelBuilder.Entity<MessageRecord>().ToCollection("messages");
    }
}
