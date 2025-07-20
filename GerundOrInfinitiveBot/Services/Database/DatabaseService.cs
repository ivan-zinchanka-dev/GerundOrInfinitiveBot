using GerundOrInfinitiveBot.Models.DataBaseObjects;
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

    public DatabaseService(
        IOptions<ConnectionSettings> options, 
        ILogger<DatabaseService> logger)
    {
        _options = options;
        _logger = logger;
        
        Database.EnsureCreated();
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.LogTo(OnLogFilter, OnLogAction);
        optionsBuilder.UseSqlServer(_options.Value.SqlServerConnection);
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Example>(example =>
        {
            example
                .HasKey(ex => ex.Id);

            /*example
                .Property(ex => ex.Id)
                .IsRequired()
                .ValueGeneratedOnAdd();*/
            
            example
                .Property(ex => ex.SourceSentence)
                .HasMaxLength(100);

            example
                .Property(ex => ex.UsedWord)
                .HasMaxLength(30);
            
            example
                .Property(ex => ex.CorrectAnswer)
                .HasMaxLength(30);
            
            example
                .Property(ex => ex.AlternativeCorrectAnswer)
                .HasMaxLength(30);
        });
        
        modelBuilder.Entity<UserData>(userData =>
        {
            userData
                .HasKey(user => user.UserId);

            userData
                .Property(user => user.UserId)
                .IsRequired()
                .ValueGeneratedNever();
            
            userData
                .HasOne(user => user.CurrentExample)
                .WithMany(example => example.CurrentUsers)
                .HasForeignKey(user => user.CurrentExampleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Answer>(answer =>
        {
            answer
                .HasKey(ans => ans.Id);

            answer.Property(ans => ans.Id)
                .IsRequired()
                .HasDefaultValueSql("NEWID()");

            answer.Property(ans => ans.UserId).IsRequired();
            answer.Property(ans => ans.ExampleId).IsRequired();
            answer.Property(ans => ans.ReceivingTime).IsRequired();
            answer.Property(ans => ans.Result).IsRequired();
            
            answer
                .HasOne(ans => ans.UserData)
                .WithMany(user => user.Answers)
                .HasForeignKey(ans => ans.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            answer
                .HasOne(ans => ans.Example)
                .WithMany(example => example.AnswersWithIt)
                .HasForeignKey(ans => ans.ExampleId)
                .OnDelete(DeleteBehavior.Cascade);
            
        });
    }

    private static bool OnLogFilter(EventId eventId, LogLevel logLevel)
    {
        return (int) logLevel >= (int) LogLevel.Warning;
    }
    
    private void OnLogAction(EventData eventData)
    {
        _logger.Log(eventData.LogLevel, eventData.EventId, eventData.ToString());
    }
}