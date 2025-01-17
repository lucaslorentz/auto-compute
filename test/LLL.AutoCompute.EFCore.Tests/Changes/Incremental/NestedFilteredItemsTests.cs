using System.Linq.Expressions;
using FluentAssertions;
using LLL.AutoCompute.ChangeCalculations;

namespace LLL.AutoCompute.EFCore.Tests.Changes.Incremental;

public class NestedFilteredItemsTests
{
    private static readonly Expression<Func<Person, IEnumerable<Pet>>> _computedExpression = (Person person) =>
        person.Friends.SelectMany(f => f.Pets).Where(p => p.Type == PetType.Cat);

    [Fact]
    public async Task TestNestedCollectionElementModified()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Type = PetType.Other;

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.SetIncremental());

        var person2 = context!.Set<Person>().Find(PersonDbContext.PersonBId)!;
        changes.Should().BeEquivalentTo(new Dictionary<Person, SetChange<Pet>>{
            { person2, new SetChange<Pet> { Removed = [pet], Added = [] }}
        });
        context.Entry(person2).Navigation(nameof(Person.Friends)).IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task TestCollectionElementRemoved()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person2 = context!.Set<Person>().Find(PersonDbContext.PersonBId)!;
        await context.Entry(person2).Navigation(nameof(Person.Friends)).LoadAsync();
        person2.Friends.Clear();

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.SetIncremental());

        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        changes.Should().BeEquivalentTo(new Dictionary<Person, SetChange<Pet>>{
            { person2, new SetChange<Pet> { Removed = [pet], Added = []}}
        });
    }

    [Fact]
    public async Task TestCollectionElementRemovedInverse()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var personA = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        await context.Entry(personA).Navigation(nameof(Person.FriendsInverse)).LoadAsync();
        personA.FriendsInverse.Clear();

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.SetIncremental());

        var person2 = context!.Set<Person>().Find(PersonDbContext.PersonBId)!;
        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        changes.Should().BeEquivalentTo(new Dictionary<Person, SetChange<Pet>>{
            { person2, new SetChange<Pet> { Removed = [pet], Added = []}}
        });
        context.Entry(person2).Navigation(nameof(Person.Friends)).IsLoaded.Should().BeFalse();
    }
}
