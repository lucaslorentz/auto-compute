using System.Linq.Expressions;
using FluentAssertions;

namespace LLL.Computed.EFCore.Tests.AffectedEntities;

public class BasicOperationsTests
{
    private static readonly Expression<Func<Person, string?>> _computedExpression = (Person p) => p.FirstName + " " + p.LastName;

    [Fact]
    public async void TestDebugString()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var affectedEntitiesProvider = context.GetAffectedEntitiesProvider(_computedExpression);
        affectedEntitiesProvider.ToDebugString()
            .Should().Be("Concat(EntitiesWithPropertyChange(Person, FirstName), EntitiesWithPropertyChange(Person, LastName))");
    }

    [Fact]
    public async void TestCreate()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = new Person { FirstName = "Jane" };
        context!.Add(person);

        var affectedEntities = await context.GetAffectedEntitiesAsync(_computedExpression);
        affectedEntities.Should().BeEquivalentTo([person]);
    }

    [Fact]
    public async void TestModified()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        person.FirstName = "Modified";

        var affectedEntities = await context.GetAffectedEntitiesAsync(_computedExpression);
        affectedEntities.Should().BeEquivalentTo([person]);
    }

    [Fact]
    public async void TestRemove()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        context.Remove(person);

        var affectedEntities = await context.GetAffectedEntitiesAsync(_computedExpression);
        affectedEntities.Should().BeEquivalentTo([person]);
    }
}
