using System.Linq.Expressions;
using FluentAssertions;
using LLL.ComputedExpression.ChangeCalculations;

namespace LLL.ComputedExpression.EFCore.Tests.Changes;

public class OneToOneTests
{
    private static readonly Expression<Func<Person, string?>> _computedExpression = (Person person) => person.FavoritePet != null ? person.FavoritePet.Type : null;

    [Fact]
    public async void TestReferenceSet()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        person.FavoritePet = pet;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<string?>>{
            { person, new ValueChange<string?>(null, "Cat")}
        });
    }

    [Fact]
    public async void TestInverseReferenceSet()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        pet.FavoritePetInverse = person;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<string?>>{
            { person, new ValueChange<string?>(null, "Cat")}
        });
    }

    [Fact]
    public async void TestReferencedEntityModified()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        person.FavoritePet = pet;
        await context.SaveAllChangesAsync();

        pet.Type = "Dog";

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<string?>>{
            { person, new ValueChange<string?>("Cat", "Dog")}
        });
    }

    [Fact]
    public async void TestReferenceUnset()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        person.FavoritePet = pet;
        await context.SaveAllChangesAsync();

        person.FavoritePet = null;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<string?>>{
            { person, new ValueChange<string?>("Cat", null)}
        });
    }

    [Fact]
    public async void TestReferenceUnsetInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        var pet = context!.Set<Pet>().Find(1)!;
        person.FavoritePet = pet;
        await context.SaveAllChangesAsync();

        pet.FavoritePetInverse = null;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<string?>>{
            { person, new ValueChange<string?>("Cat", null)}
        });
    }
}
