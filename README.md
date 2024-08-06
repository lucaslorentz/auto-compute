# EF Core Auto Compute ![CI](https://github.com/lucaslorentz/auto-compute/workflows/CI/badge.svg) [![Coverage Status](https://coveralls.io/repos/github/lucaslorentz/auto-compute/badge.svg)](https://coveralls.io/github/lucaslorentz/auto-compute)

Persisted computed properties in EF Core that update automatically on save changes.

This library is an automatic implementation of the approach described by Microsoft in: https://learn.microsoft.com/en-us/ef/core/performance/modeling-for-performance#update-cache-columns-when-inputs-change

## Getting Started

1. Install nuget package [LLL.AutoCompute.EFCore](https://www.nuget.org/packages/LLL.AutoCompute.EFCore)

2. Enable AutoCompute in your DbContext by adding: `dbContextOptions.UseAutoCompute()`

3. Define computed properties in your EF Core mappings.
    ```csharp
    modelBuilder.Entity<Person>().ComputedProperty(
        p => p.NumberOfCats,
        p => p.Pets.Count(pet => pet.Type == "Cat"));
    ```

4. That's it! The computed property will update automatically during `dbContext.SaveChanges()`.

## How it works

**Auto Compute** analyzes computed expressions and monitors changes to all referenced data. When some change happens, it traverses inverse navigations to load all entities that could have been affected and update their computed properties.

## Mapping features

### Computed properties

Computed properties are updated by doing a **full evaluation** of the expression whenever any used data changes.

Example:
```csharp
personBuilder.ComputedProperty(
    p => p.NumberOfCats,
    p => p.Pets.Count(pet => pet.Type == "Cat"));
```
In this example, all pets from all affected persons will be pre-loaded before computation.

### Incremental computed properties

Incremental computed properties are updated **without fully loading accessed collections!** The incremental change is computed loading only necessary items from collections, and then it is added to the previously computed value.

Example:
```csharp
personBuilder.ComputedProperty(
    p => p.NumberOfCats,
    p => p.Pets.Count(pet => pet.Type == "Cat"),
    static c => c.NumberIncremental());
```
In this example, NumberOfCats is incremented/decremented based on changes to Pets collection or to Pet's Type property, without loading all pets from affected persons.

## DbContext extension methods

The following DbContext methods are also available for unmapped scenarios:
- **GetChangesAsync**: Given a computed expression, it returns the root entities affected and their computed value changes. This also supports returning incremental computed changes. This method always returns the changes between unmodified DBContext and its current state.
- **GetChangesProviderAsync**: Returns a change provider with **GetChangesAsync** method in it. Each time **GetChangesAsync** is called, it returns changes between the last time it was called and the current DBContext state.

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
