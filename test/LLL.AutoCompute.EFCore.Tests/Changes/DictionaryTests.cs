﻿using FluentAssertions;

namespace LLL.AutoCompute.EFCore.Tests.Changes;

public class DictionaryTests
{
    [Fact]
    public async Task TestDictionaryKey()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Modified";

        var changes = await context.GetChangesAsync(
            (Person person) => person.Pets.ToDictionary(p => p).Where(kv => kv.Key.Type != null).Count(),
            default,
            static c => c.Void());

        changes.Keys.Should().BeEquivalentTo([pet.Owner]);
    }

    [Fact]
    public async Task TestDictionaryValue()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Modified";

        var changes = await context.GetChangesAsync(
            (Person person) => person.Pets.ToDictionary(p => p.Id).Where(kv => kv.Value.Type != null).Count(),
            default,
            static c => c.Void());

        changes.Keys.Should().BeEquivalentTo([pet.Owner]);
    }
}
