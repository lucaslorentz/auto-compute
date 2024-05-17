using FluentAssertions;

namespace LLL.ComputedExpression.EFCore.Tests.Computeds;

public class IncrementalFilteredCollectionTests
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
    }

    [Fact]
    public async void TestCollectionElementModifiedToBeFilteredOut()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(1)!;

        person.Total.Should().Be(1);

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Modified";

        await context.SaveChangesAsync();

        person.Total.Should().Be(0);
    }

    [Fact]
    public async void TestCollectionElementModifiedToContinue()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(1)!;

        person.Total.Should().Be(1);

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Dog";

        await context.SaveChangesAsync();

        person.Total.Should().Be(1);
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
    }

    private static async Task<PersonDbContext> GetDbContextAsync()
    {
        return await TestDbContext.Create<PersonDbContext>(modelBuilder =>
        {
            var personBuilder = modelBuilder.Entity<Person>();

            personBuilder.ComputedProperty(
                p => p.Total,
                p =>
                    p.Pets.Where(x => x.Type == "Cat")
                    .Concat(p.Pets.Where(x => x.Type == "Dog"))
                    .Count(),
                static c => c.NumberIncremental()
            );
        });
    }
}
