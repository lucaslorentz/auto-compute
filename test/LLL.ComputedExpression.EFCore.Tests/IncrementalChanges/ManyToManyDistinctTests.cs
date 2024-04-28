using System.Linq.Expressions;
using FluentAssertions;

namespace LLL.ComputedExpression.EFCore.Tests.IncrementalChanges;

public class ManyToManyDistinctTests
{
    private static readonly Expression<Func<Person, IEnumerable<int>>> _computedExpression =
        (Person person) => person.Friends
            .Concat(person.Relatives)
            .Distinct()
            .Select(p => p.Id)
            .ToArray();

    [Fact]
    public async void TestCollectionElementAdded()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person1 = context!.Set<Person>().Find(1)!;
        var person2 = context!.Set<Person>().Find(2)!;
        await context.Entry(person2).Navigation(nameof(Person.Relatives)).LoadAsync();

        person2.Relatives.Add(person1);

        var changes = await context.GetChangesAsync(_computedExpression, static c => c.SetIncremental());

        changes.Should().BeEmpty();
        context.Entry(person2).Navigation(nameof(Person.Friends)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async void TestCollectionElementRemoved()
    {
        using var context = await TestDbContext.Create<PersonDbContext>(
            seedData: async seedContext =>
            {
                var p1 = seedContext!.Set<Person>().Find(1)!;
                var p2 = seedContext!.Set<Person>().Find(2)!;
                await seedContext.Entry(p2).Navigation(nameof(Person.Relatives)).LoadAsync();
                p2.Relatives.Add(p1);
            });

        var person2 = context!.Set<Person>().Find(2)!;
        await context.Entry(person2).Navigation(nameof(Person.Friends)).LoadAsync();
        person2.Friends.Clear();

        var changes = await context.GetChangesAsync(_computedExpression, static c => c.SetIncremental());

        changes.Should().BeEmpty();
        context.Entry(person2).Navigation(nameof(Person.Relatives)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async void TestCollectionElementRemovedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>(
            seedData: async seedContext =>
            {
                var p1 = seedContext!.Set<Person>().Find(1)!;
                var p2 = seedContext!.Set<Person>().Find(2)!;
                await seedContext.Entry(p2).Navigation(nameof(Person.Relatives)).LoadAsync();
                p2.Relatives.Add(p1);
            });

        var person1 = context!.Set<Person>().Find(1)!;
        await context.Entry(person1).Navigation(nameof(Person.FriendsInverse)).LoadAsync();
        person1.FriendsInverse.Clear();

        var changes = await context.GetChangesAsync(_computedExpression, static c => c.SetIncremental());

        var person2 = context!.Set<Person>().Find(2)!;
        var pet = context!.Set<Pet>().Find(1)!;
        changes.Should().BeEmpty();
        context.Entry(person2).Navigation(nameof(Person.Relatives)).IsLoaded.Should().BeFalse();
    }
}
