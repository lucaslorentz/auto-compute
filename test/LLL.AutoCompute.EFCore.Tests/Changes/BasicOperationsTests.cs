using System.Linq.Expressions;
using FluentAssertions;
using LLL.AutoCompute.ChangeCalculations;

namespace LLL.AutoCompute.EFCore.Tests.Changes;

public class BasicOperationsTests
{
    private static readonly Expression<Func<Person, string?>> _computedExpression = (Person p) => p.FirstName + " " + p.LastName;

    [Fact]
    public async Task TestCreate()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var person = new Person { Id = "New", FirstName = "Jane", LastName = "Doe" };
        context!.Add(person);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<string?>>{
            { person, new ValueChange<string?>(default, "Jane Doe")}
        });
    }

    [Fact]
    public async Task TestModified()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        person.FirstName = "Modified";

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<string?>>{
            { person, new ValueChange<string?>("John Doe", "Modified Doe")}
        });
    }

    [Fact]
    public async Task TestRemove()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        context.Remove(person);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEmpty();
    }
}
