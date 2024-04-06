using System.Linq.Expressions;
using FluentAssertions;
using LLL.ComputedExpression.EFCore.Internal;

namespace LLL.ComputedExpression.EFCore.Tests.AffectedEntities;

public class CollectionTests
{
    private static readonly Expression<Func<Person, int>> _computedExpression = (Person person) => person.Pets.Count;

    [Fact]
    public async void TestDebugString()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var affectedEntitiesProvider = context.GetAffectedEntitiesProvider(_computedExpression);
        affectedEntitiesProvider!.ToDebugString()
            .Should().Be("EntitiesWithNavigationChange(Person, Pets)");
    }

    [Fact]
    public async void TestCollectionElementAdded()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = new Pet { Type = "Cat" };
        person.Pets.Add(pet);

        var affectedEntities = await context.GetAffectedEntitiesAsync(_computedExpression);
        affectedEntities.Should().BeEquivalentTo([person]);
    }

    [Fact]
    public async void TestCollectionElementAddedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = new Pet { Type = "Cat", Owner = person };
        context.Add(pet);

        var affectedEntities = await context.GetAffectedEntitiesAsync(_computedExpression);
        affectedEntities.Should().BeEquivalentTo([person]);
    }

    [Fact]
    public async void TestCollectionElementModified()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Modified";

        var affectedEntities = await context.GetAffectedEntitiesAsync(_computedExpression);
        affectedEntities.Should().BeEmpty();
    }

    [Fact]
    public async void TestCollectionElementRemoved()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        person.Pets.Remove(pet);

        var affectedEntities = await context.GetAffectedEntitiesAsync(_computedExpression);
        affectedEntities.Should().BeEquivalentTo([person]);
    }

    [Fact]
    public async void TestCollectionElementRemovedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        pet.Owner = null;

        var affectedEntities = await context.GetAffectedEntitiesAsync(_computedExpression);
        affectedEntities.Should().BeEquivalentTo([person]);
    }
}
