using AwesomeAssertions;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Tests.Computeds;

[Collection("AutoComputed design time")]
public class AutoComputedAttributeTests
{
    private static async Task<(SqliteConnection Connection, DbContextOptions Options)> CreateDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DeclarativeContext>().UseSqlite(connection).UseAutoCompute().Options;
        await using var context = new DeclarativeContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Add(new DeclarativePerson
        {
            Id = 1, Name = "Alice", Pets = [new DeclarativePet { Id = 1, IsCat = true }]
        });
        context.Add(new DeclarativePerson { Id = 2, Name = "Bob" });
        await context.SaveChangesAsync();
        return (connection, options);
    }

    [Fact]
    public async Task MapsStoredColumnsAndRetainsGetterBehavior()
    {
        var (connection, options) = await CreateDatabase();
        await using var connectionScope = connection;
        await using var context = new DeclarativeContext(options);
        var property = context.Model.FindEntityType(typeof(DeclarativePerson))!.FindProperty(nameof(DeclarativePerson.NumberOfCats))!;
        property.PropertyInfo.Should().NotBeNull();
        property.FieldInfo.Should().NotBeNull();
        property.GetPropertyAccessMode().Should().Be(PropertyAccessMode.Field);
        property.IsShadowProperty().Should().BeFalse();
        property.GetComputedMember().Should().NotBeNull();

        var query = context.People.Where(p => p.NumberOfCats > 0)
            .OrderByDescending(p => p.NumberOfCats).Select(p => p.NumberOfCats);
        var sql = query.ToQueryString();
        sql.Should().Contain("NumberOfCats").And.NotContain("COUNT(").And.NotContain("JOIN");
        (await query.SingleAsync()).Should().Be(1);

        var person = await context.People.SingleAsync(p => p.Id == 1);
        person.NumberOfCats.Should().Be(0);
        context.Entry(person).Property(p => p.NumberOfCats).CurrentValue.Should().Be(1);
        context.Entry(person).Property(p => p.NumberOfCats).OriginalValue.Should().Be(1);
        (await context.SaveChangesAsync()).Should().Be(0);
        await context.Entry(person).Collection(p => p.Pets).LoadAsync();
        person.NumberOfCats.Should().Be(1);
        person.Pets.Add(new DeclarativePet { IsCat = true });
        person.NumberOfCats.Should().Be(2);
        await context.SaveChangesAsync();
        context.Entry(person).Property(p => p.NumberOfCats).CurrentValue.Should().Be(2);
    }

    [Fact]
    public async Task TracksRelatedChangesWithUnloadedParent()
    {
        var (connection, options) = await CreateDatabase();
        await using var connectionScope = connection;
        await using (var context = new DeclarativeContext(options))
        {
            var pet = await context.Pets.SingleAsync();
            pet.IsCat = false;
            await context.SaveChangesAsync();
        }
        await using (var context = new DeclarativeContext(options))
        {
            (await context.People.Where(p => p.Id == 1).Select(p => p.NumberOfCats).SingleAsync()).Should().Be(0);
            var pet = await context.Pets.SingleAsync();
            pet.IsCat = true;
            pet.OwnerId = 2;
            context.SaveChanges();
        }
        await using (var context = new DeclarativeContext(options))
        {
            (await context.People.OrderBy(p => p.Id).Select(p => p.NumberOfCats).ToArrayAsync()).Should().Equal(0, 1);
            context.Remove(await context.Pets.SingleAsync());
            await context.SaveChangesAsync();
        }
        await using (var context = new DeclarativeContext(options))
            (await context.People.Select(p => p.NumberOfCats).ToArrayAsync()).Should().Equal(0, 0);
    }

    [Fact]
    public async Task ComputedDependenciesReadStoredValueWithoutLoadingNavigation()
    {
        var (connection, options) = await CreateDatabase();
        await using var connectionScope = connection;
        await using var context = new DeclarativeContext(options);
        var person = await context.People.SingleAsync(p => p.Id == 1);
        person.Name = "Ann";
        await context.SaveChangesAsync();
        context.Entry(person).Collection(p => p.Pets).IsLoaded.Should().BeFalse();
        context.Entry(person).Property(p => p.Label).CurrentValue.Should().Be("Ann:1");
        (await context.People.Where(p => p.Id == 1).Select(p => p.Label).SingleAsync()).Should().Be("Ann:1");

        await context.People.Where(p => p.Id == 1).ExecuteUpdateAsync(s => s.SetProperty(p => p.Label, "wrong"));
        await context.Entry(person).ReloadAsync();
        var member = context.Entry(person).Property(p => p.Label).Metadata.GetComputedMember()!;
        await member.FixAsync(person, context);
        context.Entry(person).Property(p => p.Label).CurrentValue.Should().Be("Ann:1");
        context.Entry(person).Collection(p => p.Pets).IsLoaded.Should().BeFalse();
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task ConsistencyChecksDetectCorruptStorageAndRepairIt()
    {
        var (connection, options) = await CreateDatabase();
        await using var connectionScope = connection;
        await using var context = new DeclarativeContext(options);
        await context.People.Where(p => p.Id == 1).ExecuteUpdateAsync(s => s.SetProperty(p => p.NumberOfCats, 9));
        var member = context.Model.FindEntityType(typeof(DeclarativePerson))!
            .FindProperty(nameof(DeclarativePerson.NumberOfCats))!.GetComputedMember()!;
        (await member.QueryInconsistentEntities(context, DateTime.MinValue).Cast<DeclarativePerson>().SingleAsync()).Id.Should().Be(1);
        var person = await context.People.Include(p => p.Pets).SingleAsync(p => p.Id == 1);
        context.PrepareComputedExpressionForInMemory(member.GetIsMemberConsistentLambda(context))
            .Compile().DynamicInvoke(person).Should().Be(false);
        await member.FixAsync(person, context);
        await context.SaveChangesAsync();
        (await member.QueryInconsistentEntities(context, DateTime.MinValue).Cast<DeclarativePerson>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task FluentOverridesAndIgnoresTakePrecedence()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<OverrideContext>().UseSqlite(connection).UseAutoCompute().Options;
        await using var context = new OverrideContext(options);
        await context.Database.EnsureCreatedAsync();
        var person = new DeclarativePerson { Name = "Alice" };
        context.Add(person);
        await context.SaveChangesAsync();
        context.Entry(person).Property(p => p.NumberOfCats).CurrentValue.Should().Be(5);
        context.Model.FindEntityType(typeof(DeclarativePerson))!.FindProperty(nameof(DeclarativePerson.Label)).Should().BeNull();
    }

    [Fact]
    public async Task DesignTimeModelIncludesMappedStorage()
    {
        var (connection, options) = await CreateDatabase();
        await using var connectionScope = connection;
        await using var context = new DeclarativeContext(options);
        var model = context.GetService<IDesignTimeModel>().Model;
        var property = model.FindEntityType(typeof(DeclarativePerson))!.FindProperty(nameof(DeclarativePerson.NumberOfCats))!;
        property.FieldInfo.Should().NotBeNull();
        model.GetRelationalModel().Tables.Single(t => t.Name == "People").Columns
            .Should().Contain(c => c.Name == nameof(DeclarativePerson.NumberOfCats));
    }

    [Fact]
    public void RejectsPropertyAccessThatWouldInvokeTheGetter()
    {
        var options = new DbContextOptionsBuilder<InvalidAccessContext>().UseSqlite("Data Source=:memory:").UseAutoCompute().Options;
        using var context = new InvalidAccessContext(options);
        var buildModel = () => context.Model;
        buildModel.Should().Throw<InvalidOperationException>().WithMessage("*requires its generated backing field*");
    }

    [Fact]
    public async Task MapsInheritedPropertyOnce()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<InheritanceContext>().UseSqlite(connection).UseAutoCompute().Options;
        await using var context = new InheritanceContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Add(new ADerivedDeclarativeEntity { Value = 3 });
        context.Add(new BaseDeclarativeEntity { Value = 4 });
        await context.SaveChangesAsync();
        (await context.Set<ADerivedDeclarativeEntity>().Select(e => e.Doubled).SingleAsync()).Should().Be(6);
        (await context.Set<BaseDeclarativeEntity>().OrderBy(e => e.Value).Select(e => e.Doubled).ToArrayAsync()).Should().Equal(6, 8);
        context.Model.GetAllComputedMembers().Should().HaveCount(1);
    }

    [Fact]
    public async Task GetterRetainsLazyLoadingBehavior()
    {
        var (connection, _) = await CreateDatabase();
        await using var connectionScope = connection;
        var options = new DbContextOptionsBuilder<DeclarativeContext>().UseSqlite(connection)
            .UseLazyLoadingProxies().UseAutoCompute().Options;
        await using var context = new DeclarativeContext(options);
        var person = await context.People.SingleAsync(p => p.Id == 1);
        context.Entry(person).Property(p => p.NumberOfCats).CurrentValue.Should().Be(1);
        context.Entry(person).Collection(p => p.Pets).IsLoaded.Should().BeFalse();
        person.NumberOfCats.Should().Be(1);
        context.Entry(person).Collection(p => p.Pets).IsLoaded.Should().BeTrue();
    }

    [Fact]
    public void RejectsSelfReferencingComputedProperty()
    {
        var options = new DbContextOptionsBuilder<CyclicContext>().UseSqlite("Data Source=:memory:").UseAutoCompute().Options;
        using var context = new CyclicContext(options);
        var buildModel = () => context.Model;
        buildModel.Should().Throw<Exception>().WithMessage("*computed expression*");
    }

    [Fact]
    public async Task NotMappedAttributeIsRespected()
    {
        var (connection, options) = await CreateDatabase();
        await using var connectionScope = connection;
        await using var context = new DeclarativeContext(options);
        context.Model.FindEntityType(typeof(DeclarativePerson))!.FindProperty(nameof(DeclarativePerson.Ignored)).Should().BeNull();
    }

    [Fact]
    public async Task IncrementalOverrideAndObserversUseStoredValues()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<IncrementalDeclarativeContext>().UseSqlite(connection).UseAutoCompute().Options;
        await using (var context = new IncrementalDeclarativeContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Add(new DeclarativePerson { Id = 1, Name = "Alice", Pets = [new DeclarativePet { IsCat = true }] });
            await context.SaveChangesAsync();
        }
        await using (var context = new IncrementalDeclarativeContext(options))
        {
            var person = await context.People.SingleAsync();
            person.Name = "Ann";
            await context.SaveChangesAsync();
            person.ObservedLabel.Should().Be("Ann:1");
            context.Entry(person).Collection(p => p.Pets).IsLoaded.Should().BeFalse();
            context.Add(new DeclarativePet { OwnerId = 1, IsCat = true });
            await context.SaveChangesAsync();
            context.Entry(person).Property(p => p.NumberOfCats).CurrentValue.Should().Be(2);
            person.ObservedLabel.Should().Be("Ann:2");
            context.Entry(person).Collection(p => p.Pets).IsLoaded.Should().BeFalse();
        }
    }

    [Fact]
    public void MigrationBackfillReceivesGeneratedExpressionHash()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DeclarativeContext>().UseSqlite("Data Source=:memory:")
            .EnableServiceProviderCaching(false);
        optionsBuilder.UseAutoCompute(c => c.EnableBackfillInMigrations());
        using var context = new DeclarativeContext(optionsBuilder.Options);
        var previous = EF.IsDesignTime;
        try
        {
            EF.IsDesignTime = true;
            var model = context.GetService<IDesignTimeModel>().Model;
            var property = model.FindEntityType(typeof(DeclarativePerson))!.FindProperty(nameof(DeclarativePerson.NumberOfCats))!;
            property[AutoComputeAnnotationNames.Hash].Should().BeOfType<string>().Which.Should().HaveLength(64);
            property.GetAnnotations().Should().NotContain(a => a.Name.StartsWith(AutoComputeAnnotationNames.TempPrefix));
        }
        finally
        {
            EF.IsDesignTime = previous;
            DesignTimeComputedStore.Factories = null;
        }
    }
}

[CollectionDefinition("AutoComputed design time", DisableParallelization = true)]
public class AutoComputedDesignTimeCollection;

public class DeclarativeContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<DeclarativePerson> People => Set<DeclarativePerson>();
    public DbSet<DeclarativePet> Pets => Set<DeclarativePet>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeclarativePerson>().HasIndex(p => p.NumberOfCats);
        modelBuilder.Entity<DeclarativePet>().HasOne(p => p.Owner).WithMany(p => p.Pets).HasForeignKey(p => p.OwnerId);
    }
}

public class OverrideContext(DbContextOptions options) : DeclarativeContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<DeclarativePerson>().Ignore(p => p.Label);
        modelBuilder.Entity<DeclarativePerson>().ComputedProperty(p => p.NumberOfCats, p => p.Name.Length);
    }
}

public class InvalidAccessContext(DbContextOptions options) : DeclarativeContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<DeclarativePerson>().Property(p => p.NumberOfCats).UsePropertyAccessMode(PropertyAccessMode.Property);
    }
}

public class IncrementalDeclarativeContext(DbContextOptions options) : DeclarativeContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<DeclarativePerson>().ComputedProperty(p => p.NumberOfCats,
            p => p.Pets.Count(pet => pet.IsCat), c => c.NumberIncremental());
        modelBuilder.Entity<DeclarativePerson>().ComputedObserver("LabelChanged", p => p.Label, null,
            c => c.CurrentValue(), (person, label) =>
            {
                person.ObservedLabel = label;
                return Task.CompletedTask;
            });
    }
}

public class InheritanceContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BaseDeclarativeEntity>();
        modelBuilder.Entity<ADerivedDeclarativeEntity>();
    }
}

public class CyclicContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<CyclicEntity>();
}

public partial class CyclicEntity
{
    public int Id { get; set; }
    [AutoComputed] public int Count => Count + 1;
}

public partial class BaseDeclarativeEntity
{
    public int Id { get; set; }
    public int Value { get; set; }
    [AutoComputed] public int Doubled => Value * 2;
}

public class ADerivedDeclarativeEntity : BaseDeclarativeEntity;

public partial class DeclarativePerson
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public virtual ICollection<DeclarativePet> Pets { get; set; } = [];

    [AutoComputed]
    public int NumberOfCats => Pets.Count(p => p.IsCat);

    [AutoComputed]
    public string Label { get { return Name + ":" + NumberOfCats; } }

    [AutoComputed, System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int Ignored => Name.Length;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? ObservedLabel { get; set; }
}

public class DeclarativePet
{
    public int Id { get; set; }
    public bool IsCat { get; set; }
    public int OwnerId { get; set; }
    public virtual DeclarativePerson Owner { get; set; } = null!;
}
