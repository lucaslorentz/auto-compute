# ⚡ EF Core Auto Compute ![CI](https://github.com/lucaslorentz/auto-compute/workflows/CI/badge.svg) [![NuGet](https://img.shields.io/nuget/v/LLL.AutoCompute.EFCore.svg)](https://www.nuget.org/packages/LLL.AutoCompute.EFCore) [![Coverage Status](https://coveralls.io/repos/github/lucaslorentz/auto-compute/badge.svg?branch=main)](https://coveralls.io/github/lucaslorentz/auto-compute)

**Automatically update persisted computed properties in EF Core—so you don’t have to!**

---

## Table of Contents
- [Why Use Auto Compute?](#why-use-auto-compute)
- [Features](#features)
- [Getting Started](#getting-started)
- [How It Works](#how-it-works)
- [Roadmap](#roadmap)
- [License](#license)

---

## Why Use Auto Compute?

Denormalizing data is a **powerful performance optimization**, but manually managing computed properties is error-prone and time-consuming. With EF Core Auto Compute:

- 🚀 **Boost Performance**  
  Eliminate costly joins and runtime calculations by persisting derived data.
- 🔄 **Stay Consistent**  
  Automatically keep computed values accurate as your data changes.
- 💡 **Write Less Code**  
  Define computed properties once—no more manual update logic!

## Features

### 🔧 Computed Properties
**Automatically recalculate** properties when dependencies change.  
*Ideal for aggregations like counts, sums, or complex expressions.*

```csharp
modelBuilder.Entity<Person>().ComputedProperty(
   p => p.NumberOfCats, // Property to map
   p => p.Pets.Count(pet => pet.Type == PetType.Cat)); // Computed expression
```

### ⚡ Incremental Updates
**Optimize performance** by updating values without loading entire collections.  
*Perfect for large collections.*

```csharp
modelBuilder.Entity<Post>().ComputedProperty(
   p => p.LikeCount, // Property to map
   p => p.Interactions.Count(i => i.Type == InteractionType.Like), // Computed expression
   c => c.NumberIncremental()); // Change calculation
```

### 🔗 Computed Navigations
**Auto-sync related** entities when their dependencies change.  
*Great for cloning relationships or maintaining derived collections.*

```csharp
modelBuilder.Entity<Order>().ComputedNavigation(
   o => o.Items, // Navigation to map
   o => o.CloneFrom != null
     ? o.CloneFrom.Items.Select(i => new OrderItem
     {
         Product = i.Product,
         Quantity = i.Quantity
     }).ToArray()
     : o.AsComputedUntracked().Items, // Computed expression
   c => c.CurrentValue(), // Change calculation
   c => c.ReuseItemsByKey(e => new { e.Product })); // Other options
```

### 👀 Computed Observers
**React to changes** with event-driven callbacks.  
*Notify users, log data, or trigger workflows when values update.*

```csharp
modelBuilder.Entity<Person>().ComputedObserver(
   p => p.Pets.Count(x => x.Type == PetType.Cat), // Observed expression
   p => p.IsActive, // Filter
   c => c.CurrentValue(), // Change calculation
   async (person, numberOfCats) =>
   {
     Console.WriteLine($"Person {person.FullName} now has {numberOfCats} cats.");
   }); // Callback
```

## Getting Started

1. **Install the Package:**  
    ```
    dotnet add package LLL.AutoCompute.EFCore
    ```

2. **Enable AutoCompute:**  
   ```csharp
   dbContextOptions.UseAutoCompute();
   ```

3. **Map computed properties:**
    ```csharp
    modelBuilder.Entity<Person>().ComputedProperty(
        p => p.NumberOfCats, // Property to map
        p => p.Pets.Count(pet => pet.Type == PetType.Cat)); // Computed expression
    ```

**Done!** Computed properties will now update automatically during `dbContext.SaveChanges()`.

## How It Works

**Auto Compute** analyzes computed expressions and tracks all referenced data. When any of the referenced data changes, it traverses inverse navigations to identify affected entities and updates their computed properties accordingly.

[Diagram](https://excalidraw.com/#json=fZqhU0GKni812toTdr2vZ,qkLdmgG9sw7w_24fgY9VOw)

## Roadmap

- [x] Computed properties, collections and references
- [x] Incremental computed properties
   - [x] Incremental calculation
   - [x] Load all navigation items to evaluate Linq All/Any/Contains
   - [x] Load necessary navigation items to evaluate Linq Distinct 
- [x] Computed observers
- [ ] Async queue-based update for hot computed properties
  - [ ] Throttled update - Update X seconds after the first change
  - [ ] Debounced update - Update X seconds after the last change
- [ ] Methods to query inconsistent entities
- [ ] Periodic consistency check and fixes
- [ ] Web-based UI for schema introspection, consistency check and fix

## License
MIT licensed. See [LICENSE](LICENSE) for details.
