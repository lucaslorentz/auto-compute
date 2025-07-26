using System.Linq.Expressions;
using FluentAssertions;

namespace LLL.AutoCompute.EFCore.Tests.Changes.Incremental;

public class NestedFilteredCount
{
    private static readonly Expression<Func<Person, int>> _computedExpression = person =>
        person.Friends.SelectMany(f => f.Pets).Where(p => p.Color == PetColor.Orange).Count();

    [Fact]
    public async Task TestNestedCollectionElementModified()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Color = PetColor.Other;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.NumberIncremental());

        var person2 = context!.Set<Person>().Find(PersonDbContext.PersonBId)!;
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person2, -1}
        });
        context.Entry(person2).Navigation(nameof(Person.Friends)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementRemoved()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var person2 = context!.Set<Person>().Find(PersonDbContext.PersonBId)!;
        await context.Entry(person2).Navigation(nameof(Person.Friends)).LoadAsync();
        person2.Friends.Clear();

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.NumberIncremental());
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person2, -1}
        });
    }

    [Fact]
    public async Task TestCollectionElementRemovedInverse()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var personA = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        await context.Entry(personA).Navigation(nameof(Person.FriendsInverse)).LoadAsync();
        personA.FriendsInverse.Clear();

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.NumberIncremental());

        var person2 = context!.Set<Person>().Find(PersonDbContext.PersonBId)!;
        changes.Should().BeEquivalentTo(new Dictionary<Person, int>{
            { person2, -1}
        });
        context.Entry(person2).Navigation(nameof(Person.Friends)).IsLoaded.Should().BeFalse();
    }
}
