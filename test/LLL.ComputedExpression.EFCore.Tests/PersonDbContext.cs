using LLL.Computed.Incremental;
using Microsoft.EntityFrameworkCore;

namespace LLL.Computed.EFCore.Tests;

public class Person
{
    public virtual int Id { get; set; }
    public virtual string? FirstName { get; set; }
    public virtual string? LastName { get; set; }
    public virtual string? FullName { get; protected set; }
    public virtual IList<Pet> Pets { get; protected set; } = [];
    public virtual int NumberOfCats { get; protected set; }
    public virtual bool HasCats { get; protected set; }
    public virtual string? Description { get; protected set; }
    public virtual int NumberOfCatsIncremental { get; protected set; }
    public virtual int NumberOfCatsOrDogsIncrementalFiltered { get; protected set; }
}

public class Pet
{
    public virtual int Id { get; set; }
    public virtual string? Color { get; set; }
    public virtual string? Type { get; set; }
    public virtual Person? Owner { get; set; }
}

class PersonDbContext(DbContextOptions<PersonDbContext> options) : DbContext(options),
    ISeededDbContext<PersonDbContext>
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var personBuilder = modelBuilder.Entity<Person>();
        personBuilder.ComputedProperty(p => p.FullName, p => p.FirstName + " " + p.LastName);
        personBuilder.ComputedProperty(p => p.NumberOfCats, p => p.Pets.Count(x => x.Type == "Cat"));
        personBuilder.ComputedProperty(p => p.HasCats, p => p.Pets.Any(x => x.Type == "Cat"));
        personBuilder.ComputedProperty(p => p.Description, p => p.FullName + " (" + p.Pets.Count() + " pets)");

        personBuilder.ComputedProperty(
            p => p.NumberOfCatsIncremental,
            new IncrementalComputedBuilder<Person, int>(0)
                .AddMany(p => p.Pets, p => p.Type == "Cat" ? 1 : 0)
        );

        personBuilder.ComputedProperty(
            p => p.NumberOfCatsOrDogsIncrementalFiltered,
            new IncrementalComputedBuilder<Person, int>(0)
                .AddMany(p => p.Pets.Where(x => x.Type == "Cat")
                    .Concat(p.Pets.Where(x => x.Type == "Dog")), p => 1)
        );
    }

    public static async void SeedData(PersonDbContext dbContext)
    {
        dbContext.Add(new Person
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Pets = {
                new Pet { Id = 1, Type = "Cat" }
            },
        });
    }
}
