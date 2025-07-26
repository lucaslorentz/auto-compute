using System.Linq.Expressions;
using FluentAssertions;
using LLL.AutoCompute.ChangeCalculations;

namespace LLL.AutoCompute.EFCore.Tests.Computeds;

public class ComputedObserverTests
{
    [Fact]
    public async Task TestObservePropertiesCurrentValue()
    {
        using var context = await CreateContextWithObserver(
            (Person p) => p.FirstName + " " + p.LastName,
            c => c.CurrentValue(),
            out var observedChanges);

        var person = new Person { Id = "New", FirstName = "Jane", LastName = "Doe" };
        context.Add(person);

        await context.SaveChangesAsync();

        observedChanges.Should().BeEquivalentTo([
            (person, "Jane Doe")
        ]);
    }
    [Fact]
    public async Task TestObservePropertiesVoid()
    {
        using var context = await CreateContextWithObserver(
            (Person p) => p.FirstName + " " + p.LastName,
            c => c.Void(),
            out var observedChanges);

        var person = new Person { Id = "New", FirstName = "Jane", LastName = "Doe" };
        context.Add(person);

        await context.SaveChangesAsync();

        observedChanges.Should().BeEquivalentTo([
            (person, new VoidChange())
        ]);
    }

    [Fact]
    public async Task TestObserveCollectionsCurrentValue()
    {
        using var context = await CreateContextWithObserver(
            (Person p) => p.Pets.Where(p => p.Color == PetColor.Orange),
            c => c.CurrentValue(),
            out var observedChanges);

        var person = context.Set<Person>().Find(PersonDbContext.PersonAId)!;

        var pet1 = context.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;

        var newPet = new Cat { Id = "New", Color = PetColor.Orange };
        person.Pets.Add(newPet);

        await context.SaveChangesAsync();

        observedChanges.Should().BeEquivalentTo([
            (person, new Pet[] { pet1, newPet })
        ]);
    }

    [Fact]
    public async Task TestObserveCollectionsCurrentValueIncremental()
    {
        using var context = await CreateContextWithObserver(
            (Person p) => p.Pets.Where(p => p.Color == PetColor.Orange),
            c => c.CurrentValueIncremental(),
            out var observedChanges);

        var person = context.Set<Person>().Find(PersonDbContext.PersonAId)!;

        var newPet = new Cat { Id = "New", Color = PetColor.Orange };
        person.Pets.Add(newPet);

        await context.SaveChangesAsync();

        observedChanges.Should().BeEquivalentTo([
            (person, new Pet[] { newPet })
        ]);
    }

    [Fact]
    public async Task TestObserveCollectionsSetIncremental()
    {
        using var context = await CreateContextWithObserver(
            (Person p) => p.Pets.Where(p => p.Color == PetColor.Orange),
            c => c.SetIncremental(),
            out var observedChanges);

        var person = context.Set<Person>().Find(PersonDbContext.PersonAId)!;

        var newPet = new Cat { Id = "New", Color = PetColor.Orange };
        person.Pets.Add(newPet);

        await context.SaveChangesAsync();

        observedChanges.Should().BeEquivalentTo([
            (person, new SetChange<Pet> {
                Added = [newPet],
                Removed = []
            })
        ]);
    }

    [Fact]
    public async Task TestObserveCollectionsNumberIncremental()
    {
        using var context = await CreateContextWithObserver(
            (Person p) => p.Pets.Where(p => p.Color == PetColor.Orange).Count(),
            c => c.NumberIncremental(),
            out var observedChanges);

        var person = context.Set<Person>().Find(PersonDbContext.PersonAId)!;

        var newPet = new Cat { Id = "New", Color = PetColor.Orange };
        person.Pets.Add(newPet);

        await context.SaveChangesAsync();

        observedChanges.Should().BeEquivalentTo([
            (person, 1)
        ]);
    }

    private static Task<PersonDbContext> CreateContextWithObserver<TEntity, TChange, TValue>(
        Expression<Func<TEntity, TChange>> observerExpression,
        ChangeCalculationSelector<TChange, TValue> calculationSelector,
        out List<(TEntity, TValue?)> observedChanges)
        where TEntity : class
    {
        var localObservedChanges = observedChanges = [];

        return CreateContext();

        async Task<PersonDbContext> CreateContext()
        {
            var context = await TestDbContextFactory.Create(o => new PersonDbContext(o, new PersonDbContextParams
            {
                SetupObservers = modelBuilder =>
                {
                    modelBuilder.Entity<TEntity>()
                        .ComputedObserver(
                            observerExpression,
                            null,
                            calculationSelector,
                            async (p, value) =>
                            {
                                localObservedChanges.Add((p, value));
                            });
                }
            }));

            // Ignore changes observed during seed
            localObservedChanges.Clear();

            return context;
        }
    }
}
