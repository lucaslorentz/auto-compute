using System.Linq.Expressions;
using LLL.AutoCompute.ChangesProviders;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute;

/// <summary>
/// Non-generic interface for computed changes providers.
/// </summary>
public interface IComputedChangesProvider
{
    /// <summary>The expression used to compute values.</summary>
    LambdaExpression Expression { get; }

    /// <summary>The entity context that tracks affected entities and their navigations.</summary>
    EntityContext EntityContext { get; }

    /// <summary>The calculator used to determine changes between original and current values.</summary>
    IChangeCalculator ChangeCalculator { get; }
}

/// <summary>
/// Provides change detection for entities based on a computed expression.
/// </summary>
/// <typeparam name="TEntity">The entity type being observed.</typeparam>
/// <typeparam name="TChange">The change representation type.</typeparam>
public interface IComputedChangesProvider<TEntity, TChange>
    : IComputedChangesProvider
    where TEntity : class
{
    /// <inheritdoc/>
    IChangeCalculator IComputedChangesProvider.ChangeCalculator => ChangeCalculator;

    /// <summary>The typed calculator used to determine changes.</summary>
    new IChangeCalculator<TChange> ChangeCalculator { get; }

    /// <summary>
    /// Computes changes for all affected entities based on the current input state.
    /// </summary>
    /// <param name="input">The computed input containing the current state and context.</param>
    /// <param name="changeMemory">
    /// Optional memory for delta tracking. When provided, returns the difference between the 
    /// previously computed change and the current change, enabling incremental updates across 
    /// multiple calls. Only use when the observer calculates changes from a constant original 
    /// point (e.g., database-loaded values). Pass <c>null</c> for absolute changes.
    /// </param>
    /// <returns>A dictionary mapping affected entities to their computed changes.</returns>
    Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync(ComputedInput input, ChangeMemory<TEntity, TChange>? changeMemory);
}
