﻿using System.Linq.Expressions;
using FluentAssertions;

namespace LLL.ComputedExpression.EFCore.Tests.Changes;

public class FilteredCollectionTests
{
    private static readonly Expression<Func<Person, int>> _computedExpression = (Person person) => person.Pets.Where(p => p.Type == "Cat").Count();

    [Fact]
    public async void TestCollectionElementAdded()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = new Pet { Type = "Cat" };
        person.Pets.Add(pet);

        var changes = await context.GetChangesAsync(_computedExpression);
        changes.Should().BeEquivalentTo(new Dictionary<Person, ConstValueChange<int>>{
            { person, new ConstValueChange<int>(1, 2)}
        });
    }

    [Fact]
    public async void TestCollectionElementAddedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = new Pet { Type = "Cat", Owner = person };
        context.Add(pet);

        var changes = await context.GetChangesAsync(_computedExpression);
        changes.Should().BeEquivalentTo(new Dictionary<Person, ConstValueChange<int>>{
            { person, new ConstValueChange<int>(1, 2)}
        });
    }

    [Fact]
    public async void TestCollectionElementModified()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Modified";

        var changes = await context.GetChangesAsync(_computedExpression);
        changes.Should().BeEquivalentTo(new Dictionary<Person, ConstValueChange<int>>{
            { pet.Owner!, new ConstValueChange<int>(1, 0)}
        });
    }

    [Fact]
    public async void TestCollectionElementRemoved()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        person.Pets.Remove(pet);

        var changes = await context.GetChangesAsync(_computedExpression);
        changes.Should().BeEquivalentTo(new Dictionary<Person, ConstValueChange<int>>{
            { person, new ConstValueChange<int>(1, 0)}
        });
    }

    [Fact]
    public async void TestCollectionElementRemovedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        pet.Owner = null;

        var changes = await context.GetChangesAsync(_computedExpression);
        changes.Should().BeEquivalentTo(new Dictionary<Person, ConstValueChange<int>>{
            { person, new ConstValueChange<int>(1, 0)}
        });
    }

    [Fact]
    public async void DeltaChangesTest()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;

        // Add a cat
        person.Pets.Add(new Pet { Type = "Cat" });
        var changes = await context.GetDeltaChangesAsync(_computedExpression);
        changes.Should().BeEquivalentTo(new Dictionary<Person, ConstValueChange<int>>{
            { person, new ConstValueChange<int>(1, 2)}
        });

        // No other change
        changes = await context.GetDeltaChangesAsync(_computedExpression);
        changes.Should().BeEmpty();

        // Remove a cat
        person.Pets.RemoveAt(0);
        changes = await context.GetDeltaChangesAsync(_computedExpression);
        changes.Should().BeEquivalentTo(new Dictionary<Person, ConstValueChange<int>>{
            { person, new ConstValueChange<int>(2, 1)}
        });
    }
}
