# ⚡ EF Core Auto Compute ![CI](https://github.com/lucaslorentz/auto-compute/workflows/CI/badge.svg) [![NuGet](https://img.shields.io/nuget/v/LLL.AutoCompute.EFCore.svg)](https://www.nuget.org/packages/LLL.AutoCompute.EFCore) [![Coverage Status](https://coveralls.io/repos/github/lucaslorentz/auto-compute/badge.svg?branch=main)](https://coveralls.io/github/lucaslorentz/auto-compute)

**Automatically update persisted computed properties in EF Core—so you don’t have to!**

---

## Table of Contents
- [Why Use Auto Compute?](#why-use-auto-compute)
- [Features](#features)
- [Getting Started](#getting-started)
- [How It Works](#how-it-works)
- [Explorer UI](#explorer-ui)
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

**Example Usage** - Add a new cat or modify existing pets to trigger recomputation:
```csharp
var person = dbContext.People.First();
person.Pets.Add(new Pet { Type = PetType.Cat }); // Add to navigation collection
dbContext.SaveChanges(); // NumberOfCats is automatically recalculated!
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

**Example Usage** - Add a new interaction or modify existing interactions to trigger recomputation:
```csharp
dbContext.Interactions.Add(new Interaction { 
   Post = post,
   Type = InteractionType.Like 
});
dbContext.SaveChanges(); // Post LikeCount updates via lightweight increment
```

### 🕒 Deferred Updates For Unloaded Entities
Use this when a small change may affect many related records.

Example: changing a product price or a shipping rule can impact thousands of orders.

`SetChangePropagationTarget(ChangePropagationTarget.LoadedEntities)` keeps `SaveChanges()` fast:
- entities already loaded in the current `DbContext` are updated during `SaveChanges`;
- entities not loaded are deferred (you can process them later with your async/queue flow).

This gives immediate consistency for what the user is editing, without a large synchronous fan-out.

```csharp
modelBuilder.Entity<Order>()
    .Navigation(o => o.Items)
    .SetChangePropagationTarget(ChangePropagationTarget.LoadedEntities);
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

## Explorer UI

A **web-based dashboard** for inspecting your Auto Compute setup at runtime — browse entity schemas, check consistency, fix stale data, and visualize change propagation flows.

### Setup

1. **Install the package:**
    ```
    dotnet add package LLL.AutoCompute.EFCore.Explorer
    ```

2. **Register services:**
    ```csharp
    services.AddAutoComputeExplorer();
    ```

3. **Map the endpoints:**
    ```csharp
    app.MapAutoComputeExplorer<YourDbContext>("/auto-compute-explorer");
    ```

4. **Navigate to** `/auto-compute-explorer` in your browser.

### What's included

- **Entity Catalog** — lists all entities with links to their schema and data.
- **Schema Inspector** — view properties (with type, PK, shadow flags), navigations, methods, and observers for each entity. Computed members show their LINQ expression and full dependency chain.
- **Data Browser** — paginated table with column selection, text search, per-column sorting, dynamic filters (=, >=, <=, >, <), and inconsistency filtering by date range.
- **Entity Detail View** — inspect a single entity instance with all property, computed, and method values; fix individual or all computed members.
- **Consistency Dashboard** — check consistency of any computed member (with optional "since" date), see consistent/inconsistent/total counts, and bulk-fix inconsistencies.
- **Computed Members & Observers List** — cross-entity view of all computed members and observers with their dependencies and consistency status.
- **Entity Context Graph** — interactive node-link diagram (powered by React Flow + dagre) showing how changes propagate through entity relationships. Exportable as PNG/SVG.

### Options

You can customize which methods appear in the schema via `MethodFilter`:

```csharp
services.AddAutoComputeExplorer(options =>
{
    options.MethodFilter = m =>
        m.IsPublic
        && !m.IsStatic
        && m.GetParameters().Length == 0
        && !m.IsSpecialName;
});
```

The default filter already includes only public, non-static, parameterless, non-special methods.

## Roadmap

- [x] Computed properties, collections and references
- [x] Incremental computed properties
   - [x] Incremental calculation
   - [x] Load all navigation items to evaluate Linq All/Any/Contains
   - [x] Load necessary navigation items to evaluate Linq Distinct
- [x] Computed observers
- [x] Methods to query inconsistent entities
- [x] Methods to fix inconsistent entities
- [x] Web-based UI for schema introspection, consistency check and fix
- [ ] Async queue-based update for hot computed properties
  - [ ] Throttled update - Update X seconds after the first change
  - [ ] Debounced update - Update X seconds after the last change
- [ ] Periodic consistency check and fixes

## License
MIT licensed. See [LICENSE](LICENSE) for details.
