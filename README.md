# EF Core Auto Compute ![CI](https://github.com/lucaslorentz/auto-compute/workflows/CI/badge.svg) [![Coverage Status](https://coveralls.io/repos/github/lucaslorentz/auto-compute/badge.svg)](https://coveralls.io/github/lucaslorentz/auto-compute)

**Automatically update persisted computed properties in EF Core on save changes!**

This library provides an automated implementation of the approach described by Microsoft:  
[Modeling for Performance > Update cache columns when inputs change](https://learn.microsoft.com/en-us/ef/core/performance/modeling-for-performance#update-cache-columns-when-inputs-change).

## Getting Started

1. **Install the NuGet package:**  
   [LLL.AutoCompute.EFCore](https://www.nuget.org/packages/LLL.AutoCompute.EFCore)

2. **Enable AutoCompute in your `DbContext`:**  
   ```csharp
   dbContextOptions.UseAutoCompute();
   ```

3. **Define computed properties in your EF Core mappings:**
    ```csharp
    modelBuilder.Entity<Person>().ComputedProperty(
        p => p.NumberOfCats,
        p => p.Pets.Count(pet => pet.Type == PetType.Cat));
    ```

4. **Done!** Computed properties will now update automatically during `dbContext.SaveChanges()`.

## How It Works

**Auto Compute** analyzes computed expressions and tracks all referenced data. When any of the referenced data changes, it traverses inverse navigations to identify affected entities and updates their computed properties accordingly.

## Mapping Features

### Computed Properties

Computed properties are fully recalculated whenever any referenced data changes.

**Example:**
```csharp
personBuilder.ComputedProperty(
    p => p.NumberOfCats,
    p => p.Pets.Count(pet => pet.Type == PetType.Cat));
```

**Alternative Syntax:**
```csharp
personBuilder.Property(p => p.NumberOfCats)
    .AutoCompute((Person p) => p.Pets.Count(x => x.Type == PetType.Cat));
```

**Note:** In this example, all pets for the affected `Person` entities are pre-loaded before computation.

### Incrementally Computed Properties

Incrementally computed properties update without fully loading collections. The library computes changes incrementally and adjusts the existing value accordingly.

**Example:**
```csharp
personBuilder.ComputedProperty(
    p => p.NumberOfCats,
    p => p.Pets.Count(pet => pet.Type == PetType.Cat),
    static c => c.NumberIncremental());
```

**Alternative Syntax:**
```csharp
personBuilder.Property(p => p.NumberOfCats)
    .AutoCompute((Person p) => p.Pets.Count(x => x.Type == PetType.Cat),
        static c => c.NumberIncremental());
```

**Note:** In this example, `NumberOfCats` is updated incrementally based on changes to the `Pets` collection or the `Pet.Type` property, without loading all pets for the affected `Person`.

## Computed Navigations

Computed navigations update reference or collection navigation properties.

**Example:**
```csharp
orderBuilder.ComputedNavigation(
    e => e.Items,
    e => e.CloneFrom != null
        ? e.CloneFrom.Items.Select(i => new OrderItem
        {
            Product = i.Product,
            Quantity = i.Quantity
        }).ToArray()
        : e.AsComputedUntracked().Items,
    c => c.CurrentValue(),
    c => c.ReuseItemsByKey(e => new { e.Product }));
```

**Alternative Syntax:**
```csharp
orderBuilder.Navigation(e => e.Items)
    .AutoCompute(e => e.CloneFrom != null
        ? e.CloneFrom.Items.Select(i => new OrderItem
        {
            Product = i.Product,
            Quantity = i.Quantity
        }).ToArray()
        : e.AsComputedUntracked().Items,
    c => c.CurrentValue(),
    c => c.ReuseItemsByKey(e => new { e.Product }));
```

**Key Details:**
- Updates are based on changes to the items the order was cloned from.
- Items are reused if their key (e.g., `Product`) matches, and only the specified properties (e.g., `Product` and `Quantity`) are updated.

## Computed Observers

Computed observers allow you to define callbacks that react to computed changes. These callbacks support both normal and incremental change calculations.

Example:
```csharp
personBuilder.ComputedObserver(
    p => p.Pets.Count(x => x.Type == PetType.Cat),
    null, // Filter
    c => c.CurrentValue(),
    async (person, numberOfCats) =>
    {
        Console.WriteLine($"Person {person.FullName} now has {numberOfCats} cats.");
    });
```

Alternatively you can get an event object with all changes, having access to other services as well:
```csharp
personBuilder.ComputedObserver(
    p => p.Pets.Count(x => x.Type == PetType.Cat),
    null, // Filter
    c => c.NumberIncremental(),
    async (e) =>
    {
        var personNotifier = e.DbContext.GetService<IPersonNotifier>();
        foreach (var (person, catsIncrement) in e.Changes) {
            if (catsIncrement <= 0)
                continue;

            personNotifier.SendMessage(person, $"Congrats on your new {catsIncrement} cats!");
        }
    });
```

**Important:** Observers are triggered only after `dbContext.SaveChanges()` completes. If additional changes are made to entities in observers, you must call `SaveChanges()` again to save them.

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
