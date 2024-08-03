# EF Core Auto Compute ![CI](https://github.com/lucaslorentz/auto-compute/workflows/CI/badge.svg) [![Coverage Status](https://coveralls.io/repos/github/lucaslorentz/auto-compute/badge.svg)](https://coveralls.io/github/lucaslorentz/auto-compute)

Auto Compute is a library designed to automatically maintain derived data within .NET EF Core applications.

Whether you're implementing denormalization strategies or handling any other form of derived data in your EF Core projects, Auto Compute offers a straightforward solution. It tracks changes across related entities, automatically updating derived properties, thereby reducing complexity, maintenance overhead, and ensuring consistency throughout your application.

This library is an automatic implementation of the approach described by Microsoft in: https://learn.microsoft.com/en-us/ef/core/performance/modeling-for-performance#update-cache-columns-when-inputs-change

## Getting Started

Install nuget package
```sh
dotnet add package LLL.AutoCompute.EFCore
```

Enable by calling UseAutoCompute from DbContext OnConfiguring:
```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    => optionsBuilder.UseAutoCompute();
```
Or while adding DbContext to service collection:
```
services.AddDbContext<PersonDbContext>(
    b => b.UseAutoCompute());
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
    personBuilder.ComputedProperty(p => p.NumberOfCats, person => person.Pets.Count(pet => pet.Type == "Cat"), static c => c.NumberIncremental());
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

### Basic scenario

For a basic scenario like this:
```csharp
personBuilder.ComputedProperty(person => person.FullName, person => person.FirstName + " " + person.LastName);
```
The FullName property will be automatically updated whenever:
- Person's FirstName property changes.
- Person's LastName property changes.

### Complex scenario

For a more complex scenario:
```csharp
personBuilder.ComputedProperty(person => person.NumberOfCats, person => person.Pets.Count(pet => pet.Type == "Cat"));
```
The NumberOfCats property will be automatically updated whenever:
- Person's Pets collection change (add, remove, inverse add, inverse remove)
- Pet's Type property changes

### Incremental computation

Incremental computations calculate the incremental change to a value **without fully loading accessed collections!**

A scenario like:
```
personBuilder.ComputedProperty(person => person.NumberOfCats, person => person.Pets.Count(pet => pet.Type == "Cat"), static c => c.NumberIncremental());
```
Will return the incremental change to NumberOfCats for each Person without loading all Pets from that person. That's achieved by computing the original and current computed value using only the affected collection items and then calculating the delta.

## Mapping features

### Computed properties

Computed properties are updated by doing a **full evaluation** of the expression whenever any used data changes.

Example:
```csharp
personBuilder.ComputedProperty(person => person.NumberOfCats, person => person.Pets.Count(pet => pet.Type == "Cat"));
```
In this example, all pets from all affected persons will be lazy-loaded during re-evaluation.

### Incremental computed properties

Incremental computed properties are updated **without fully loading accessed collections!** The incremental change is computed loading only necessary items from collections, and then it is added to the previously computed value.

Example:
```csharp
personBuilder.ComputedProperty(p => p.NumberOfCats, b => b.AddCollection(person => person.Pets.Count(pet => pet.Type == "Cat"), static c => c.NumberIncremental());
```
In this example, NumberOfCats is incremented/decremented based on changes to Pets collection or to Pet's Type property, without loading all pets from affected persons.

## DbContext extension methods

The following DbContext methods are also available for unmapped scenarios:
- **GetChangesAsync**: Given a computed expression, it returns the root entities affected and their computed value changes. This also supports returning incremental computed changes. This method always returns the changes between unmodified DBContext and its current state.
- **GetChangesProviderAsync**: RhangesAsync, but returns a change provider with **GetChangesAsync** method in it. Each time **GetChangesAsync** is called, it returns changes between the last time it was called and the current DBContext state.

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
