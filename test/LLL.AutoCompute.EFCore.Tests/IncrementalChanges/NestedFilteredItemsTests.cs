using System.Linq.Expressions;
using FluentAssertions;
using LLL.AutoCompute.ChangeCalculations;

namespace LLL.AutoCompute.EFCore.Tests.IncrementalChanges;

public class NestedFilteredItemsTests
{
    private static readonly Expression<Func<Person, IEnumerable<Pet>>> _computedExpression = (Person person) =>
        person.Friends.SelectMany(f => f.Pets).Where(p => p.Type == "Cat");

    [Fact]
    public async void TestNestedCollectionElementModified()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Modified";

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.SetIncremental());

        var person2 = context!.Set<Person>().Find(2)!;
        changes.Should().BeEquivalentTo(new Dictionary<Person, SetChange<Pet>>{
            { person2, new SetChange<Pet> { Removed = [pet], Added = [] }}
        });
        context.Entry(person2).Navigation(nameof(Person.Friends)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async void TestCollectionElementRemoved()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person2 = context!.Set<Person>().Find(2)!;
        await context.Entry(person2).Navigation(nameof(Person.Friends)).LoadAsync();
        person2.Friends.Clear();

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.SetIncremental());

        var pet = context!.Set<Pet>().Find(1)!;
        changes.Should().BeEquivalentTo(new Dictionary<Person, SetChange<Pet>>{
            { person2, new SetChange<Pet> { Removed = [pet], Added = []}}
        });
    }

    [Fact]
    public async void TestCollectionElementRemovedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person1 = context!.Set<Person>().Find(1)!;
        await context.Entry(person1).Navigation(nameof(Person.FriendsInverse)).LoadAsync();
        person1.FriendsInverse.Clear();

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.SetIncremental());

        var person2 = context!.Set<Person>().Find(2)!;
        var pet = context!.Set<Pet>().Find(1)!;
        changes.Should().BeEquivalentTo(new Dictionary<Person, SetChange<Pet>>{
            { person2, new SetChange<Pet> { Removed = [pet], Added = []}}
        });
        context.Entry(person2).Navigation(nameof(Person.Friends)).IsLoaded.Should().BeFalse();
    }
}
