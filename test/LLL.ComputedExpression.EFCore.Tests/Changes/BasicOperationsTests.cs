using System.Linq.Expressions;
using FluentAssertions;

namespace LLL.ComputedExpression.EFCore.Tests.Changes;

public class BasicOperationsTests
{
    private static readonly Expression<Func<Person, string?>> _computedExpression = (Person p) => p.FirstName + " " + p.LastName;

    [Fact]
    public async void TestCreate()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = new Person { FirstName = "Jane", LastName = "Doe" };
        context!.Add(person);

        var changes = await context.GetChangesAsync(_computedExpression);
        changes.Should().BeEquivalentTo(new Dictionary<Person, ConstValueChange<string?>>{
            { person, new ConstValueChange<string?>(default, "Jane Doe")}
        });
    }

    [Fact]
    public async void TestModified()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        person.FirstName = "Modified";

        var changes = await context.GetChangesAsync(_computedExpression);
        changes.Should().BeEquivalentTo(new Dictionary<Person, ConstValueChange<string?>>{
            { person, new ConstValueChange<string?>("John Doe", "Modified Doe")}
        });
    }

    [Fact]
    public async void TestRemove()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        context.Remove(person);

        var changes = await context.GetChangesAsync(_computedExpression);
        changes.Should().BeEquivalentTo(new Dictionary<Person, ConstValueChange<string?>>{
            { person, new ConstValueChange<string?>("John Doe", default)}
});
    }
}
