using System.Linq.Expressions;
using FluentAssertions;
using LLL.AutoCompute.ChangeCalculations;

namespace LLL.AutoCompute.EFCore.Tests.Changes.Incremental;

public class OneToManyFilteredItemsTests
{
    private static readonly Expression<Func<Person, IEnumerable<Pet>>> _computedExpression = (Person person) =>
        person.Pets.Where(p => p.Type == PetType.Cat);

    [Fact]
    public async Task TestCollectionElementAdded()
    {
        using var context = await TestDbContext.Create<PersonDbContext>(
            useLazyLoadingProxies: false
        );

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = new Pet { Id = "New", Type = PetType.Cat };
        person.Pets.Add(pet);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.SetIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, SetChange<Pet>>{
            { person, new SetChange<Pet>{ Removed = [], Added = [pet]}}
        });
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementAddedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = new Pet { Id = "New", Type = PetType.Cat, Owner = person };
        context.Add(pet);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.SetIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, SetChange<Pet>>{
            { person, new SetChange<Pet> { Removed = [], Added = [pet]}}
        });
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementMoved()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var personA = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var personB = context!.Set<Person>().Find(PersonDbContext.PersonBId)!;
        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Owner = personB;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.SetIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, SetChange<Pet>>{
            { personA, new SetChange<Pet>{ Removed = [pet], Added = []}},
            { personB, new SetChange<Pet>{ Removed = [], Added = [pet]}}
        });
        context.Entry(personA).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
        context.Entry(personB).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementModified()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Type = PetType.Other;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.SetIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, SetChange<Pet>>{
            { pet.Owner!, new SetChange<Pet> { Removed = [pet], Added = []}}
        });
        context.Entry(pet.Owner!).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementRemoved()
    {
        using var context = await TestDbContext.Create<PersonDbContext>(
            useLazyLoadingProxies: false
        );

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        person.Pets.Remove(pet);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.SetIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, SetChange<Pet>>{
            { person, new SetChange<Pet>{ Removed = [pet], Added = [] }}
        });
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementRemovedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Owner = null;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.SetIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, SetChange<Pet>>{
            { person, new SetChange<Pet>{ Removed = [pet], Added = []}}
        });
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task DeltaTest()
    {
        using var context = await TestDbContext.Create<PersonDbContext>(
            useLazyLoadingProxies: false
        );

        // Add a cat
        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var newPet = new Pet { Id = "New", Type = PetType.Cat };
        person.Pets.Add(newPet);

        var deltaProvider = context.GetChangesProvider(_computedExpression, default, static c => c.SetIncremental())!;

        var changes = await deltaProvider.GetChangesAsync();
        changes.Should().BeEquivalentTo(new Dictionary<Person, SetChange<Pet>>{
            { person, new SetChange<Pet>{
                Removed = [],
                Added = [newPet],
            }}
        });

        changes = await deltaProvider.GetChangesAsync();
        changes.Should().BeEmpty();
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();

        await context.Entry(person).Navigation(nameof(person.Pets)).LoadAsync();
        var pet = person.Pets.First(p => p != newPet);
        person.Pets.Clear();

        changes = await deltaProvider.GetChangesAsync();
        changes.Should().BeEquivalentTo(new Dictionary<Person, SetChange<Pet>>{
            { person, new SetChange<Pet>{
                Removed = [pet, newPet],
                Added = [],
            }}
        });
    }
}
