﻿using FluentAssertions;

namespace LLL.ComputedExpression.EFCore.Tests.AffectedEntities;

public class StopTrackingTests
{
    [Fact]
    public async void Property()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var changesProvider = context.GetChangesProvider(
            (Person p) => p.AsComputedUntracked().FirstName + " " + p.LastName,
            c => c.VoidChange());

        changesProvider!.ToDebugString()
            .Should().Be("EntitiesWithPropertyChange(Person.LastName)");
    }

    [Fact]
    public async void Collection()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var changesProvider = context.GetChangesProvider(
            (Person p) => p.AsComputedUntracked().Pets.Count(p => p.Type == "Cat"),
            c => c.VoidChange());

        changesProvider.Should().BeNull();
    }

    [Fact]
    public async void CollectionItemProperty()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var changesProvider = context.GetChangesProvider(
            (Person p) => p.Pets.Count(p => p.AsComputedUntracked().Type == "Cat" && p.Color == "Black"),
            c => c.VoidChange());

        changesProvider!.ToDebugString()
            .Should().Be("Concat(EntitiesWithNavigationChange(Person.Pets), Load(EntitiesWithPropertyChange(Pet.Color), Owner))");
    }
}
