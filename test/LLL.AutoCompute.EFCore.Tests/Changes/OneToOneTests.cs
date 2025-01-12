using System.Linq.Expressions;
using FluentAssertions;
using LLL.AutoCompute.ChangeCalculations;

namespace LLL.AutoCompute.EFCore.Tests.Changes;

public class OneToOneTests
{
    private static readonly Expression<Func<Person, PetType?>> _computedExpression = (Person person) => person.FavoritePet != null ? person.FavoritePet.Type : null;

    [Fact]
    public async Task TestReferenceSet()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        person.FavoritePet = pet;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<PetType?>>{
            { person, new ValueChange<PetType?>(null, PetType.Cat)}
        });
    }

    [Fact]
    public async Task TestInverseReferenceSet()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.FavoritePetInverse = person;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<PetType?>>{
            { person, new ValueChange<PetType?>(null, PetType.Cat)}
        });
    }

    [Fact]
    public async Task TestReferencedEntityModified()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        person.FavoritePet = pet;
        await context.SaveChangesAsync();

        pet.Type = PetType.Dog;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<PetType?>>{
            { person, new ValueChange<PetType?>(PetType.Cat, PetType.Dog)}
        });
    }

    [Fact]
    public async Task TestReferenceUnset()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        person.FavoritePet = pet;
        await context.SaveChangesAsync();

        person.FavoritePet = null;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<PetType?>>{
            { person, new ValueChange<PetType?>(PetType.Cat, null)}
        });
    }

    [Fact]
    public async Task TestReferenceUnsetInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        person.FavoritePet = pet;
        await context.SaveChangesAsync();

        pet.FavoritePetInverse = null;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.ValueChange());
        changes.Should().BeEquivalentTo(new Dictionary<Person, ValueChange<PetType?>>{
            { person, new ValueChange<PetType?>(PetType.Cat, null)}
        });
    }
}
