using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using LLL.AutoCompute.EFCore.Explorer.Models;
using LLL.AutoCompute.EFCore.Internal;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using LLL.AutoCompute.EntityContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Explorer;

public static class AutoComputeExplorerHelper
{
    private static readonly ConcurrentDictionary<string, Delegate> _compiledComputedsCache = new();
    private static readonly ConcurrentDictionary<string, Delegate> _compiledConsistencyCheckersCache = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new CustomClassToStringConverter() }
    };

    public static async Task<EntDetailsModel> MapEntityType(
        IEntityType entityType,
        DbContext dbContext,
        Func<MethodInfo, bool> methodFilter)
    {
        var mappedProperties = new List<EntPropertyModel>();
        foreach (var p in entityType.GetProperties())
            mappedProperties.Add(await MapProperty(p, dbContext));

        var mappedNavigations = new List<EntNavigationModel>();
        foreach (var n in entityType.GetNavigations())
            mappedNavigations.Add(await MapNavigation(n, dbContext));

        var mappedMethods = new List<EntMethodModel>();
        foreach (var m in entityType.ClrType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (methodFilter(m))
                mappedMethods.Add(MapMethod(m));
        }

        return new EntDetailsModel
        {
            Name = entityType.Name,
            Properties = [.. mappedProperties],
            Navigations = [.. mappedNavigations],
            Methods = [.. mappedMethods.OrderBy(m => m.Name)],
            Observers = [.. await Task.WhenAll((entityType.GetComputedObservers() ?? [])
                .Select(o => MapObserver(o, entityType.Name)))]
        };
    }

    public static async Task<EntPropertyModel> MapProperty(
        IProperty property,
        DbContext dbContext)
    {
        var key = (property.DeclaringType as IEntityType)?.FindKey(property);
        var computed = property.GetComputedMember();

        return new EntPropertyModel
        {
            Name = property.Name,
            IsPrimaryKey = key?.IsPrimaryKey() ?? false,
            IsShadow = property.IsShadowProperty(),
            ClrType = StringifyType(property.ClrType),
            EnumItems = GetEnumItems(property.ClrType),
            Computed = computed is not null
                ? await MapComputed(computed, dbContext)
                : null
        };
    }

    public static async Task<EntNavigationModel> MapNavigation(
        INavigationBase navigation,
        DbContext dbContext)
    {
        var computed = navigation.GetComputedMember();
        var foreignKey = (navigation as INavigation)?.ForeignKey;

        return new EntNavigationModel
        {
            Name = navigation.Name,
            IsCollection = navigation.IsCollection,
            TargetEntity = navigation.TargetEntityType.Name,
            FilterKey = foreignKey?.Properties.First().Name,
            Computed = computed is not null
                ? await MapComputed(computed, dbContext)
                : null
        };
    }

    public static EntMethodModel MapMethod(MethodInfo m)
    {
        return new EntMethodModel
        {
            Name = $"{m.Name}()",
            ClrType = StringifyType(m.ReturnType),
            EnumItems = GetEnumItems(m.ReturnType),
        };
    }

    public static Dictionary<string, EntEnumItemModel>? GetEnumItems(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType.IsEnum
            ? Enum.GetValues(underlyingType).OfType<Enum>().ToDictionary(v => v.ToString("D"), v => new EntEnumItemModel
            {
                Label = v.ToString()!,
                Value = v.ToString("D")
            })
            : null;
    }

    public static async Task<EntComputedModel> MapComputed(
        ComputedMember computed,
        DbContext dbContext)
    {
        var allEntitiesDependencies = computed.ChangesProvider.EntityContext
            .GetAllObservedMembers(propagationTargetFilter: ChangePropagationTarget.AllEntities)
            .OfType<EFCoreObservedMember>()
            .ToArray();
        var loadedEntitiesDependencies = computed.ChangesProvider.EntityContext
            .GetAllObservedMembers(propagationTargetFilter: ChangePropagationTarget.LoadedEntities)
            .OfType<EFCoreObservedMember>()
            .ToArray();

        return new EntComputedModel
        {
            Name = computed.ToDebugString(),
            Entity = computed.EntityType.Name,
            Member = computed.Property.Name,
            Expression = computed.ChangesProvider.Expression.ToString(),
            Dependencies = computed.ObservedMembers
                .Select(MapObservedMember)
                .OrderBy(m => m.EntityName)
                .ThenBy(m => m.MemberName)
                .ToArray(),
            AllEntitiesDependencies = allEntitiesDependencies
                .Select(MapObservedMember)
                .OrderBy(m => m.EntityName)
                .ThenBy(m => m.MemberName)
                .ToArray(),
            LoadedEntitiesDependencies = loadedEntitiesDependencies
                .Select(MapObservedMember)
                .OrderBy(m => m.EntityName)
                .ThenBy(m => m.MemberName)
                .ToArray(),
            EntityContextGraph = ToFlowGraph(computed.ChangesProvider.EntityContext),
        };
    }

    public static async Task<EntObserverModel> MapObserver(ComputedObserver computed, string entityName)
    {
        var allEntitiesDependencies = computed.ChangesProvider.EntityContext
            .GetAllObservedMembers(propagationTargetFilter: ChangePropagationTarget.AllEntities)
            .OfType<EFCoreObservedMember>()
            .ToArray();
        var loadedEntitiesDependencies = computed.ChangesProvider.EntityContext
            .GetAllObservedMembers(propagationTargetFilter: ChangePropagationTarget.LoadedEntities)
            .OfType<EFCoreObservedMember>()
            .ToArray();

        return new EntObserverModel
        {
            Name = computed.Name,
            Entity = entityName,
            Expression = computed.ChangesProvider.Expression.ToString(),
            Dependencies = computed.ObservedMembers
                .Select(MapObservedMember)
                .OrderBy(m => m.EntityName)
                .ThenBy(m => m.MemberName)
                .ToArray(),
            AllEntitiesDependencies = allEntitiesDependencies
                .Select(MapObservedMember)
                .OrderBy(m => m.EntityName)
                .ThenBy(m => m.MemberName)
                .ToArray(),
            LoadedEntitiesDependencies = loadedEntitiesDependencies
                .Select(MapObservedMember)
                .OrderBy(m => m.EntityName)
                .ThenBy(m => m.MemberName)
                .ToArray(),
            EntityContextGraph = ToFlowGraph(computed.ChangesProvider.EntityContext)
        };
    }

    public static EntObservedMemberModel MapObservedMember(EFCoreObservedMember observedMember)
    {
        return new EntObservedMemberModel
        {
            EntityName = observedMember.Member.DeclaringType.ContainingEntityType.Name,
            MemberName = observedMember.Member.Name
        };
    }

    public static FlowGraphModel ToFlowGraph(EntityContext entityContext)
    {
        var model = new FlowGraphModel
        {
            Nodes = [],
            Edges = []
        };

        AddToFlowGraph(model, entityContext, []);

        return model;
    }

    public static void AddToFlowGraph(FlowGraphModel model, EntityContext entityContext, HashSet<string> added)
    {
        var identifier = entityContext.Id.ToString();

        if (!added.Add(identifier))
            return;

        var nodeDetails = GetNodeDetails(entityContext);

        model.Nodes.Add(new FlowNodeModel
        {
            Id = identifier,
            Type = "dependency",
            Data = new FlowNodeDataModel
            {
                Label = entityContext.GetType().Name,
                EntityType = entityContext.EntityType.Name,
                Expression = nodeDetails,
                IsTrackingChanges = entityContext.IsTrackingChanges,
                PropagationTarget = entityContext.PropagationTarget.ToString(),
                CanResolveLoadedEntities = entityContext.CanResolveLoadedEntities,
                Observing = entityContext.ObservedMembers.Select(m => m.Name).ToList()
            }
        });

        foreach (var child in entityContext.ChildContexts)
        {
            AddToFlowGraph(model, child, added);

            if (added.Add($"{identifier}->{child.Id}"))
            {
                model.Edges.Add(new FlowEdgeModel
                {
                    Id = $"{identifier}->{child.Id}",
                    Source = identifier,
                    Target = child.Id.ToString(),
                    Label = GetNodeDetails(child)
                });
            }
        }

        if (entityContext is CompositeEntityContext c)
        {
            foreach (var parent in c.Parents)
            {
                AddToFlowGraph(model, parent, added);

                if (added.Add($"{parent.Id}->{identifier}"))
                {
                    model.Edges.Add(new FlowEdgeModel
                    {
                        Id = $"{parent.Id}->{identifier}",
                        Source = parent.Id.ToString(),
                        Target = identifier,
                        Label = GetNodeDetails(entityContext)
                    });
                }
            }
        }
    }

    public static string GetNodeDetails(EntityContext entityContext)
    {
        return entityContext.Expression switch
        {
            MemberExpression memberExp => $".{memberExp.Member.Name}",
            MethodCallExpression methodExp => methodExp.Method.DeclaringType is null
                || !methodExp.Method.IsStatic
                || methodExp.Method.IsDefined(typeof(ExtensionAttribute), true)
                ? $".{methodExp.Method.Name}()"
                : $"{methodExp.Method.DeclaringType?.Name}.{methodExp.Method.Name}()",
            ParameterExpression paramExp => $"Param {paramExp.Name}",
            _ => $"{entityContext.Expression.NodeType}"
        };
    }

    public static string StringifyType(Type type)
    {
        var nullableInnerType = Nullable.GetUnderlyingType(type);
        if (nullableInnerType is not null)
        {
            return $"Nullable<{StringifyType(nullableInnerType)}>";
        }

        return type.FullName!;
    }

    public static JsonElement SerializeValue(object? value)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, value?.GetType() ?? typeof(object), _jsonOptions);
        return JsonSerializer.Deserialize<JsonElement>(bytes);
    }

    public static async Task<EntDataModel> EntityToModel(
        DbContext dbContext,
        object entity,
        HashSet<string>? include,
        Func<MethodInfo, bool> methodFilter)
    {
        var entry = dbContext.Entry(entity);
        var entityType = entry.Metadata;

        var keyPropertyEntry = entry.Properties
            .FirstOrDefault(p => p.Metadata.IsKey());

        var model = new EntDataModel
        {
            Id = keyPropertyEntry?.CurrentValue!
        };

        foreach (var propertyEntry in entry.Properties)
        {
            var property = propertyEntry.Metadata;

            if (include is null || include.Contains(property.Name) || property.IsPrimaryKey())
            {
                model.PropertyValues[property.Name] = SerializeValue(propertyEntry.CurrentValue);

                var computedMember = property.GetComputedMember();
                if (computedMember is not null)
                {
                    var cacheKey = $"{entityType.Name}.{property.Name}";
                    var compiled = _compiledComputedsCache.GetOrAdd(cacheKey, _ =>
                    {
                        var lambda = computedMember.ChangesProvider.Expression;
                        return lambda.Compile();
                    });
                    try
                    {
                        model.ComputedValues[property.Name] = SerializeValue(compiled.DynamicInvoke(entity));
                    }
                    catch (Exception ex)
                    {
                        model.ComputedValues[property.Name] = SerializeValue($"Error computing: {ex.Message}");
                    }

                    var isConsistentLambda = _compiledConsistencyCheckersCache.GetOrAdd(cacheKey, _ =>
                    {
                        var lambda = computedMember.GetIsMemberConsistentLambda(dbContext);
                        return lambda.Compile();
                    });
                    try
                    {
                        model.MembersConsistency[property.Name] = SerializeValue(isConsistentLambda.DynamicInvoke(entity) is true);
                    }
                    catch (Exception ex)
                    {
                        model.MembersConsistency[property.Name] = SerializeValue($"Error checking consistency: {ex.Message}");
                    }
                }
            }
        }

        foreach (var referenceEntry in entry.References)
        {
            if (include is not null && !include.Contains(referenceEntry.Metadata.Name))
                continue;

            if (!referenceEntry.IsLoaded)
                await referenceEntry.LoadAsync();

            var targetEntry = referenceEntry.TargetEntry;
            if (targetEntry is not null)
            {
                var targetKeyPropertyEntry = targetEntry.Properties
                    .FirstOrDefault(p => p.Metadata.IsKey());

                if (targetKeyPropertyEntry is not null)
                {
                    model.ReferenceValues[referenceEntry.Metadata.Name] = new EntityReferenceModel
                    {
                        Id = targetKeyPropertyEntry.CurrentValue!,
                        ToStringValue = targetEntry.Entity.ToString()
                    };
                }
            }
        }

        foreach (var collectionEntry in entry.Collections)
        {
            if (include is not null && !include.Contains(collectionEntry.Metadata.Name))
                continue;

            if (collectionEntry.Metadata.IsShadowProperty())
                continue;

            var count = await collectionEntry.Query().OfType<object>().CountAsync();

            model.ReferenceValues[collectionEntry.Metadata.Name] = new EntityReferenceModel
            {
                Count = count
            };
        }

        foreach (var method in entityType.ClrType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (include is not null && include.Contains($"{method.Name}()") && methodFilter(method))
            {
                try
                {
                    model.MethodValues[$"{method.Name}()"] = SerializeValue(method.Invoke(entity, null));
                }
                catch (Exception ex)
                {
                    model.MethodValues[$"{method.Name}()"] = SerializeValue($"Error: {ex.Message}");
                }
            }
        }

        return model;
    }

    public static async Task<EntListModel> GetPaginatedList(
        DbContext dbContext,
        IEntityType entityType,
        IQueryable<object> query,
        string? pageToken,
        int pageSize,
        string? include,
        Dictionary<string, string> filters,
        string? search,
        string? sortBy,
        bool sortDescending,
        Func<MethodInfo, bool> methodFilter)
    {
        var method = typeof(AutoComputeExplorerHelper).GetMethod(nameof(GetPaginatedListInternal), BindingFlags.NonPublic | BindingFlags.Static)!;
        var genericMethod = method.MakeGenericMethod(entityType.ClrType);
        return await (Task<EntListModel>)genericMethod.Invoke(null, [dbContext, entityType, query, pageToken, pageSize, include, filters, search, sortBy, sortDescending, methodFilter])!;
    }

    private static async Task<EntListModel> GetPaginatedListInternal<T>(
        DbContext dbContext,
        IEntityType entityType,
        IQueryable<object> query,
        string? pageToken,
        int pageSize,
        string? include,
        Dictionary<string, string> filters,
        string? search,
        string? sortBy,
        bool sortDescending,
        Func<MethodInfo, bool> methodFilter)
        where T : class
    {
        var typedQuery = query.OfType<T>();

        var parameter = Expression.Parameter(typeof(T), "x");

        if (!string.IsNullOrEmpty(search))
        {
            var containsMethod = typeof(string).GetMethod("Contains", [typeof(string)])!;
            Expression? searchExpression = null;

            foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(string)))
            {
                var propertyAccess = Expression.Call(null,
                    typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(typeof(string)),
                    parameter, Expression.Constant(property.Name));

                var containsCall = Expression.Call(propertyAccess, containsMethod, Expression.Constant(search));

                if (searchExpression == null)
                    searchExpression = containsCall;
                else
                    searchExpression = Expression.OrElse(searchExpression, containsCall);
            }

            if (searchExpression != null)
            {
                var searchLambda = Expression.Lambda<Func<T, bool>>(searchExpression, parameter);
                typedQuery = typedQuery.Where(searchLambda);
            }
        }

        var keyPropertyMetadata = entityType.FindPrimaryKey()?.Properties.First();
        var keyPropertyName = keyPropertyMetadata?.Name ?? "Id";
        var keyPropertyType = keyPropertyMetadata?.ClrType ?? typeof(object);

        var efPropertyMethod = typeof(EF).GetMethods()
            .Single(m => m.Name == nameof(EF.Property) && m.IsGenericMethod && m.GetParameters().Length == 2)
            .MakeGenericMethod(keyPropertyType);
        var efPropertyCall = Expression.Call(null, efPropertyMethod, parameter, Expression.Constant(keyPropertyName));

        foreach (var filter in filters)
        {
            var filterKey = filter.Key;
            var op = "eq";
            if (filterKey.EndsWith("_gte")) { op = "gte"; filterKey = filterKey[..^4]; }
            else if (filterKey.EndsWith("_lte")) { op = "lte"; filterKey = filterKey[..^4]; }
            else if (filterKey.EndsWith("_gt")) { op = "gt"; filterKey = filterKey[..^3]; }
            else if (filterKey.EndsWith("_lt")) { op = "lt"; filterKey = filterKey[..^3]; }

            var property = entityType.FindProperty(filterKey);
            var navigation = entityType.FindNavigation(filterKey);
            if (property == null && navigation == null) continue;

            var filterValue = filter.Value;
            if (string.IsNullOrEmpty(filterValue)) continue;

            var targetProperty = property;

            if (navigation != null)
                targetProperty = navigation.ForeignKey.Properties.FirstOrDefault();

            if (targetProperty == null) continue;

            var propertyType = targetProperty.ClrType;
            var genericEfPropertyMethod = typeof(EF).GetMethods()
                .Single(m => m.Name == nameof(EF.Property) && m.IsGenericMethod && m.GetParameters().Length == 2)
                .MakeGenericMethod(propertyType);
            var propertyAccess = Expression.Call(null, genericEfPropertyMethod, parameter, Expression.Constant(targetProperty.Name));

            Expression? comparison = null;
            if (propertyType == typeof(string))
            {
                var containsMethod = typeof(string).GetMethod("Contains", [typeof(string)])!;
                comparison = Expression.Call(propertyAccess, containsMethod, Expression.Constant(filterValue));
            }
            else
            {
                try
                {
                    var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
                    object convertedValue;

                    if (targetType == typeof(DateOnly))
                    {
                        var datePart = filterValue.Contains('T') ? filterValue.Split('T')[0] : filterValue;
                        convertedValue = DateOnly.Parse(datePart);
                    }
                    else if (targetType == typeof(Guid))
                    {
                        convertedValue = Guid.Parse(filterValue);
                    }
                    else if (targetType == typeof(DateTime) && filterValue.Contains('T'))
                    {
                        convertedValue = DateTime.Parse(filterValue, null, System.Globalization.DateTimeStyles.RoundtripKind);
                    }
                    else if (targetType.IsEnum)
                    {
                        convertedValue = Enum.Parse(targetType, filterValue);
                    }
                    else
                    {
                        convertedValue = Convert.ChangeType(filterValue, targetType);
                    }

                    var constant = Expression.Constant(convertedValue, propertyType);

                    comparison = op switch
                    {
                        "gte" => Expression.GreaterThanOrEqual(propertyAccess, constant),
                        "lte" => Expression.LessThanOrEqual(propertyAccess, constant),
                        "gt" => Expression.GreaterThan(propertyAccess, constant),
                        "lt" => Expression.LessThan(propertyAccess, constant),
                        _ => Expression.Equal(propertyAccess, constant)
                    };
                }
                catch { /* Ignore invalid formats */ }
            }

            if (comparison != null)
            {
                var filterLambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
                typedQuery = typedQuery.Where(filterLambda);
            }
        }

        if (!string.IsNullOrEmpty(pageToken))
        {
            var tokenValue = Convert.ChangeType(pageToken, keyPropertyType);
            var constant = Expression.Constant(tokenValue, keyPropertyType);
            var body = Expression.GreaterThan(efPropertyCall, constant);
            var whereLambda = Expression.Lambda<Func<T, bool>>(body, parameter);
            typedQuery = typedQuery.Where(whereLambda);
        }

        var sortProperty = !string.IsNullOrEmpty(sortBy) ? (entityType.FindProperty(sortBy) ?? entityType.FindPrimaryKey()?.Properties.First()) : entityType.FindPrimaryKey()?.Properties.First();
        var sortPropertyName = sortProperty?.Name ?? keyPropertyName;
        var sortPropertyType = sortProperty?.ClrType ?? keyPropertyType;

        var sortEfPropertyMethod = typeof(EF).GetMethods()
            .Single(m => m.Name == nameof(EF.Property) && m.IsGenericMethod && m.GetParameters().Length == 2)
            .MakeGenericMethod(sortPropertyType);
        var sortPropertyCall = Expression.Call(null, sortEfPropertyMethod, parameter, Expression.Constant(sortPropertyName));

        var orderByMethodName = sortDescending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy);
        var orderByMethod = typeof(Queryable).GetMethods()
            .Single(m => m.Name == orderByMethodName && m.IsGenericMethodDefinition && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), sortPropertyType);

        var orderByLambda = Expression.Lambda(sortPropertyCall, parameter);
        var orderedQuery = (IQueryable<T>)orderByMethod.Invoke(null, [typedQuery, orderByLambda])!;

        var items = await orderedQuery.Take(pageSize + 1).ToListAsync();

        var hasNextPage = items.Count > pageSize;
        if (hasNextPage)
            items.RemoveAt(items.Count - 1);

        var parsedInclude = include?.Split(",").ToHashSet();

        var entitiesModels = new List<EntDataModel>();
        foreach (var e in items)
            entitiesModels.Add(await EntityToModel(dbContext, e, parsedInclude, methodFilter));

        string? nextPageToken = null;
        if (hasNextPage && items.Count > 0)
        {
            var lastItem = items.Last();
            var lastId = dbContext.Entry(lastItem).Property(keyPropertyName).CurrentValue;
            nextPageToken = lastId?.ToString();
        }

        return new EntListModel
        {
            Entities = entitiesModels.ToArray(),
            NextPageToken = nextPageToken,
            HasNextPage = hasNextPage
        };
    }
}

public class CustomClassToStringConverter : JsonConverter<object>
{
    public override bool CanConvert(Type typeToConvert)
    {
        // Unwrap nullable
        typeToConvert = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;

        // Exclude primitives
        if (typeToConvert.IsPrimitive ||
            typeToConvert.IsEnum ||
            typeToConvert == typeof(string) ||
            typeToConvert == typeof(decimal) ||
            typeToConvert == typeof(DateTime) ||
            typeToConvert == typeof(DateTimeOffset) ||
            typeToConvert == typeof(DateOnly) ||
            typeToConvert == typeof(TimeOnly) ||
            typeToConvert == typeof(Guid) ||
            typeToConvert == typeof(JsonElement))
            return false;

        // Exclude arrays and collections
        if (typeof(IEnumerable).IsAssignableFrom(typeToConvert))
            return false;

        // Exclude dictionaries
        if (typeof(IDictionary).IsAssignableFrom(typeToConvert))
            return false;

        // Only apply to custom reference types
        return typeToConvert.IsClass;
    }

    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.ToString());
    }
}
