using FluentAssertions;

namespace LLL.Computed.EFCore.Tests.Computeds;

public class ComputedsTests
{
    [Fact]
    public async void TestProperty()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;

        person.FullName.Should().Be("John Doe");
        person.Description.Should().Be("John Doe (1 pets)");

        person.FirstName = "Jane";

        await context.SaveChangesAsync();

        person.FullName.Should().Be("Jane Doe");
        person.Description.Should().Be("Jane Doe (1 pets)");
    }

    [Fact]
    public async void TestCollectionElementAdded()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;

        person.NumberOfCats.Should().Be(1);
        person.Description.Should().Be("John Doe (1 pets)");

        var pet = new Pet { Type = "Cat" };
        person.Pets.Add(pet);

        await context.SaveChangesAsync();

        person.NumberOfCats.Should().Be(2);
        person.HasCats.Should().BeTrue();
        person.Description.Should().Be("John Doe (2 pets)");
    }

    [Fact]
    public async void TestCollectionElementAddedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;

        person.NumberOfCats.Should().Be(1);
        person.HasCats.Should().BeTrue();
        person.Description.Should().Be("John Doe (1 pets)");

        var pet = new Pet { Type = "Cat", Owner = person };
        context.Add(pet);

        await context.SaveChangesAsync();

        person.NumberOfCats.Should().Be(2);
        person.HasCats.Should().BeTrue();
        person.Description.Should().Be("John Doe (2 pets)");
    }

    [Fact]
    public async void TestCollectionElementModified()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;

        person.NumberOfCats.Should().Be(1);
        person.HasCats.Should().BeTrue();
        person.Description.Should().Be("John Doe (1 pets)");

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Modified";

        await context.SaveChangesAsync();

        person.NumberOfCats.Should().Be(0);
        person.HasCats.Should().BeFalse();
        person.Description.Should().Be("John Doe (1 pets)");
    }

    [Fact]
    public async void TestCollectionElementRemoved()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;

        person.NumberOfCats.Should().Be(1);
        person.HasCats.Should().BeTrue();
        person.Description.Should().Be("John Doe (1 pets)");

        var pet = context!.Set<Pet>().Find(1)!;
        person.Pets.Remove(pet);

        await context.SaveChangesAsync();

        person.NumberOfCats.Should().Be(0);
        person.HasCats.Should().BeFalse();
        person.Description.Should().Be("John Doe (0 pets)");
    }

    [Fact]
    public async void TestCollectionElementRemovedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        person.Description.Should().Be("John Doe (1 pets)");

        person.NumberOfCats.Should().Be(1);
        person.HasCats.Should().BeTrue();

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Owner = null;

        await context.SaveChangesAsync();

        person.NumberOfCats.Should().Be(0);
        person.HasCats.Should().BeFalse();
        person.Description.Should().Be("John Doe (0 pets)");
    }
}
