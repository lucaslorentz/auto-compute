# Computed Expression ![CI](https://github.com/lucaslorentz/computed-expression/workflows/CI/badge.svg) [![Coverage Status](https://coveralls.io/repos/github/lucaslorentz/computed-expression/badge.svg)](https://coveralls.io/github/lucaslorentz/computed-expression)

Computed Expression is a library designed to automatically maintain derived data within .NET EF Core applications.

Whether you're implementing denormalization strategies or handling any other form of derived data in your EF Core projects, Computed Expression offers a straightforward solution. It tracks changes across related entities, automatically updating derived properties, thereby reducing complexity, maintenance overhead, and ensuring consistency throughout your application.

This library is an automatic implementation of the approach described by Microsoft in: https://learn.microsoft.com/en-us/ef/core/performance/modeling-for-performance#update-cache-columns-when-inputs-change

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
    personBuilder.ComputedProperty(person => person.HasCats, person => person.Pets.Any(pet => pet.Type == "Cat"));
    personBuilder.ComputedProperty(person => person.Description, person => person.FullName + " (" + person.Pets.Count() + " pets)");
    personBuilder.IncrementalComputedProperty(p => p.NumberOfCats, b => b.AddCollection(person => person.Pets.Where(pet => pet.Type == "Cat"), pet => 1));
}
```

That's it! Now, all defined computed properties will be automatically updated during `dbContext.SaveChanges()`:
```csharp
var person = context.Persons.Find(1);
Console.WriteLine(person.HasCats); // Output: false
Console.WriteLine(person.NumberOfCats); // Output: 0

var pet = new Pet { Type = "Cat", Owner = person };
context.Add(pet);

await context.SaveChangesAsync();

Console.WriteLine(person.HasCats); // Output: true
Console.WriteLine(person.NumberOfCats); // Output: 1
```

Check the rest of the readme to understand more all features.

## How it works

This library operates by meticulously analyzing computed expressions and tracking all referenced data within them. It then traverses inverse navigations to pinpoint all root entities that could be affected by changes to the referenced data.

For this basic scenario:
```csharp
personBuilder.ComputedProperty(person => person.FullName, person => person.FirstName + " " + person.LastName);
```
The FullName property will be automatically updated whenever:
- Person's FirstName property changes.
- Person's LastName property changes.

For this more complex scenario:
```csharp
personBuilder.ComputedProperty(person => person.NumberOfCats, person => person.Pets.Count(pet => pet.Type == "Cat"));
```
The NumberOfCats property will be automatically updated whenever:
- Person's Pets collection change (add, remove, inverse collection add, inverse collection remove)
- Pet's Type property changes

## Mapping features

### Computed properties

Computed properties are updated by doing a **full evaluation** of the expression whenever any used data changes.

Example:
```
personBuilder.ComputedProperty(person => person.NumberOfCats, person => person.Pets.Count(pet => pet.Type == "Cat"));
```
In this example, all pets from all affected persons will be lazy-loaded during re-evaluation.

### Incremental computed properties

Incremental computed properties are updated by **adding the change** of its parts to the previously computed value whenever any used data changes.

Example:
```
personBuilder.IncrementalComputedProperty(p => p.NumberOfCats, b => b.AddCollection(person => person.Pets.Where(pet => pet.Type == "Cat"), pet => 1));
```
In this example, NumberOfCats is incremented/decremented based on changes to Pets collection or to Pet's Type property.

## DbContext features

The following DbContext methods are also available for unmapped scenarios:
- **GetAffectedEntitiesAsync**: Given a computed expression, it returns the root entities affected by the unsaved DbContext changes.
- **GetChangesAsync**: Given a computed expression, it returns the root entities affected by the unsaved DbContext changes with their respective original and current values.
- **GetIncrementalChanges**: Given an incremental computed definition, it returns the root entities affected by the unsaved DbContext changes and their respective incremental change.

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
