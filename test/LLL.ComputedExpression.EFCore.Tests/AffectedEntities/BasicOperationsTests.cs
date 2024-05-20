using System.Linq.Expressions;
using FluentAssertions;

namespace LLL.ComputedExpression.EFCore.Tests.AffectedEntities;

public class BasicOperationsTests
{
    private static readonly Expression<Func<Person, string?>> _computedExpression = (Person p) => p.FirstName + " " + p.LastName;

    [Fact]
    public async void TestDebugString()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var changesProvider = context.GetChangesProvider(_computedExpression, default, static c => c.Void());
        changesProvider!.ToDebugString()
            .Should().Be("Concat(EntitiesWithPropertyChange(Person.FirstName), EntitiesWithPropertyChange(Person.LastName))");
    }

    [Fact]
    public async void TestCreate()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = new Person { FirstName = "Jane" };
        context!.Add(person);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.Void());
        changes.Keys.Should().BeEquivalentTo([person]);
    }

    [Fact]
    public async void TestModified()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        person.FirstName = "Modified";

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.Void());
        changes.Keys.Should().BeEquivalentTo([person]);
    }

    [Fact]
    public async void TestRemove()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        context.Remove(person);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.Void());
        changes.Keys.Should().BeEmpty();
    }
}
