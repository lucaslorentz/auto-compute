#pragma warning disable EF1001

using System.Linq.Expressions;
using System.Reflection;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Update.Internal;

namespace LLL.AutoCompute.EFCore.Internal;

public class AutoComputeMigrationsModelDiffer(
    IRelationalTypeMappingSource typeMappingSource,
    IMigrationsAnnotationProvider migrationsAnnotationProvider,
#if NET9_0_OR_GREATER
    IRelationalAnnotationProvider relationalAnnotationProvider,
#endif
    IRowIdentityMapFactory rowIdentityMapFactory,
    CommandBatchPreparerDependencies commandBatchPreparerDependencies,
    ICurrentDbContext currentDbContext
) : MigrationsModelDiffer(
    typeMappingSource,
    migrationsAnnotationProvider,
#if NET9_0_OR_GREATER
    relationalAnnotationProvider,
#endif
    rowIdentityMapFactory,
    commandBatchPreparerDependencies)
{
    public override IReadOnlyList<MigrationOperation> GetDifferences(
        IRelationalModel? source, IRelationalModel? target)
    {
        var operations = base.GetDifferences(source, target).ToList();

        if (target == null || DesignTimeComputedStore.Factories == null || DesignTimeComputedStore.Factories.Count == 0)
            return operations;

        var dbContext = currentDbContext.Context;

        // Create computed members from stored factories (model is finalized now)
        var analyzer = dbContext.Model.GetComputedExpressionAnalyzerOrThrow();
        var computedMembers = new List<(ComputedMember Member, IProperty Property)>();
        foreach (var (property, factory) in DesignTimeComputedStore.Factories)
        {
            try
            {
                var computed = factory(analyzer, property);
                computedMembers.Add((computed, property));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AutoComputeMigrationsModelDiffer] Failed to create computed member for {property.DeclaringType.ShortName()}.{property.Name}: {ex.Message}");
            }
        }

        foreach (var (computed, property) in computedMembers)
        {
            var newHash = property.FindAnnotation(AutoComputeAnnotationNames.Hash)?.Value as string;
            if (newHash == null)
                continue;

            var (oldEntityExists, oldPropertyExists, oldHash) = FindOldHash(source, property);
            if (newHash == oldHash)
                continue;

            // Property exists in old snapshot but has no hash — first time tracking, assume consistent
            if (oldPropertyExists && oldHash == null)
                continue;

            // Entity type is new — no existing data to backfill
            if (!oldEntityExists)
                continue;

            try
            {
                var sql = CaptureExecuteUpdateSql(dbContext, computed, property);
                operations.Add(new SqlOperation { Sql = sql, SuppressTransaction = true });
            }
            catch (Exception ex)
            {
                var inner = ex is TargetInvocationException { InnerException: { } ie } ? ie : ex;
                var reason = inner.Message.Contains("could not be translated")
                    ? "expression uses navigation properties not supported by ExecuteUpdate"
                    : inner.Message;
                operations.Add(new SqlOperation
                {
                    Sql = $"-- AUTO-COMPUTE BACKFILL: {property.DeclaringType.ShortName()}.{property.Name}\n" +
                          $"-- Could not auto-generate UPDATE SQL: {reason}\n" +
                          $"-- Please add the UPDATE SQL manually."
                });
            }
        }

        // Cleanup static state
        DesignTimeComputedStore.Factories = null;

        return operations;
    }

    private static (bool EntityExists, bool PropertyExists, string? Hash) FindOldHash(IRelationalModel? source, IProperty property)
    {
        if (source == null)
            return (false, false, null);

        var oldEntityType = source.Model.FindEntityType(property.DeclaringType.Name);
        if (oldEntityType == null)
            return (false, false, null);

        var oldProperty = oldEntityType.FindProperty(property.Name);
        if (oldProperty == null)
            return (true, false, null);

        return (true, true, oldProperty.FindAnnotation(AutoComputeAnnotationNames.Hash)?.Value as string);
    }

    private static readonly MethodInfo _captureHelperMethod = typeof(AutoComputeMigrationsModelDiffer)
        .GetMethod(nameof(CaptureHelper), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static string CaptureExecuteUpdateSql(
        DbContext dbContext,
        ComputedMember computed,
        IProperty property)
    {
        var method = _captureHelperMethod.MakeGenericMethod(property.DeclaringType.ClrType, property.ClrType);
        return (string)method.Invoke(null, new object[] { dbContext, computed, property })!;
    }

    private static string CaptureHelper<TEntity, TProperty>(
        DbContext dbContext,
        ComputedMember computed,
        IProperty property) where TEntity : class
    {
        // 1. Prepare expression for database translation
        var preparedExpr = (Expression<Func<TEntity, TProperty>>)dbContext.PrepareComputedExpressionForDatabase(
            computed.ChangesProvider.Expression);

        // 2. Build property selector: e => e.PropertyName
        var eParam = Expression.Parameter(typeof(TEntity), "e");
        Expression propAccess;
        if (property.PropertyInfo != null)
            propAccess = Expression.Property(eParam, property.PropertyInfo);
        else
        {
            var efPropertyMethod = typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(typeof(TProperty));
            propAccess = Expression.Call(null, efPropertyMethod, eParam, Expression.Constant(property.Name));
        }
        var propSelector = Expression.Lambda<Func<TEntity, TProperty>>(propAccess, eParam);

        // 3. Build SetPropertyCalls expression: s => s.SetProperty(e => e.Prop, computedExpr)
        //    SetProperty takes Func<> params. LambdaExpression.Type returns the delegate type
        //    (e.g. Func<T,P>), so passing lambdas directly to Expression.Call works.
        var sParam = Expression.Parameter(typeof(SetPropertyCalls<TEntity>), "s");

        var setPropertyMethod = typeof(SetPropertyCalls<TEntity>)
            .GetMethods()
            .First(m => m.Name == "SetProperty"
                && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 2
                && m.GetParameters()[1].ParameterType.IsGenericType
                && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
            .MakeGenericMethod(typeof(TProperty));

        var setPropertyCall = Expression.Call(
            sParam,
            setPropertyMethod,
            propSelector,
            preparedExpr);

        var setPropertyCallsExpr = Expression.Lambda<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>>(
            setPropertyCall, sParam);

        // 4. Capture SQL via interceptor
        using (SqlCaptureInterceptor.StartCapture())
        {
            dbContext.Set<TEntity>().ExecuteUpdate(setPropertyCallsExpr);
        }

        return SqlCaptureInterceptor.CapturedSql
            ?? throw new InvalidOperationException(
                "Failed to capture SQL. Ensure the database is running during migration generation.");
    }
}
