using System.Linq.Expressions;
using FluentAssertions;
using LLL.AutoCompute.ChangeCalculators;

namespace LLL.AutoCompute.EFCore.Tests.Changes;

public class ManyToManyTests
{
    private static readonly Expression<Func<Person, int>> _computedExpression = (Person person) => person.Friends.Count;

    [Fact]
    public async Task TestCollectionElementAdded()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var person2 = context!.Set<Person>().Find(PersonDbContext.PersonBId)!;
        await context.Entry(person2).Navigation(nameof(Person.Friends)).LoadAsync();

        person2.Friends.Add(new Person { Id = "New" });

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { person2, new ValueChange<int>(1, 2)}
        });
    }

    [Fact]
    public async Task TestCollectionElementAddedInverse()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var person2 = context!.Set<Person>().Find(PersonDbContext.PersonBId)!;
        var newPerson = new Person { Id = "New", FriendsInverse = { person2 } };
        context.Add(newPerson);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { person2, new ValueChange<int>(1, 2)}
        });
    }

    [Fact]
    public async Task TestCollectionElementModified()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var personA = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        personA.FirstName = "Modified";

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEmpty();
    }

    [Fact]
    public async Task TestCollectionElementRemoved()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var personA = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var personB = context!.Set<Person>().Find(PersonDbContext.PersonBId)!;
        await context.Entry(personB).Navigation(nameof(Person.Friends)).LoadAsync();
        personB.Friends.RemoveAt(0);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { personB, new ValueChange<int>(1, 0)}
        });
    }

    [Fact]
    public async Task TestCollectionElementRemovedInverse()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var personA = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        await context.Entry(personA).Navigation(nameof(Person.FriendsInverse)).LoadAsync();
        var personB = context!.Set<Person>().Find(PersonDbContext.PersonBId)!;

        personA.FriendsInverse.RemoveAt(0);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { personB, new ValueChange<int>(1, 0)}
        });
    }
}
