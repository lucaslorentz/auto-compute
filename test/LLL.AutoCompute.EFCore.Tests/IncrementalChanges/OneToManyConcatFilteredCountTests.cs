using System.Linq.Expressions;
using FluentAssertions;

namespace LLL.AutoCompute.EFCore.Tests.IncrementalChanges;

public class OneToManyConcatFilteredCountTests
{
    private static readonly Expression<Func<Person, int>> _computedExpression =
        (Person person) => person.Pets.Where(p => p.Type == "Cat")
            .Concat(person.Pets.Where(p => p.Type == "Dog"))
            .Count();

    [Fact]
    public async void TestCollectionElementAdded()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = new Pet { Type = "Cat" };
        person.Pets.Add(pet);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.NumberIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person, 1}
        });
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async void TestCollectionElementAddedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = new Pet { Type = "Cat", Owner = person };
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
    public async void TestCollectionElementModifiedToBeFilteredOut()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Modified";

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
    public async void TestCollectionElementModifiedToContinue()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Dog";

        var changes = await context.GetChangesAsync(
            _computedExpression,
            default,
            static c => c.NumberIncremental());
        changes.Should().BeEmpty();
        context.Entry(pet.Owner!).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async void TestCollectionElementRemoved()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        person.Pets.Remove(pet);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.NumberIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person, -1}
        });
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async void TestCollectionElementRemovedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        pet.Owner = null;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.NumberIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person, -1}
        });
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async void DeltaTest()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        // Add a cat
        var person = context!.Set<Person>().Find(1)!;
        var pet = new Pet { Type = "Cat" };
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
