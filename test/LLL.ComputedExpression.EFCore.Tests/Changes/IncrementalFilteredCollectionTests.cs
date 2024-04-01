using FluentAssertions;
using LLL.ComputedExpression.Incremental;

namespace LLL.ComputedExpression.EFCore.Tests.Changes;

public class IncrementalFilteredCollectionTests
{
    private static readonly IIncrementalComputed<Person, int> _incrementalComputed = new NumberIncrementalComputed<Person, int>(0)
        .AddCollection(person => person.Pets.Where(p => p.Type == "Cat")
            .Concat(person.Pets.Where(p => p.Type == "Dog")), cat => 1);

    [Fact]
    public async void TestCollectionElementAdded()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = new Pet { Type = "Cat" };
        person.Pets.Add(pet);

        var changes = await context.GetIncrementalChanges(_incrementalComputed);
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person, 1}
        });
    }

    [Fact]
    public async void TestCollectionElementAddedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = new Pet { Type = "Cat", Owner = person };
        context.Add(pet);

        var changes = await context.GetIncrementalChanges(_incrementalComputed);
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person, 1}
        });
    }

    [Fact]
    public async void TestCollectionElementModifiedToBeFilteredOut()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Modified";

        var changes = await context.GetIncrementalChanges(_incrementalComputed);
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { pet.Owner!, -1}
        });
    }

    [Fact]
    public async void TestCollectionElementModifiedToContinue()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Dog";

        var changes = await context.GetIncrementalChanges(_incrementalComputed);
        changes.Should().BeEmpty();
    }

    [Fact]
    public async void TestCollectionElementRemoved()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        person.Pets.Remove(pet);

        var changes = await context.GetIncrementalChanges(_incrementalComputed);
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person, -1}
        });
    }

    [Fact]
    public async void TestCollectionElementRemovedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        pet.Owner = null;

        var changes = await context.GetIncrementalChanges(_incrementalComputed);
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person, -1}
        });
    }
}
