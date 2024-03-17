# Computed Expression ![CI](https://github.com/lucaslorentz/computed-expression/workflows/CI/badge.svg) [![Coverage Status](https://coveralls.io/repos/github/lucaslorentz/computed-expression/badge.svg?branch=main)](https://coveralls.io/github/lucaslorentz/computed-expression?branch=main)

Computed Expression is a library designed to simplify the management of computed properties in .NET applications, particularly for Entity Framework Core projects. With Computed Expression, you can define computed properties using lambda expressions and ensure they are automatically updated whenever underlying data changes.

## Getting Started

To start using Computed Expression in your project, simply install the desired NuGet package:
```sh
dotnet add package LLL.ComputedExpression.EFCore
```

Once installed, you can define computed properties in your EF Core DbContext using lambda expressions, as demonstrated below:
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

That's it, now all defined computed properties will be automatically updated during dbContext saveChanges.

## How it works

This library operates by analyzing computed expressions and tracking all data referenced within them. It then traverses inverse navigations to identify all entities that could be affected by changes to the referenced data.

### Basic example
```csharp
personBuilder.ComputedProperty(p => p.FullName, p => p.FirstName + " " + p.LastName);
```
In this example, the following data will be monitored for changes:

- Person's FirstName property.
- Person's LastName property.

Whenever a change occurs in any of these monitored properties, the FullName property of that person will be automatically updated with the result of the computed expression.

### Navigation example
```csharp
personBuilder.ComputedProperty(p => p.NumberOfCats, p => p.Pets.Count(x => x.Type == "Cat"));
```
In this example, the library will monitor changes in the following data:

- Person's Pets collection, including changes made to the inverse reference in the Pets entities.
- Pet's Type property.

Whenever a change occurs in any of these monitored properties, the NumberOfCats property of that person will be automatically updated with the result of the computed expression.

For changes in the Type property of Pets, the library automatically traverses the inverse navigation of Person.Pets (i.e., Pets.Owner) to identify which Persons could be affected by changes in the Type of a Pet.

### Complex example

Consider the following scenario where the logic becomes more intricate:
```csharp
personBuilder.ComputedProperty(
  person => person.NumberOfBlackKittens,
  person => person.Pets.Where(pet => pet.Type == "Cat")
    .SelectMany(pet => pet.Offsprings)
    .Where(pet => pet.Color == "Black")
    .Count()
);
```
In this example, the library also monitors changes to the pets' Color property. Whenever a change occurs, it identifies the affected pets by traversing the Offspring's inverse navigation. Subsequently, it determines the affected persons by traversing the Pets' inverse navigation.

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
