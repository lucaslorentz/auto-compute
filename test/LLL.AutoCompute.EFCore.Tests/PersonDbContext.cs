using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore.Tests;

public class Person
{
    public virtual required string Id { get; set; }
    public virtual string? FirstName { get; set; }
    public virtual string? LastName { get; set; }
    public virtual Pet? FavoritePet { get; set; }
    public virtual IList<Pet> Pets { get; protected set; } = [];
    public virtual IList<Person> Friends { get; protected set; } = [];
    public virtual IList<Person> FriendsInverse { get; protected set; } = [];
    public virtual IList<FriendsJoin> FriendsJoin { get; protected set; } = [];
    public virtual IList<FriendsJoin> FriendsInverseJoin { get; protected set; } = [];
    public virtual IList<Person> Relatives { get; protected set; } = [];
    public virtual IList<Person> RelativesInverse { get; protected set; } = [];
    public virtual IList<RelativesJoin> RelativesJoin { get; protected set; } = [];
    public virtual IList<RelativesJoin> RelativesInverseJoin { get; protected set; } = [];

    public virtual string? FullName { get; protected set; }
    public virtual int NumberOfOrangePets { get; protected set; }
    public virtual bool HasOrangePets { get; protected set; }
    public virtual int NumberOfBlackPets { get; protected set; }
    public virtual bool HasBlackPets { get; protected set; }
    public virtual int NumberOfPets { get; protected set; }
    public virtual int NumberOfOrangeAndBlackPets { get; protected set; }
    public virtual string? Description { get; protected set; }
    public virtual int FriendsCount { get; protected set; }
    public virtual int RelativesCount { get; protected set; }
    public virtual int DistinctFriendRelativesCount { get; protected set; }
}

public abstract class Pet
{
    public virtual required string Id { get; set; }
    public virtual PetColor? Color { get; set; }
    public virtual Person? Owner { get; set; }
    public virtual Person? FavoritePetInverse { get; set; }
}

public class Cat : Pet { }
public class Dog : Pet { }

public enum PetColor
{
    Orange,
    Black,
    White,
    Other
}

public class RelativesJoin
{
    public virtual required Person FromPerson { get; init; }
    public virtual required Person ToPerson { get; init; }
}

public class FriendsJoin
{
    public virtual required Person FromPerson { get; init; }
    public virtual required Person ToPerson { get; init; }
}

record class PersonDbContextParams
{
    public bool UseIncrementalComputation { get; set; }
    public Action<ModelBuilder>? SetupObservers { get; set; }
}

class PersonDbContext(
    DbContextOptions options,
    PersonDbContextParams parameters
) : DbContext(options), ITestDbContext<PersonDbContext>
{
    public const string PersonAId = "A";
    public const string PersonAPet1Id = "A.Pet.1";
    public const string PersonBId = "B";

    public object? ConfigurationKey => parameters;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var personBuilder = modelBuilder.Entity<Person>();

        personBuilder.HasOne(e => e.FavoritePet)
            .WithOne(e => e.FavoritePetInverse)
            .HasForeignKey<Person>("FavoritePetId");

        personBuilder.HasMany(e => e.Friends)
            .WithMany(e => e.FriendsInverse)
            .UsingEntity<FriendsJoin>(
                l => l.HasOne<Person>(x => x.ToPerson).WithMany(x => x.FriendsInverseJoin),
                r => r.HasOne<Person>(x => x.FromPerson).WithMany(x => x.FriendsJoin)
            );

        personBuilder.HasMany(e => e.Relatives)
            .WithMany(e => e.RelativesInverse)
            .UsingEntity<RelativesJoin>(
                l => l.HasOne<Person>(x => x.ToPerson).WithMany(x => x.RelativesInverseJoin),
                r => r.HasOne<Person>(x => x.FromPerson).WithMany(x => x.RelativesJoin)
            );

        personBuilder.ComputedProperty(
            p => p.FullName,
            p => p.FirstName + " " + p.LastName);

        personBuilder.ComputedProperty(
            p => p.NumberOfOrangePets,
            p => p.Pets.Count(x => x.Color == PetColor.Orange),
            c => parameters.UseIncrementalComputation ? c.NumberIncremental() : c.CurrentValue());

        personBuilder.ComputedProperty(
            p => p.NumberOfBlackPets,
            p => p.Pets.Count(x => x.Color == PetColor.Black),
            c => parameters.UseIncrementalComputation ? c.NumberIncremental() : c.CurrentValue());

        personBuilder.ComputedProperty(
            p => p.NumberOfPets,
            p => p.Pets.Count,
            c => parameters.UseIncrementalComputation ? c.NumberIncremental() : c.CurrentValue());

        personBuilder.ComputedProperty(
            p => p.HasOrangePets,
            p => p.NumberOfOrangePets > 0);

        personBuilder.ComputedProperty(
            p => p.HasBlackPets,
            p => p.NumberOfBlackPets > 0);

        personBuilder.ComputedProperty(
            p => p.NumberOfOrangeAndBlackPets,
            p => p.Pets.Where(x => x.Color == PetColor.Orange)
                .Concat(p.Pets.Where(x => x.Color == PetColor.Black))
                .Count(),
            c => parameters.UseIncrementalComputation ? c.NumberIncremental() : c.CurrentValue());

        personBuilder.ComputedProperty(p => p.Description, p => p.FullName + " (" + p.NumberOfPets + " pets)");

        var petBuilder = modelBuilder.Entity<Pet>();
        petBuilder.HasDiscriminator<string>("Type")
            .HasValue<Cat>("Cat")
            .HasValue<Dog>("Dog");

        parameters.SetupObservers?.Invoke(modelBuilder);
    }

    public void SeedData()
    {
        var personA = new Person
        {
            Id = PersonAId,
            FirstName = "John",
            LastName = "Doe",
            Pets = {
                new Cat { Id = PersonAPet1Id, Color = PetColor.Orange }
            },
        };

        var personB = new Person
        {
            Id = PersonBId,
            FirstName = "Jane",
            LastName = "Doe",
            Friends = {
                personA
            }
        };

        Add(personA);
        Add(personB);
    }

    public static PersonDbContext Create(DbContextOptions options)
    {
        return new PersonDbContext(options, new PersonDbContextParams());
    }
}
