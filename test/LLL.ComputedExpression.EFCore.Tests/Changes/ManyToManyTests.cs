using System.Linq.Expressions;
using FluentAssertions;

namespace LLL.ComputedExpression.EFCore.Tests.Changes;

public class ManyToManyTests
{
    private static readonly Expression<Func<Person, int>> _computedExpression = (Person person) => person.Friends.Count;

    [Fact]
    public async void TestCollectionElementAdded()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person2 = context!.Set<Person>().Find(2)!;
        await context.Entry(person2).Navigation(nameof(Person.Friends)).LoadAsync();

        person2.Friends.Add(new Person());

        var changes = await context.GetChangesAsync(_computedExpression);
        changes.Should().BeEquivalentTo(new Dictionary<Person, ConstValueChange<int>>{
            { person2, new ConstValueChange<int>(1, 2)}
        });
    }

    [Fact]
    public async void TestCollectionElementAddedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person2 = context!.Set<Person>().Find(2)!;
        var newPerson = new Person { FriendsInverse = { person2 } };
        context.Add(newPerson);

        var changes = await context.GetChangesAsync(_computedExpression);
        changes.Should().BeEquivalentTo(new Dictionary<Person, ConstValueChange<int>>{
            { person2, new ConstValueChange<int>(1, 2)}
        });
    }

    [Fact]
    public async void TestCollectionElementModified()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person1 = context!.Set<Person>().Find(1)!;
        person1.FirstName = "Modified";

        var changes = await context.GetChangesAsync(_computedExpression);
        changes.Should().BeEmpty();
    }

    [Fact]
    public async void TestCollectionElementRemoved()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person1 = context!.Set<Person>().Find(1)!;
        var person2 = context!.Set<Person>().Find(2)!;
        await context.Entry(person2).Navigation(nameof(Person.Friends)).LoadAsync();
        person2.Friends.RemoveAt(0);

        var changes = await context.GetChangesAsync(_computedExpression);
        changes.Should().BeEquivalentTo(new Dictionary<Person, ConstValueChange<int>>{
            { person2, new ConstValueChange<int>(1, 0)}
        });
    }

    [Fact]
    public async void TestCollectionElementRemovedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person1 = context!.Set<Person>().Find(1)!;
        await context.Entry(person1).Navigation(nameof(Person.FriendsInverse)).LoadAsync();
        var person2 = context!.Set<Person>().Find(2)!;

        person1.FriendsInverse.RemoveAt(0);

        var changes = await context.GetChangesAsync(_computedExpression);
        changes.Should().BeEquivalentTo(new Dictionary<Person, ConstValueChange<int>>{
            { person2, new ConstValueChange<int>(1, 0)}
        });
    }
}
