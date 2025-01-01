using System.Linq.Expressions;
using FluentAssertions;

namespace LLL.AutoCompute.EFCore.Tests.IncrementalChanges;

public class NestedFilteredCount
{
    private static readonly Expression<Func<Person, int>> _computedExpression = (Person person) =>
        person.Friends.SelectMany(f => f.Pets).Where(p => p.Type == "Cat").Count();

    [Fact]
    public async Task TestNestedCollectionElementModified()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Modified";

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.NumberIncremental());

        var person2 = context!.Set<Person>().Find(2)!;
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person2, -1}
        });
        context.Entry(person2).Navigation(nameof(Person.Friends)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementRemoved()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person2 = context!.Set<Person>().Find(2)!;
        await context.Entry(person2).Navigation(nameof(Person.Friends)).LoadAsync();
        person2.Friends.Clear();

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.NumberIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person2, -1}
        });
    }

    [Fact]
    public async Task TestCollectionElementRemovedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person1 = context!.Set<Person>().Find(1)!;
        await context.Entry(person1).Navigation(nameof(Person.FriendsInverse)).LoadAsync();
        person1.FriendsInverse.Clear();

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.NumberIncremental());

        var person2 = context!.Set<Person>().Find(2)!;
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person2, -1}
        });
        context.Entry(person2).Navigation(nameof(Person.Friends)).IsLoaded.Should().BeFalse();
    }
}
