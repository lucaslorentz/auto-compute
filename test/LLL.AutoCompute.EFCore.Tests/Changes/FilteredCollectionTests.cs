﻿using System.Linq.Expressions;
using FluentAssertions;
using LLL.AutoCompute.EFCore.Internal;
using LLL.AutoCompute.ChangeCalculations;

namespace LLL.AutoCompute.EFCore.Tests.Changes;

public class FilteredCollectionTests
{
    private static readonly Expression<Func<Person, int>> _computedExpression = (Person person) => person.Pets.Where(p => p.Type == "Cat").Count();

    [Fact]
    public async Task TestCollectionElementAdded()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = new Pet { Type = "Cat" };
        person.Pets.Add(pet);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { person, new ValueChange<int>(1, 2)}
        });
    }

    [Fact]
    public async Task TestCollectionElementAddedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = new Pet { Type = "Cat", Owner = person };
        context.Add(pet);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { person, new ValueChange<int>(1, 2)}
        });
    }

    [Fact]
    public async Task TestCollectionElementModified()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Modified";

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { pet.Owner!, new ValueChange<int>(1, 0)}
        });
    }

    [Fact]
    public async Task TestCollectionElementRemoved()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        person.Pets.Remove(pet);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { person, new ValueChange<int>(1, 0)}
        });
    }

    [Fact]
    public async Task TestCollectionElementRemovedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        pet.Owner = null;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { person, new ValueChange<int>(1, 0)}
        });
    }

    [Fact]
    public async Task DeltaChangesTest()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;

        var changesProvider = context.GetChangesProvider(_computedExpression, default, static c => c.ValueChange())!;

        // Add a cat
        person.Pets.Add(new Pet { Type = "Cat" });
        var changes = await changesProvider.GetChangesAsync();
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { person, new ValueChange<int>(1, 2)}
        });

        // No other change
        changes = await changesProvider.GetChangesAsync();
        changes.Should().BeEmpty();

        // Remove a cat
        person.Pets.RemoveAt(0);
        changes = await changesProvider.GetChangesAsync();
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<int>>{
            { person, new ValueChange<int>(2, 1)}
        });
    }
}
