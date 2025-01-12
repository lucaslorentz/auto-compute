using System.Linq.Expressions;
using FluentAssertions;

namespace LLL.AutoCompute.EFCore.Tests.IncrementalChanges;

public class ManyToManyJoinDistinctTests
{
    private static readonly Expression<Func<Person, IEnumerable<string>>> _computedExpression =
        (Person person) => person.FriendsJoin.Select(f => f.ToPerson)
            .Concat(person.RelativesJoin.Select(x => x.ToPerson))
            .Distinct()
            .Select(p => p.Id)
            .ToArray();

    [Fact]
    public async Task TestCollectionElementAdded()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var personA = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var personB = context!.Set<Person>().Find(PersonDbContext.PersonBId)!;
        await context.Entry(personB).Navigation(nameof(Person.Relatives)).LoadAsync();

        personB.Relatives.Add(personA);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.SetIncremental());

        changes.Should().BeEmpty();
        context.Entry(personB).Navigation(nameof(Person.Friends)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementRemoved()
    {
        using var context = await TestDbContext.Create<PersonDbContext>(
            seedData: async seedContext =>
            {
                var p1 = seedContext!.Set<Person>().Find(PersonDbContext.PersonAId)!;
                var p2 = seedContext!.Set<Person>().Find(PersonDbContext.PersonBId)!;
                await seedContext.Entry(p2).Navigation(nameof(Person.Relatives)).LoadAsync();
                p2.Relatives.Add(p1);
            });

        var person2 = context!.Set<Person>().Find(PersonDbContext.PersonBId)!;
        await context.Entry(person2).Navigation(nameof(Person.Friends)).LoadAsync();
        person2.Friends.Clear();

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.SetIncremental());

        changes.Should().BeEmpty();
        context.Entry(person2).Navigation(nameof(Person.Relatives)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementRemovedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>(
            seedData: async seedContext =>
            {
                var p1 = seedContext!.Set<Person>().Find(PersonDbContext.PersonAId)!;
                var p2 = seedContext!.Set<Person>().Find(PersonDbContext.PersonBId)!;
                await seedContext.Entry(p2).Navigation(nameof(Person.Relatives)).LoadAsync();
                p2.Relatives.Add(p1);
            });

        var personA = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        await context.Entry(personA).Navigation(nameof(Person.FriendsInverse)).LoadAsync();
        personA.FriendsInverse.Clear();

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.SetIncremental());

        var person2 = context!.Set<Person>().Find(PersonDbContext.PersonBId)!;
        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        changes.Should().BeEmpty();
        context.Entry(person2).Navigation(nameof(Person.Relatives)).IsLoaded.Should().BeFalse();
    }
}
