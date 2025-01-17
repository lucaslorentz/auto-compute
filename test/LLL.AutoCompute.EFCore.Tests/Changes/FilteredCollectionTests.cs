using System.Linq.Expressions;
using FluentAssertions;
using LLL.AutoCompute.ChangeCalculations;

namespace LLL.AutoCompute.EFCore.Tests.Changes;

public class FilteredCollectionTests
{
    private static readonly Expression<Func<Person, int>> _computedExpression = (Person person) => person.Pets.Where(p => p.Type == PetType.Cat).Count();

    [Fact]
    public async Task TestCollectionElementAdded()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = new Pet { Id = "New", Type = PetType.Cat };
        person.Pets.Add(pet);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { person, new ValueChange<int>(1, 2)}
        });
    }

    [Fact]
    public async Task TestCollectionElementAddedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = new Pet { Id = "New", Type = PetType.Cat, Owner = person };
        context.Add(pet);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { person, new ValueChange<int>(1, 2)}
        });
    }

    [Fact]
    public async Task TestCollectionElementModified()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Type = PetType.Other;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { pet.Owner!, new ValueChange<int>(1, 0)}
        });
    }

    [Fact]
    public async Task TestCollectionElementRemoved()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

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
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Owner = null;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { person, new ValueChange<int>(1, 0)}
        });
    }

    [Fact]
    public async Task DeltaChangesTest()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;

        var changesProvider = context.GetChangesProvider(_computedExpression, default, static c => c.ValueChange())!;

        // Add a cat
        person.Pets.Add(new Pet { Id = "New", Type = PetType.Cat });
        var changes = await changesProvider.GetChangesAsync();
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { person, new ValueChange<int>(1, 2)}
        });

        // No other change
        changes = await changesProvider.GetChangesAsync();
        changes.Should().BeEmpty();

        // Remove a cat
        person.Pets.RemoveAt(0);
        changes = await changesProvider.GetChangesAsync();
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { person, new ValueChange<int>(2, 1)}
        });
    }
}
