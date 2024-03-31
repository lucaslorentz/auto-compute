# Computed Expression ![CI](https://github.com/lucaslorentz/computed-expression/workflows/CI/badge.svg) [![Coverage Status](https://coveralls.io/repos/github/lucaslorentz/computed-expression/badge.svg)](https://coveralls.io/github/lucaslorentz/computed-expression)

Computed Expression is a library designed to automatically maintain derived data within .NET EF Core applications.

Whether you're implementing denormalization strategies or handling any other form of derived data in your EF Core projects, Computed Expression offers a straightforward solution. It tracks changes across related entities, automatically updating derived properties, thereby reducing complexity, maintenance overhead, and ensuring consistency throughout your application.

This library is basically an automatic implementation of the approach described by Microsoft in: https://learn.microsoft.com/en-us/ef/core/performance/modeling-for-performance#update-cache-columns-when-inputs-change

## Getting Started

Install nuget package
```sh
dotnet add package LLL.ComputedExpression.EFCore
```

Enable by calling UseComputeds from DbContext OnConfiguring:
```
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    => optionsBuilder.UseComputeds();
```
Or while adding DbContext to service collection:
```
services.AddDbContext<PersonDbContext>(
    b => b.UseComputeds());
```

Next, define computed properties in your EF Core mappings. Here's a demonstration:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    var personBuilder = modelBuilder.Entity<Person>();
    personBuilder.ComputedProperty(person => person.FullName, person => person.FirstName + " " + person.LastName);
    personBuilder.ComputedProperty(person => person.NumberOfCats, person => person.Pets.Count(pet => pet.Type == "Cat"));
    personBuilder.ComputedProperty(person => person.HasCats, person => person.Pets.Any(pet => pet.Type == "Cat"));
    personBuilder.ComputedProperty(person => person.Description, person => person.FullName + " (" + person.Pets.Count() + " pets)");
}
```

That's it! Now, all defined computed properties will be automatically updated during `dbContext.SaveChanges()`.

Check [PersonDbContext](test/LLL.ComputedExpression.EFCore.Tests/PersonDbContext.cs) for a complete example.

## How it works

This library operates by meticulously analyzing computed expressions and tracking all referenced data within them. It then traverses inverse navigations to pinpoint all entities that could be affected by changes to the referenced data.

### Basic example
Consider the following basic example:
```csharp
personBuilder.ComputedProperty(person => person.FullName, person => person.FirstName + " " + person.LastName);
```
In this scenario, the following data will be monitored for changes:
- Person's FirstName property.
- Person's LastName property.

Whenever a change occurs in any of these monitored properties, the FullName property of that person will be automatically updated with the result of the computed expression.

### Navigation example
Let's explore a navigation example:
```csharp
personBuilder.ComputedProperty(person => person.NumberOfCats, person => person.Pets.Count(pet => pet.Type == "Cat"));
```
Here, the library will monitor changes in the following data:
- Person's Pets collection, including changes made to the inverse reference in the Pets entities.
- Pet's Type property.

Whenever a change occurs in any of these monitored properties, the NumberOfCats property of that person will be automatically updated with the result of the computed expression.

For changes in the **Type** property of **Pets**, the library automatically traverses the inverse navigation of **Person.Pets** (i.e., **Pets.Owner**) to identify which **Persons** could be affected by changes in the **Type** of a **Pet**.

### Complex example

Consider a more intricate scenario:
```csharp
personBuilder.ComputedProperty(
  person => person.NumberOfBlackKittens,
  person => person.Pets.Where(pet => pet.Type == "Cat")
    .SelectMany(pet => pet.Offsprings)
    .Where(pet => pet.Color == "Black")
    .Count()
);
```
In this example, the library also monitors changes to the pets' **Color** property. Whenever a change occurs, it identifies the affected pets by traversing the Offspring's inverse navigation. Subsequently, it determines the affected persons by traversing the Pets' inverse navigation.

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
