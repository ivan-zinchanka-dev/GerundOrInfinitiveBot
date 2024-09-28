using GerundOrInfinitiveBot.DataBaseObjects;
using GerundOrInfinitiveBot.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GerundOrInfinitiveBot.Services.Database;

public class DatabaseService : DbContext
{
    private readonly IOptions<ConnectionSettings> _options;
    private readonly ILogger<DatabaseService> _logger;

    public DbSet<Example> Examples { get; private set; }
    public DbSet<UserData> UserData { get; private set; }
    public DbSet<Answer> Answers { get; private set; }

    public DatabaseService(IOptions<ConnectionSettings> options, ILogger<DatabaseService> logger)
    {
        _options = options;
        _logger = logger;
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.LogTo(OnLogFilter, OnLogAction);
        //optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.UseSqlServer(_options.Value.SqlServerConnection);
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Example>()
            .HasKey(example => example.Id);

        modelBuilder.Entity<UserData>(userData =>
        {
            userData
                .HasKey(user => user.UserId);
            
            userData
                .HasOne(user => user.CurrentExample)
                .WithMany(example => example.CurrentUsers)
                .HasForeignKey(user => user.CurrentExampleId);
        });

        modelBuilder.Entity<Answer>(answer =>
        {
            answer
                .HasKey(ans => ans.Id);

            answer
                .HasOne(ans => ans.UserData)
                .WithMany(user => user.Answers)
                .HasForeignKey(ans => ans.UserId);

            answer
                .HasOne(ans => ans.Example)
                .WithMany(example => example.AnswersWithIt)
                .HasForeignKey(ans => ans.ExampleId);
            
        });
    }

    private static bool OnLogFilter(EventId eventId, LogLevel logLevel)
    {
        return (int) logLevel >= (int) LogLevel.Information;
    }
    
    private void OnLogAction(EventData eventData)
    {
        _logger.Log(eventData.LogLevel, eventData.EventId, eventData.ToString());
    }
}