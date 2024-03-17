using FluentAssertions;

namespace L3.Computed.EFCore.Tests.AffectedEntities;

public class BasicOperationsTests
{
    [Fact]
    public async void TestCreate()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = new Person { FirstName = "Jane" };
        context!.Add(person);

        var affectedEntities = await context.GetAffectedEntitiesAsync((Person p) => p.FirstName);
        affectedEntities.Should().BeEquivalentTo([person]);
    }

    [Fact]
    public async void TestModified()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        person.FirstName = "Modified";

        var affectedEntities = await context.GetAffectedEntitiesAsync((Person p) => p.FirstName);
        affectedEntities.Should().BeEquivalentTo([person]);
    }

    [Fact]
    public async void TestRemove()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        context.Remove(person);

        var affectedEntities = await context.GetAffectedEntitiesAsync((Person p) => p.FirstName);
        affectedEntities.Should().BeEquivalentTo([person]);
    }
}
