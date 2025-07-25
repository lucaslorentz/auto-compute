using System.Linq.Expressions;
using FluentAssertions;
using LLL.AutoCompute.ChangeCalculations;

namespace LLL.AutoCompute.EFCore.Tests.Changes;

public class OneToOneTests
{
    private static readonly Expression<Func<Person, PetColor?>> _computedExpression = person => person.FavoritePet != null ? person.FavoritePet.Color : null;

    [Fact]
    public async Task TestReferenceSet()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        person.FavoritePet = pet;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<PetColor?>>{
            { person, new ValueChange<PetColor?>(null, PetColor.Orange)}
        });
    }

    [Fact]
    public async Task TestInverseReferenceSet()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.FavoritePetInverse = person;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<PetColor?>>{
            { person, new ValueChange<PetColor?>(null, PetColor.Orange)}
        });
    }

    [Fact]
    public async Task TestReferencedEntityModified()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        person.FavoritePet = pet;
        await context.SaveChangesAsync();

        pet.Color = PetColor.Black;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<PetColor?>>{
            { person, new ValueChange<PetColor?>(PetColor.Orange, PetColor.Black)}
        });
    }

    [Fact]
    public async Task TestReferenceUnset()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        person.FavoritePet = pet;
        await context.SaveChangesAsync();

        person.FavoritePet = null;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<PetColor?>>{
            { person, new ValueChange<PetColor?>(PetColor.Orange, null)}
        });
    }

    [Fact]
    public async Task TestReferenceUnsetInverse()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        person.FavoritePet = pet;
        await context.SaveChangesAsync();

        pet.FavoritePetInverse = null;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<PetColor?>>{
            { person, new ValueChange<PetColor?>(PetColor.Orange, null)}
        });
    }
}
