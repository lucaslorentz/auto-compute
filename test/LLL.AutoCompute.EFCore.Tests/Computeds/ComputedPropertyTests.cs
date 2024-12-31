using FluentAssertions;

namespace LLL.AutoCompute.EFCore.Tests.Computeds;

public class ComputedPropertyTests
{
    [Fact]
    public async Task TestProperty()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(1)!;

        person.FullName.Should().Be("John Doe");
        person.Description.Should().Be("John Doe (1 pets)");

        person.FirstName = "Jane";

        await context.SaveChangesAsync();

        person.FullName.Should().Be("Jane Doe");
        person.Description.Should().Be("Jane Doe (1 pets)");
    }

    [Fact]
    public async Task TestCollectionElementAdded()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(1)!;

        person.Total.Should().Be(1);
        person.Description.Should().Be("John Doe (1 pets)");

        var pet = new Pet { Type = "Cat" };
        person.Pets.Add(pet);

        await context.SaveChangesAsync();

        person.Total.Should().Be(2);
        person.HasCats.Should().BeTrue();
        person.Description.Should().Be("John Doe (2 pets)");
    }

    [Fact]
    public async Task TestCollectionElementAddedInverse()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(1)!;

        person.Total.Should().Be(1);
        person.HasCats.Should().BeTrue();
        person.Description.Should().Be("John Doe (1 pets)");

        var pet = new Pet { Type = "Cat", Owner = person };
        context.Add(pet);

        await context.SaveChangesAsync();

        person.Total.Should().Be(2);
        person.HasCats.Should().BeTrue();
        person.Description.Should().Be("John Doe (2 pets)");
    }

    [Fact]
    public async Task TestCollectionElementModified()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(1)!;

        person.Total.Should().Be(1);
        person.HasCats.Should().BeTrue();
        person.Description.Should().Be("John Doe (1 pets)");

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Modified";

        await context.SaveChangesAsync();

        person.Total.Should().Be(0);
        person.HasCats.Should().BeFalse();
        person.Description.Should().Be("John Doe (1 pets)");
    }

    [Fact]
    public async Task TestCollectionElementRemoved()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(1)!;

        person.Total.Should().Be(1);
        person.HasCats.Should().BeTrue();
        person.Description.Should().Be("John Doe (1 pets)");

        var pet = context!.Set<Pet>().Find(1)!;
        person.Pets.Remove(pet);

        await context.SaveChangesAsync();

        person.Total.Should().Be(0);
        person.HasCats.Should().BeFalse();
        person.Description.Should().Be("John Doe (0 pets)");
    }

    [Fact]
    public async Task TestCollectionElementRemovedInverse()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(1)!;
        person.Description.Should().Be("John Doe (1 pets)");

        person.Total.Should().Be(1);
        person.HasCats.Should().BeTrue();

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Owner = null;

        await context.SaveChangesAsync();

        person.Total.Should().Be(0);
        person.HasCats.Should().BeFalse();
        person.Description.Should().Be("John Doe (0 pets)");
    }

    private static async Task<PersonDbContext> GetDbContextAsync()
    {
        return await TestDbContext.Create<PersonDbContext>(modelBuilder =>
        {
            var personBuilder = modelBuilder.Entity<Person>();
            personBuilder.ComputedProperty(p => p.FullName, p => p.FirstName + " " + p.LastName);
            personBuilder.ComputedProperty(p => p.Total, p => p.Pets.Count(x => x.Type == "Cat"));
            personBuilder.ComputedProperty(p => p.HasCats, p => p.Pets.Any(x => x.Type == "Cat"));
            personBuilder.ComputedProperty(p => p.Description, p => p.FullName + " (" + p.Pets.Count() + " pets)");
        });
    }
}
