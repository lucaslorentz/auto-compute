using System.Linq.Expressions;
using FluentAssertions;

namespace LLL.ComputedExpression.EFCore.Tests.AffectedEntities;

public class CollectionTests
{
    private static readonly Expression<Func<Person, int>> _computedExpression = (Person person) => person.Pets.Count;

    [Fact]
    public async void TestDebugString()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var changesProvider = context.GetChangesProvider(_computedExpression, default, static c => c.Void());
        changesProvider!.ToDebugString()
            .Should().Be("EntitiesWithNavigationChange(Person.Pets)");
    }

    [Fact]
    public async void TestCollectionElementAdded()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = new Pet { Type = "Cat" };
        person.Pets.Add(pet);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.Void());
        changes.Keys.Should().BeEquivalentTo([person]);
    }

    [Fact]
    public async void TestCollectionElementAddedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = new Pet { Type = "Cat", Owner = person };
        context.Add(pet);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.Void());
        changes.Keys.Should().BeEquivalentTo([person]);
    }

    [Fact]
    public async void TestCollectionElementMoved()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person1 = context!.Set<Person>().Find(1)!;
        var person2 = context!.Set<Person>().Find(2)!;
        var pet = context!.Set<Pet>().Find(1)!;
        pet.Owner = person2;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.Void());
        changes.Keys.Should().BeEquivalentTo([person1, person2]);
    }

    [Fact]
    public async void TestCollectionElementModified()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Modified";

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.Void());
        changes.Keys.Should().BeEmpty();
    }

    [Fact]
    public async void TestCollectionElementRemoved()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        person.Pets.Remove(pet);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.Void());
        changes.Keys.Should().BeEquivalentTo([person]);
    }

    [Fact]
    public async void TestCollectionElementRemovedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        pet.Owner = null;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.Void());
        changes.Keys.Should().BeEquivalentTo([person]);
    }
}
