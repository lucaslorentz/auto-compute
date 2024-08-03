using System.Linq.Expressions;
using FluentAssertions;

namespace LLL.AutoCompute.EFCore.Tests.IncrementalChanges;

public class OneToManyFilteredCountTests
{
    private static readonly Expression<Func<Person, int>> _computedExpression = (Person person) =>
        person.Pets.Where(p => p.Type == "Cat").Count();

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

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.NumberIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person, 1}
        });
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async void TestCollectionElementMoved()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person1 = context!.Set<Person>().Find(1)!;
        var person2 = context!.Set<Person>().Find(2)!;
        var pet = context!.Set<Pet>().Find(1)!;
        pet.Owner = person2;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.NumberIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person1, -1},
            { person2, 1}
        });
        context.Entry(person1).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
        context.Entry(person2).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async void TestCollectionElementModified()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Modified";

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.NumberIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { pet.Owner!, -1}
        });
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
        var newPet = new Pet { Type = "Cat" };
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
