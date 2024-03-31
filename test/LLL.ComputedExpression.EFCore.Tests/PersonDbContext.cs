using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LLL.ComputedExpression.EFCore.Tests;

public class Person
{
    public virtual int Id { get; set; }
    public virtual string? FirstName { get; set; }
    public virtual string? LastName { get; set; }
    public virtual string? FullName { get; protected set; }
    public virtual IList<Pet> Pets { get; protected set; } = [];
    public virtual bool HasCats { get; protected set; }
    public virtual string? Description { get; protected set; }
    public virtual int Total { get; protected set; }
}

public class Pet
{
    public virtual int Id { get; set; }
    public virtual string? Color { get; set; }
    public virtual string? Type { get; set; }
    public virtual Person? Owner { get; set; }
}

class PersonDbContext(
    DbContextOptions options,
    Action<ModelBuilder>? customizeModel
) : DbContext(options), ITestDbContext<PersonDbContext>
{
    public Action<ModelBuilder>? CustomizeModel => customizeModel;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Person>();
        modelBuilder.Entity<Pet>();
        customizeModel?.Invoke(modelBuilder);
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

    public static PersonDbContext Create(
        DbContextOptions options,
        Action<ModelBuilder>? customizeModel)
    {
        return new PersonDbContext(options, customizeModel);
    }
}
