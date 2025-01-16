using FluentAssertions;

namespace LLL.AutoCompute.EFCore.Tests.Computeds;

public class ComputedPropertyTests
{
    [Fact]
    public async Task TestPropertyModified()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;

        VerifyInitialStatePersonA(person);

        person.FirstName = "Jane";

        await context.SaveChangesAsync();

        person.FullName.Should().Be("Jane Doe");
        person.Description.Should().Be("Jane Doe (1 pets)");
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementAdded()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;

        VerifyInitialStatePersonA(person);

        var pet = new Pet { Id = "New", Type = PetType.Cat };
        person.Pets.Add(pet);

        await context.SaveChangesAsync();

        person.NumberOfCats.Should().Be(2);
        person.HasCats.Should().BeTrue();
        person.NumberOfDogs.Should().Be(0);
        person.HasDogs.Should().BeFalse();
        person.NumberOfPets.Should().Be(2);
        person.NumberOfCatsAndDogsConcat.Should().Be(2);
        person.Description.Should().Be("John Doe (2 pets)");
    }

    [Fact]
    public async Task TestCollectionElementAddedInverse()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;

        VerifyInitialStatePersonA(person);

        var pet = new Pet { Id = "New", Type = PetType.Cat, Owner = person };
        context.Add(pet);

        await context.SaveChangesAsync();

        person.NumberOfCats.Should().Be(2);
        person.HasCats.Should().BeTrue();
        person.NumberOfDogs.Should().Be(0);
        person.HasDogs.Should().BeFalse();
        person.NumberOfPets.Should().Be(2);
        person.NumberOfCatsAndDogsConcat.Should().Be(2);
        person.Description.Should().Be("John Doe (2 pets)");
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeTrue();
    }

    [Fact]
    public async Task TestCollectionElementModified()
    {
        using var context = await GetDbContextAsync();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;

        VerifyInitialStatePersonA(person);

        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Type = PetType.Dog;

        await context.SaveChangesAsync();

        person.NumberOfCats.Should().Be(0);
        person.HasCats.Should().BeFalse();
        person.NumberOfDogs.Should().Be(1);
        person.HasDogs.Should().BeTrue();
        person.NumberOfPets.Should().Be(1);
        person.NumberOfCatsAndDogsConcat.Should().Be(1);
        person.Description.Should().Be("John Doe (1 pets)");
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeTrue();
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
        
        person.NumberOfCats.Should().Be(0);
        person.HasCats.Should().BeFalse();
        person.NumberOfDogs.Should().Be(0);
        person.HasDogs.Should().BeFalse();
        person.NumberOfPets.Should().Be(0);
        person.NumberOfCatsAndDogsConcat.Should().Be(0);
        person.Description.Should().Be("John Doe (0 pets)");
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeTrue();
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
        
        person.NumberOfCats.Should().Be(0);
        person.HasCats.Should().BeFalse();
        person.NumberOfDogs.Should().Be(0);
        person.HasDogs.Should().BeFalse();
        person.NumberOfPets.Should().Be(0);
        person.NumberOfCatsAndDogsConcat.Should().Be(0);
        person.Description.Should().Be("John Doe (0 pets)");
        context.Entry(person).Navigation(nameof(Person.Pets)).IsLoaded.Should().BeTrue();
    }

    private static async Task<PersonDbContext> GetDbContextAsync()
    {
        return await TestDbContext.Create<PersonDbContext>();
    }

    private static void VerifyInitialStatePersonA(Person person)
    {
        person.FullName.Should().Be("John Doe");
        person.NumberOfCats.Should().Be(1);
        person.HasCats.Should().BeTrue();
        person.NumberOfDogs.Should().Be(0);
        person.HasDogs.Should().BeFalse();
        person.NumberOfPets.Should().Be(1);
        person.NumberOfCatsAndDogsConcat.Should().Be(1);
        person.Description.Should().Be("John Doe (1 pets)");
    }
}
