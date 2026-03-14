# CLAUDE.md - LLL.AutoCompute

## Project Overview

LLL.AutoCompute is an open-source library for automatically computing and updating properties in Entity Framework Core. It supports multi-targeting across .NET 8, 9, and 10.

## Build & Test

```bash
dotnet build
dotnet test
```

## Language

English for everything (code, commits, PRs, docs).

## Package Versioning

- **Library projects** (`src/`): Use base versions (`x.0.0`) for Microsoft.Extensions.* and Microsoft.EntityFrameworkCore.* packages to avoid forcing consumers to update to a specific patch version.
- **Test projects** (`test/`): Use latest stable patch versions since they don't ship to consumers.

## Multi-targeting

All projects target `net10.0;net9.0;net8.0`. Use conditional `ItemGroup` or `Condition` attributes for framework-specific package references.

## Git Conventions

- Commit messages in English, imperative present tense
- Main branch: `main`
