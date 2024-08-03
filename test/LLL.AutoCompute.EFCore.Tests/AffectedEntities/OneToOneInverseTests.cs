using System.Linq.Expressions;
using FluentAssertions;

namespace LLL.AutoCompute.EFCore.Tests.AffectedEntities;

public class OneToOneInverseTests
{
    private static readonly Expression<Func<Pet, string?>> _computedExpression = (Pet pet) => pet.FavoritePetInverse != null ? pet.FavoritePetInverse.FirstName : null;

    [Fact]
    public async void TestDebugString()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var changesProvider = context.GetChangesProvider(_computedExpression, default, static c => c.Void());
        changesProvider!.ToDebugString()
            .Should().Be("Concat(EntitiesWithNavigationChange(Pet.FavoritePetInverse), Load(EntitiesWithPropertyChange(Person.FirstName), FavoritePet))");
    }

    [Fact]
    public async void TestReferenceSet()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        person.FavoritePet = pet;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.Void());
        changes.Keys.Should().BeEquivalentTo([pet]);
    }

    [Fact]
    public async void TestInverseReferenceSet()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        pet.FavoritePetInverse = person;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.Void());
        changes.Keys.Should().BeEquivalentTo([pet]);
    }

    [Fact]
    public async void TestReferencedEntityModified()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        person.FavoritePet = pet;
        await context.SaveChangesAsync();

        person.FirstName = "Modified";

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.Void());
        changes.Keys.Should().BeEquivalentTo([pet]);
    }

    [Fact]
    public async void TestReferenceUnset()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        person.FavoritePet = pet;
        await context.SaveChangesAsync();

        person.FavoritePet = null;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.Void());
        changes.Keys.Should().BeEquivalentTo([pet]);
    }

    [Fact]
    public async void TestReferenceUnsetInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        person.FavoritePet = pet;
        await context.SaveChangesAsync();

        pet.FavoritePetInverse = null;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.Void());
        changes.Keys.Should().BeEquivalentTo([pet]);
    }
}
