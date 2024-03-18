using FluentAssertions;

namespace LLL.Computed.EFCore.Tests.AffectedEntities;

public class DictionaryTests
{
    [Fact]
    public async void TestDictionaryKey()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Modified";

        var affectedEntities = await context.GetAffectedEntitiesAsync(
            (Person person) => person.Pets.ToDictionary(p => p).Where(kv => kv.Key.Type != null).Count());

        affectedEntities.Should().BeEquivalentTo([pet.Owner]);
    }

    [Fact]
    public async void TestDictionaryValue()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Modified";

        var affectedEntities = await context.GetAffectedEntitiesAsync(
            (Person person) => person.Pets.ToDictionary(p => p.Id).Where(kv => kv.Value.Type != null).Count());

        affectedEntities.Should().BeEquivalentTo([pet.Owner]);
    }
}
