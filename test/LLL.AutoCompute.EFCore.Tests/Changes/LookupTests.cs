using FluentAssertions;

namespace LLL.AutoCompute.EFCore.Tests.Changes;

public class LookupTests
{
    [Fact]
    public async Task TestLookupKeys()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Color = PetColor.Other;

        var changes = await context.GetChangesAsync(
            (Person person) => person.Pets.ToLookup(p => p).Where(kv => kv.Key.Color != null).Count(),
            default,
            c => c.CurrentValue());

        changes.Keys.Should().BeEquivalentTo([pet.Owner]);
    }

    [Fact]
    public async Task TestLookupValue()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Color = PetColor.Other;

        var changes = await context.GetChangesAsync(
            (Person person) => person.Pets.ToLookup(p => p.Id).Where(kv => kv.Any(p => p.Color != null)).Count(),
            default,
            c => c.CurrentValue());

        changes.Keys.Should().BeEquivalentTo([pet.Owner]);
    }
}
