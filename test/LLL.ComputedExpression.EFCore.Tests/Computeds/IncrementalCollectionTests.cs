using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace LLL.ComputedExpression.EFCore.Tests.Computeds;

public class IncrementalCollectionTests
{
    [Fact]
    public async void TestCollectionElementAdded()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(1)!;

        person.Total.Should().Be(1);

        var pet = new Pet { Type = "Cat" };
        person.Pets.Add(pet);

        await context.SaveChangesAsync();

        person.Total.Should().Be(2);
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async void TestCollectionElementAddedInverse()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(1)!;

        person.Total.Should().Be(1);

        var pet = new Pet { Type = "Cat", Owner = person };
        context.Add(pet);

        await context.SaveChangesAsync();

        person.Total.Should().Be(2);
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async void TestCollectionElementModified()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(1)!;

        person.Total.Should().Be(1);

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Modified";

        await context.SaveChangesAsync();

        person.Total.Should().Be(1);
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async void TestCollectionElementRemoved()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(1)!;

        person.Total.Should().Be(1);

        var pet = context!.Set<Pet>().Find(1)!;
        person.Pets.Remove(pet);

        await context.SaveChangesAsync();

        person.Total.Should().Be(0);
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async void TestCollectionElementRemovedInverse()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(1)!;

        person.Total.Should().Be(1);

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Owner = null;

        await context.SaveChangesAsync();

        person.Total.Should().Be(0);
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    private static async Task<DbContext> GetDbContextAsync()
    {
        return await TestDbContext.Create<PersonDbContext>(modelBuilder =>
        {
            var personBuilder = modelBuilder.Entity<Person>();
            personBuilder.ComputedProperty(
                p => p.Total,
                p => p.Pets.Count,
                static c => c.NumberIncremental()
            );
        });
    }
}
