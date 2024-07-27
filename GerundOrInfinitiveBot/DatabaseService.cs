using GerundOrInfinitiveBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GerundOrInfinitiveBot;

public class DatabaseService : DbContext
{
    private const string SqlServerConnectionKey = "SqlServerConnection";
    
    private readonly IConfigurationRoot _configurationRoot;
    public DbSet<Example> Examples { get; private set; }
    
    public DatabaseService(IConfigurationRoot configurationRoot)
    {
        _configurationRoot = configurationRoot;
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.LogTo(Console.WriteLine, LogLevel.Debug);
        optionsBuilder.UseSqlServer(_configurationRoot.GetConnectionString(SqlServerConnectionKey));
        
        //optionsBuilder.LogTo(_logStream.WriteLine, new[] { RelationalEventId.CommandExecuted });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

    }
}