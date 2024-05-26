using System.Linq.Expressions;
using FluentAssertions;
using LLL.ComputedExpression.ChangeCalculations;

namespace LLL.ComputedExpression.EFCore.Tests.Changes;

public class ChangeObserverTests
{
    private static readonly Expression<Func<Person, string?>> _computedExpression = (Person p) => p.FirstName + " " + p.LastName;

    [Fact]
    public async void TestCreate()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var observedChanges = new List<IReadOnlyDictionary<Person, ValueChange<string?>>>();

        context.ObserveComputedChanges(
            _computedExpression,
            default,
            static c => c.ValueChange(),
            async changes =>
            {
                observedChanges.Add(changes);
            });

        var person = new Person { FirstName = "Jane", LastName = "Doe" };
        context!.Add(person);

        await context.SaveAllChangesAsync();

        person.FirstName = "John";

        await context.SaveAllChangesAsync();

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        observedChanges.Should().BeEquivalentTo(new List<Dictionary<Person, ValueChange<string?>>> {
            new() {
                { person, new ValueChange<string?>(default, "Jane Doe")}
            },
            new() {
                { person, new ValueChange<string?>("Jane Doe", "John Doe")}
            }
        });
    }
}
