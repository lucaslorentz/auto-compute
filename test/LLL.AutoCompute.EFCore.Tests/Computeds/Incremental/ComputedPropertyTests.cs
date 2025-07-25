using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore.Tests.Computeds.Incremental;

public class ComputedPropertyTests
{
    [Fact]
    public async Task TestCollectionElementAdded()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;

        VerifyInitialStatePersonA(person);

        var pet = new Cat { Id = "New", Color = PetColor.Orange };
        person.Pets.Add(pet);

        await context.SaveChangesAsync();

        person.NumberOfOrangePets.Should().Be(2);
        person.HasOrangePets.Should().BeTrue();
        person.NumberOfBlackPets.Should().Be(0);
        person.HasBlackPets.Should().BeFalse();
        person.NumberOfPets.Should().Be(2);
        person.NumberOfOrangeAndBlackPets.Should().Be(2);
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementAddedInverse()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;

        VerifyInitialStatePersonA(person);

        var pet = new Cat { Id = "New", Color = PetColor.Orange, Owner = person };
        context.Add(pet);

        await context.SaveChangesAsync();

        person.NumberOfOrangePets.Should().Be(2);
        person.HasOrangePets.Should().BeTrue();
        person.NumberOfBlackPets.Should().Be(0);
        person.HasBlackPets.Should().BeFalse();
        person.NumberOfPets.Should().Be(2);
        person.NumberOfOrangeAndBlackPets.Should().Be(2);
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementModified()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;

        VerifyInitialStatePersonA(person);

        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Color = PetColor.Black;

        await context.SaveChangesAsync();

        person.NumberOfOrangePets.Should().Be(0);
        person.HasOrangePets.Should().BeFalse();
        person.NumberOfBlackPets.Should().Be(1);
        person.HasBlackPets.Should().BeTrue();
        person.NumberOfPets.Should().Be(1);
        person.NumberOfOrangeAndBlackPets.Should().Be(1);
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementRemoved()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;

        VerifyInitialStatePersonA(person);

        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        person.Pets.Remove(pet);

        await context.SaveChangesAsync();

        person.NumberOfOrangePets.Should().Be(0);
        person.HasOrangePets.Should().BeFalse();
        person.NumberOfBlackPets.Should().Be(0);
        person.HasBlackPets.Should().BeFalse();
        person.NumberOfPets.Should().Be(0);
        person.NumberOfOrangeAndBlackPets.Should().Be(0);
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementRemovedInverse()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;

        VerifyInitialStatePersonA(person);

        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Owner = null;

        await context.SaveChangesAsync();

        person.NumberOfOrangePets.Should().Be(0);
        person.HasOrangePets.Should().BeFalse();
        person.NumberOfBlackPets.Should().Be(0);
        person.HasBlackPets.Should().BeFalse();
        person.NumberOfPets.Should().Be(0);
        person.NumberOfOrangeAndBlackPets.Should().Be(0);
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    private static async Task<DbContext> GetDbContextAsync()
    {
        return await TestDbContextFactory.Create(
            options => new PersonDbContext(options, new PersonDbContextParams
            {
                UseIncrementalComputation = true
            }),
            useLazyLoadingProxies: false);
    }

    private static void VerifyInitialStatePersonA(Person person)
    {
        person.NumberOfOrangePets.Should().Be(1);
        person.HasOrangePets.Should().BeTrue();
        person.NumberOfBlackPets.Should().Be(0);
        person.HasBlackPets.Should().BeFalse();
        person.NumberOfPets.Should().Be(1);
        person.NumberOfOrangeAndBlackPets.Should().Be(1);
    }
}
