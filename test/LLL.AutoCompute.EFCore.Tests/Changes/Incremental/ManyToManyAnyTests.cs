using System.Linq.Expressions;
using FluentAssertions;

namespace LLL.AutoCompute.EFCore.Tests.Changes.Incremental;

public class ManyToManyContainsTests
{
    private static readonly Expression<Func<Person, bool>> _computedExpression =
        (Person person) => person.Friends
            .Concat(person.Relatives)
            .Any();

    [Fact]
    public async Task LoadsEntireCollectionsToDetermineAny()
    {
        using var context = await TestDbContextFactory.Create<PersonDbContext>();

        var personA = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        var personB = context!.Set<Person>().Find(PersonDbContext.PersonBId)!;
        await context.Entry(personA).Navigation(nameof(Person.RelativesInverse)).LoadAsync();

        personA.RelativesInverse.Add(personB);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.CurrentValueIncremental());

        changes.Should().HaveCount(1);
        context.Entry(personB).Navigation(nameof(Person.Friends)).IsLoaded.Should().BeTrue();
        context.Entry(personB).Navigation(nameof(Person.Relatives)).IsLoaded.Should().BeTrue();
    }
}
