using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore.Explorer.Tests;

public class AutoComputedExplorerTests
{
    [Fact]
    public async Task InspectorDistinguishesStoredAndCalculatedValues()
    {
        await using var context = new InspectorContext();
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        context.Add(new InspectorEntity { Value = 3 });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        await context.Set<InspectorEntity>().ExecuteUpdateAsync(s => s.SetProperty(e => e.Doubled, 10));

        var entity = await context.Set<InspectorEntity>().SingleAsync();
        var model = await AutoComputeExplorerHelper.EntityToModel(context, entity, null, static _ => false);

        model.PropertyValues[nameof(InspectorEntity.Doubled)].GetInt32().Should().Be(10);
        model.ComputedValues[nameof(InspectorEntity.Doubled)].GetInt32().Should().Be(6);
        model.MembersConsistency[nameof(InspectorEntity.Doubled)].GetBoolean().Should().BeFalse();
        model.PropertyValues[nameof(InspectorEntity.Tripled)].GetInt32().Should().Be(9);
        model.ComputedValues[nameof(InspectorEntity.Tripled)].GetInt32().Should().Be(13);
        model.MembersConsistency[nameof(InspectorEntity.Tripled)].GetBoolean().Should().BeFalse();
    }
}

public class InspectorContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite("Data Source=:memory:").UseAutoCompute();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<InspectorEntity>();
}

public partial class InspectorEntity
{
    public int Id { get; set; }
    public int Value { get; set; }
    [AutoComputed] public int Doubled => Value * 2;
    [AutoComputed] public int Tripled => Doubled + Value;
}
