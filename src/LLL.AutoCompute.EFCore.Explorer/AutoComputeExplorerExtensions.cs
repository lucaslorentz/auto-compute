using LLL.AutoCompute.EFCore.Explorer.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using LLL.AutoCompute.EFCore.Metadata.Internal;

namespace LLL.AutoCompute.EFCore.Explorer;

public static class AutoComputeExplorerExtensions
{
    public static IServiceCollection AddAutoComputeExplorer(
        this IServiceCollection services,
        Action<AutoComputeExplorerOptions>? configure = null)
    {
        var options = services.AddOptions<AutoComputeExplorerOptions>();
        if (configure != null)
            options.Configure(configure);
        return services;
    }

    public static IEndpointConventionBuilder MapAutoComputeExplorer<TDbContext>(
        this IEndpointRouteBuilder endpoints,
        string basePath = "/auto-compute-explorer")
        where TDbContext : DbContext
    {
        basePath = basePath.TrimEnd('/');

        var group = endpoints.MapGroup(basePath);

        var apiGroup = group.MapGroup("/api");

        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<AutoComputeExplorerOptions>>().Value;

        // GET /api/ents
        apiGroup.MapGet("/ents", async (TDbContext dbContext) =>
        {
            var model = dbContext.Model.GetEntityTypes()
                .Where(e => !e.IsOwned())
                .Select(e => new EntModel { Name = e.Name })
                .ToArray();

            return Results.Ok(model);
        });

        // GET /api/ents/{entityName}/schema
        apiGroup.MapGet("/ents/{entityName}", async (string entityName, TDbContext dbContext) =>
        {
            var entityType = dbContext.Model.FindEntityType(entityName);
            if (entityType is null)
                return Results.NotFound();

            var model = await AutoComputeExplorerHelper.MapEntityType(entityType, dbContext, options.MethodFilter);
            return Results.Ok(model);
        });

        // GET /api/ents/{entityName}/items
        apiGroup.MapGet("/ents/{entityName}/items", async (
            string entityName,
            TDbContext dbContext,
            HttpContext httpContext,
            string? include,
            string? inconsistentMember,
            string? pageToken,
            DateTime? since,
            string? search,
            string? sortBy,
            bool sortDescending = false,
            int pageSize = 10) =>
        {
            var entityType = dbContext.Model.FindEntityType(entityName);
            if (entityType is null)
                return Results.NotFound();

            var filters = httpContext.Request.Query
                .Where(q => q.Key.StartsWith("f_"))
                .ToDictionary(q => q.Key[2..], q => q.Value.ToString());

            IQueryable<object> query;

            if (inconsistentMember is not null)
            {
                var computedMember = entityType.FindMember(inconsistentMember)?.GetComputedMember();
                if (computedMember is null)
                    return Results.UnprocessableEntity("Computed member not found");

                query = computedMember.QueryInconsistentEntities(
                    dbContext,
                    since ?? DateTime.Now.AddDays(-30))
                    .OfType<object>();
            }
            else
            {
                var setMethod = typeof(DbContext).GetMethod("Set", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, [typeof(string)])!;
                query = ((IQueryable)setMethod.MakeGenericMethod(entityType.ClrType).Invoke(dbContext, [entityType.Name])!)
                    .OfType<object>();
            }

            var result = await AutoComputeExplorerHelper.GetPaginatedList(
                dbContext,
                entityType,
                query,
                pageToken,
                pageSize,
                include,
                filters,
                search,
                sortBy,
                sortDescending,
                options.MethodFilter);

            return Results.Ok(result);
        });

        // GET /api/ents/{entityName}/items/{id}
        apiGroup.MapGet("/ents/{entityName}/items/{id}", async (
            string entityName,
            string id,
            string? include,
            TDbContext dbContext) =>
        {
            var entityType = dbContext.Model.FindEntityType(entityName);
            if (entityType is null)
                return Results.NotFound();

            var typedId = Convert.ChangeType(id, entityType.GetKeys().First().GetKeyType());
            var entity = await dbContext.FindAsync(entityType.ClrType, typedId);
            if (entity is null)
                return Results.NotFound();

            var parsedInclude = include?.Split(",").ToHashSet();
            var model = await AutoComputeExplorerHelper.EntityToModel(dbContext, entity, parsedInclude, options.MethodFilter);
            return Results.Ok(model);
        });

        // GET /api/ents/{entityName}/members/{memberName}/consistency
        apiGroup.MapGet("/ents/{entityName}/members/{memberName}/consistency", async (
            string entityName,
            string memberName,
            DateTime? since,
            TDbContext dbContext) =>
        {
            var entityType = dbContext.Model.FindEntityType(entityName);
            if (entityType is null)
                return Results.NotFound();

            var property = entityType.FindMember(memberName);
            if (property is null)
                return Results.NotFound();

            var computedMember = property.GetComputedMember();
            if (computedMember is null)
                return Results.NotFound();

            var consistency = await computedMember.CheckConsistencyAsync(dbContext, since ?? DateTime.Today.AddMonths(-1));
            return Results.Ok(new ConsistencyModel(consistency));
        });

        // POST /api/ents/{entityName}/members/{memberName}/consistency/fix
        apiGroup.MapPost("/ents/{entityName}/members/{memberName}/consistency/fix", async (
            string entityName,
            string memberName,
            DateTime? since,
            TDbContext dbContext) =>
        {
            var entityType = dbContext.Model.FindEntityType(entityName);
            if (entityType is null)
                return Results.NotFound();

            var property = entityType.FindMember(memberName);
            if (property is null)
                return Results.NotFound();

            var computedMember = property.GetComputedMember();
            if (computedMember is null)
                return Results.NotFound();

            var inconsistentEnts = computedMember
                .QueryInconsistentEntities(dbContext, since ?? DateTime.Today.AddMonths(-1))
                .OfType<object>()
                .ToArray();

            foreach (var ent in inconsistentEnts)
                await computedMember.FixAsync(ent, dbContext);

            await dbContext.SaveChangesAsync();

            return Results.Ok(inconsistentEnts.Length);
        });

        // POST /api/ents/{entityName}/items/{id}/fix
        apiGroup.MapPost("/ents/{entityName}/items/{id}/fix", async (
            string entityName,
            string id,
            string? memberName,
            TDbContext dbContext) =>
        {
            var entityType = dbContext.Model.FindEntityType(entityName);
            if (entityType is null)
                return Results.NotFound();

            var typedId = Convert.ChangeType(id, entityType.GetKeys().First().GetKeyType());
            var entity = await dbContext.FindAsync(entityType.ClrType, typedId);
            if (entity is null)
                return Results.NotFound();

            if (memberName is not null)
            {
                var property = entityType.FindMember(memberName);
                if (property is null)
                    return Results.NotFound();

                var computedMember = property.GetComputedMember();
                if (computedMember is null)
                    return Results.UnprocessableEntity("Member is not computed");

                await computedMember.FixAsync(entity, dbContext);
            }
            else
            {
                var properties = entityType.GetProperties()
                    .Select(p => p.GetComputedMember());
                var navigations = entityType.GetNavigations()
                    .Select(n => n.GetComputedMember());

                foreach (var computedMember in properties.Concat(navigations).Where(c => c is not null))
                    await computedMember!.FixAsync(entity, dbContext);
            }

            await dbContext.SaveChangesAsync();

            return Results.Ok();
        });

        // GET /api/computeds
        apiGroup.MapGet("/computeds", async (TDbContext dbContext) =>
        {
            var data = new List<EntComputedModel>();

            var sortedComputeds = dbContext.Model.GetAllComputedMembers()
                .OrderBy(c => c.Property.DeclaringType.Name)
                .ThenBy(c => c.Property.Name);

            foreach (var c in sortedComputeds)
                data.Add(await AutoComputeExplorerHelper.MapComputed(c, dbContext));

            return Results.Ok(data.ToArray());
        });

        // GET /api/observers
        apiGroup.MapGet("/observers", async (TDbContext dbContext) =>
        {
            var data = new List<EntObserverModel>();

            foreach (var entityType in dbContext.Model.GetEntityTypes().OrderBy(e => e.Name))
            {
                var observers = entityType.GetComputedObservers();
                if (observers is null) continue;

                foreach (var o in observers)
                    data.Add(await AutoComputeExplorerHelper.MapObserver(o, entityType.Name));
            }

            return Results.Ok(data.ToArray());
        });

        // GET /configuration.json
        group.MapGet("/configuration.json", () => Results.Ok(new { basePath }));

        // Serve embedded static files
        var assembly = typeof(AutoComputeExplorerExtensions).Assembly;
        var fileProvider = new ManifestEmbeddedFileProvider(assembly, "wwwroot");

        group.MapGet("/", (HttpContext context) =>
        {
            if (!context.Request.Path.HasValue || !context.Request.Path.Value.EndsWith("/"))
                return Results.Redirect(context.Request.Path.Value + "/", true);

            var fileInfo = fileProvider.GetFileInfo("index.html");
            return Results.File(fileInfo.CreateReadStream(), "text/html");
        });

        group.MapGet("/{**path}", (string path) =>
        {
            var fileInfo = fileProvider.GetFileInfo(path);
            if (!fileInfo.Exists)
            {
                return Results.File(fileProvider.GetFileInfo("index.html").CreateReadStream(), "text/html");
            }

            var contentType = GetContentType(path);
            return Results.File(fileInfo.CreateReadStream(), contentType);
        });

        return group;
    }

    private static string GetContentType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".html" => "text/html",
            ".js" => "application/javascript",
            ".css" => "text/css",
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".eot" => "application/vnd.ms-fontobject",
            ".map" => "application/json",
            _ => "application/octet-stream"
        };
    }
}
