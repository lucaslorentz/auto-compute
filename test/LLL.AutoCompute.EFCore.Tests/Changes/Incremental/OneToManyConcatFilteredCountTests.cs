using System.Linq.Expressions;
using FluentAssertions;

namespace LLL.AutoCompute.EFCore.Tests.Changes.Incremental;

public class OneToManyConcatFilteredCountTests
{
    private static readonly Expression<Func<Person, int>> _computedExpression =
        (Person person) => person.Pets.Where(p => p.Type == PetType.Cat)
            .Concat(person.Pets.Where(p => p.Type == PetType.Dog))
            .Count();

    [Fact]
    public async Task TestCollectionElementAdded()
    {
        using var context = await TestDbContext.Create<PersonDbContext>(
            useLazyLoadingProxies: false
        );

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = new Pet { Id = "New", Type = PetType.Cat };
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
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = new Pet { Id = "New", Type = PetType.Cat, Owner = person };
        context.Add(pet);

        var changes = await context.GetChangesAsync(
            _computedExpression,
            default,
            static c => c.NumberIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person, 1}
        });
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementModifiedToBeFilteredOut()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Type = PetType.Other;

        var changes = await context.GetChangesAsync(
            _computedExpression,
            default,
            static c => c.NumberIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { pet.Owner!, -1}
        });
        context.Entry(pet.Owner!).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementModifiedToContinue()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Type = PetType.Dog;

        var changes = await context.GetChangesAsync(
            _computedExpression,
            default,
            static c => c.NumberIncremental());
        changes.Should().BeEmpty();
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

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.NumberIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person, -1}
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

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.NumberIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person, -1}
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
        var pet = new Pet { Id = "New", Type = PetType.Cat };
        person.Pets.Add(pet);

        var provider = context.GetChangesProvider(
            _computedExpression,
            default,
            c => c.NumberIncremental())!;

        var changes = await provider.GetChangesAsync();
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person, 1}
        });

        changes = await provider.GetChangesAsync();
        changes.Should().BeEmpty();

        person.Pets.Clear();
        changes = await provider.GetChangesAsync();
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person, -1}
        });
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }
}
