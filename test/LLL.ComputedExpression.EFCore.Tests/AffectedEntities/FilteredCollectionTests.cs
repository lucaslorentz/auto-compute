using System.Linq.Expressions;
using FluentAssertions;

namespace LLL.Computed.EFCore.Tests.AffectedEntities;

public class FilteredCollectionTests
{
    private static readonly Expression<Func<Person, int>> _computedExpression = (Person person) => person.Pets.Where(p => p.Type == "Cat" && p.Color == "Black").Count();

    [Fact]
    public async void TestDebugString()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var affectedEntitiesProvider = context.GetAffectedEntitiesProvider(_computedExpression);
        affectedEntitiesProvider!.ToDebugString()
            .Should().Be("Concat(EntitiesWithNavigationChange(Person, Pets), Load(Concat(EntitiesWithPropertyChange(Pet, Type), EntitiesWithPropertyChange(Pet, Color)), Owner))");
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
        affectedEntities.Should().BeEquivalentTo([pet.Owner]);
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
