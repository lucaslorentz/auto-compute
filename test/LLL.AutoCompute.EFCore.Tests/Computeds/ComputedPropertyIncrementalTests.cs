using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore.Tests.Computeds;

public class ComputedPropertyIncrementalTests
{
    [Fact]
    public async Task TestCollectionElementAdded()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;

        person.NumberOfCats.Should().Be(1);
        person.NumberOfCatsAndDogsConcat.Should().Be(1);

        var pet = new Pet { Id = "New", Type = PetType.Cat };
        person.Pets.Add(pet);

        await context.SaveChangesAsync();

        person.NumberOfCats.Should().Be(2);
        person.NumberOfCatsAndDogsConcat.Should().Be(2);
        person.HasCats.Should().BeTrue();
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementAddedInverse()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;

        person.NumberOfCats.Should().Be(1);
        person.NumberOfCatsAndDogsConcat.Should().Be(1);

        var pet = new Pet { Id = "New", Type = PetType.Cat, Owner = person };
        context.Add(pet);

        await context.SaveChangesAsync();

        person.NumberOfCats.Should().Be(2);
        person.NumberOfCatsAndDogsConcat.Should().Be(2);
        person.HasCats.Should().BeTrue();
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementModified()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;

        person.NumberOfCats.Should().Be(1);
        person.NumberOfCatsAndDogsConcat.Should().Be(1);

        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Type = PetType.Other;

        await context.SaveChangesAsync();

        person.NumberOfCats.Should().Be(0);
        person.NumberOfCatsAndDogsConcat.Should().Be(0);
        person.HasCats.Should().BeFalse();
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementRemoved()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;

        person.NumberOfCats.Should().Be(1);
        person.NumberOfCatsAndDogsConcat.Should().Be(1);

        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        person.Pets.Remove(pet);

        await context.SaveChangesAsync();

        person.NumberOfCats.Should().Be(0);
        person.NumberOfCatsAndDogsConcat.Should().Be(0);
        person.HasCats.Should().BeFalse();
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementRemovedInverse()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;

        person.NumberOfCats.Should().Be(1);
        person.NumberOfCatsAndDogsConcat.Should().Be(1);

        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Owner = null;

        await context.SaveChangesAsync();

        person.NumberOfCats.Should().Be(0);
        person.NumberOfCatsAndDogsConcat.Should().Be(0);
        person.HasCats.Should().BeFalse();
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    private static async Task<DbContext> GetDbContextAsync()
    {
        return await TestDbContext.Create(
            options => new PersonDbContext(options, new PersonDbContextParams {
                UseIncrementalComputation = true
            }),
            useLazyLoadingProxies: false);
    }
}
