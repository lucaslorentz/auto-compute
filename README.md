# Computed Expression ![CI](https://github.com/lucaslorentz/computed-expression/workflows/CI/badge.svg) [![Coverage Status](https://coveralls.io/repos/github/lucaslorentz/computed-expression/badge.svg?branch=main)](https://coveralls.io/github/lucaslorentz/computed-expression?branch=main)

Computed Expression is a powerful library crafted to streamline the management of computed properties within .NET applications, particularly tailored for Entity Framework Core projects. By leveraging Computed Expression, you can effortlessly define computed properties using lambda expressions, ensuring their automatic updating whenever the underlying data changes.

## Getting Started

Install nuget package
```sh
dotnet add package LLL.ComputedExpression.EFCore
```

Next, define computed properties in your EF Core mappings. Here's a demonstration:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    var personBuilder = modelBuilder.Entity<Person>();
    personBuilder.ComputedProperty(p => p.FullName, p => p.FirstName + " " + p.LastName);
    personBuilder.ComputedProperty(p => p.NumberOfCats, p => p.Pets.Count(x => x.Type == "Cat"));
    personBuilder.ComputedProperty(p => p.HasCats, p => p.Pets.Any(x => x.Type == "Cat"));
    personBuilder.ComputedProperty(p => p.Description, p => p.FullName + " (" + p.Pets.Count() + " pets)");
}
```

That's it! Now, all defined computed properties will be automatically updated during `dbContext.SaveChanges()`.

Check [PersonDbContext](test/LLL.ComputedExpression.EFCore.Tests/PersonDbContext.cs) for a complete example.

## How it works

This library operates by meticulously analyzing computed expressions and tracking all referenced data within them. It then traverses inverse navigations to pinpoint all entities that could be affected by changes to the referenced data.

### Basic example
Consider the following basic example:
```csharp
personBuilder.ComputedProperty(p => p.FullName, p => p.FirstName + " " + p.LastName);
```
In this scenario, the following data will be monitored for changes:
- Person's FirstName property.
- Person's LastName property.

Whenever a change occurs in any of these monitored properties, the FullName property of that person will be automatically updated with the result of the computed expression.

### Navigation example
Let's explore a navigation example:
```csharp
personBuilder.ComputedProperty(p => p.NumberOfCats, p => p.Pets.Count(x => x.Type == "Cat"));
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
