using System.Linq.Expressions;
using FluentAssertions;

namespace LLL.AutoCompute.EFCore.Tests.Changes.Incremental;

public class OneToManyFilteredCountTests
{
    private static readonly Expression<Func<Person, int>> _computedExpression = person =>
        person.Pets.Where(p => p.Color == PetColor.Orange).Count();

    [Fact]
    public async Task TestCollectionElementAdded()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>(
            useLazyLoadingProxies: false
        );

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = new Cat { Id = "New", Color = PetColor.Orange };
        person.Pets.Add(pet);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.NumberIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person, 1}
        });
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementAddedInverse()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = new Cat { Id = "New", Color = PetColor.Orange, Owner = person };
        context.Add(pet);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.NumberIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person, 1}
        });
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementMoved()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var personA = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var personB = context!.Set<Person>().Find(PersonDbContext.PersonBId)!;
        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Owner = personB;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.NumberIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { personA, -1},
            { personB, 1}
        });
        context.Entry(personA).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
        context.Entry(personB).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementModified()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Color = PetColor.Other;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.NumberIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { pet.Owner!, -1}
        });
        context.Entry(pet.Owner!).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementRemoved()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>(
            useLazyLoadingProxies: false
        );

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        person.Pets.Remove(pet);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.NumberIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person, -1}
        });
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementRemovedInverse()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Owner = null;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.NumberIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person, -1}
        });
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task DeltaTest()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>(
            useLazyLoadingProxies: false
        );

        // Add a cat
        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var newPet = new Cat { Id = "New", Color = PetColor.Orange };
        person.Pets.Add(newPet);

        var deltaProvider = context.GetChangesProvider(_computedExpression, default, static c => c.NumberIncremental())!;

        var changes = await deltaProvider.GetChangesAsync();
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person, 1}
        });

        changes = await deltaProvider.GetChangesAsync();
        changes.Should().BeEmpty();
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();

        await context.Entry(person).Navigation(nameof(person.Pets)).LoadAsync();
        var pet = person.Pets.First(p => p != newPet);
        person.Pets.Clear();

        changes = await deltaProvider.GetChangesAsync();
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person, -2}
        });
    }
}
