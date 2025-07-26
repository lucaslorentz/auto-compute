using System.Linq.Expressions;
using FluentAssertions;
using LLL.AutoCompute.ChangeCalculations;

namespace LLL.AutoCompute.EFCore.Tests.Changes;

public class CollectionTests
{
    private static readonly Expression<Func<Person, int>> _computedExpression = (Person person) => person.Pets.Count;

    [Fact]
    public async Task TestCollectionElementAdded()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = new Cat { Id = "New", Color = PetColor.Orange };
        person.Pets.Add(pet);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { person, new ValueChange<int>(1, 2)}
        });
    }

    [Fact]
    public async Task TestCollectionElementAddedInverse()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = new Cat { Id = "New", Color = PetColor.Orange, Owner = person };
        context.Add(pet);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { person, new ValueChange<int>(1, 2)}
        });
    }

    [Fact]
    public async Task TestCollectionElementModified()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Color = PetColor.Other;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEmpty();
    }

    [Fact]
    public async Task TestCollectionElementRemoved()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        person.Pets.Remove(pet);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { person, new ValueChange<int>(1, 0)}
        });
    }

    [Fact]
    public async Task TestCollectionElementRemovedInverse()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Owner = null;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { person, new ValueChange<int>(1, 0)}
        });
    }
}
