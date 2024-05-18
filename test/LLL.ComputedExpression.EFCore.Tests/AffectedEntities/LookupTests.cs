using FluentAssertions;

namespace LLL.ComputedExpression.EFCore.Tests.AffectedEntities;

public class LookupTests
{
    [Fact]
    public async void TestLookupKeys()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Modified";

        var changes = await context.GetChangesAsync(
            (Person person) => person.Pets.ToLookup(p => p).Where(kv => kv.Key.Type != null).Count(),
            c => c.Void());

        changes.Keys.Should().BeEquivalentTo([pet.Owner]);
    }

    [Fact]
    public async void TestLookupValue()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Modified";

        var changes = await context.GetChangesAsync(
            (Person person) => person.Pets.ToLookup(p => p.Id).Where(kv => kv.Any(p => p.Type != null)).Count(),
            c => c.Void());

        changes.Keys.Should().BeEquivalentTo([pet.Owner]);
    }
}
