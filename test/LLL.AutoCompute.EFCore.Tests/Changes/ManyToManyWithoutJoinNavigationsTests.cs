using System.Linq.Expressions;
using FluentAssertions;
using LLL.AutoCompute.ChangeCalculators;

namespace LLL.AutoCompute.EFCore.Tests.Changes;

public class ManyToManyWithoutJoinNavigationsTests
{
    private static readonly Expression<Func<Person, int>> _computedExpression = (Person person) => person.Colleagues.Count;

    [Fact]
    public async Task TestCollectionElementAdded()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var personB = context!.Set<Person>().Find(PersonDbContext.PersonBId)!;
        await context.Entry(personB).Navigation(nameof(Person.Colleagues)).LoadAsync();

        personB.Colleagues.Add(new Person { Id = "New" });

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { personB, new ValueChange<int>(1, 2)}
        });
    }

    [Fact]
    public async Task TestCollectionElementAddedInverse()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var personB = context!.Set<Person>().Find(PersonDbContext.PersonBId)!;
        var newPerson = new Person { Id = "New", ColleaguesInverse = { personB } };
        context.Add(newPerson);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { personB, new ValueChange<int>(1, 2)}
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

        var personB = context!.Set<Person>().Find(PersonDbContext.PersonBId)!;
        await context.Entry(personB).Navigation(nameof(Person.Colleagues)).LoadAsync();

        personB.Colleagues.RemoveAt(0);

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
        await context.Entry(personA).Navigation(nameof(Person.ColleaguesInverse)).LoadAsync();
        var personB = context!.Set<Person>().Find(PersonDbContext.PersonBId)!;

        personA.ColleaguesInverse.RemoveAt(0);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { personB, new ValueChange<int>(1, 0)}
        });
    }

    [Fact]
    public async Task TestJoinEntryAddedDirectly()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var personA = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var personB = context!.Set<Person>().Find(PersonDbContext.PersonBId)!;

        context.Set<Dictionary<string, object>>("ColleaguesJoin").Add(new Dictionary<string, object>
        {
            ["FromPersonId"] = personA.Id,
            ["ToPersonId"] = personB.Id,
        });

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { personA, new ValueChange<int>(0, 1)}
        });
    }

    [Fact]
    public async Task TestJoinEntryAddedDirectlyWithUntrackedPrincipals()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        context.Set<Dictionary<string, object>>("ColleaguesJoin").Add(new Dictionary<string, object>
        {
            ["FromPersonId"] = PersonDbContext.PersonAId,
            ["ToPersonId"] = PersonDbContext.PersonBId,
        });

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { context.Set<Person>().Find(PersonDbContext.PersonAId)!, new ValueChange<int>(0, 1)}
        });
    }
}
