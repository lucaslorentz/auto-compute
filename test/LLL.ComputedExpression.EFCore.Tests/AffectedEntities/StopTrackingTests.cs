using FluentAssertions;
using LLL.ComputedExpression.EFCore.Internal;

namespace LLL.ComputedExpression.EFCore.Tests.AffectedEntities;

public class StopTrackingTests
{
    [Fact]
    public async void Property()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var affectedEntitiesProvider = context.GetAffectedEntitiesProvider(
            (Person p) => p.AsComputedUntracked().FirstName + " " + p.LastName);

        affectedEntitiesProvider!.ToDebugString()
            .Should().Be("EntitiesWithPropertyChange(Person, LastName)");
    }

    [Fact]
    public async void Collection()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var affectedEntitiesProvider = context.GetAffectedEntitiesProvider(
            (Person p) => p.AsComputedUntracked().Pets.Count(p => p.Type == "Cat"));

        affectedEntitiesProvider.Should().BeNull();
    }

    [Fact]
    public async void CollectionItemProperty()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var affectedEntitiesProvider = context.GetAffectedEntitiesProvider(
            (Person p) => p.Pets.Count(p => p.AsComputedUntracked().Type == "Cat" && p.Color == "Black"));

        affectedEntitiesProvider!.ToDebugString()
            .Should().Be("Concat(EntitiesWithNavigationChange(Person, Pets), Load(EntitiesWithPropertyChange(Pet, Color), Owner))");
    }
}
