using System.Linq.Expressions;
using FluentAssertions;

namespace LLL.AutoCompute.EFCore.Tests.IncrementalChanges;

public class ManyToManyContainsTests
{
    private static readonly Expression<Func<Person, bool>> _computedExpression =
        (Person person) => person.Friends
            .Concat(person.Relatives)
            .Any();

    [Fact]
    public async Task LoadsEntireCollectionsToDetermineAny()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person1 = context!.Set<Person>().Find(1)!;
        var person2 = context!.Set<Person>().Find(2)!;
        await context.Entry(person1).Navigation(nameof(Person.RelativesInverse)).LoadAsync();

        person1.RelativesInverse.Add(person2);

        var changes = await context.GetChangesAsync(_computedExpression, default, static c => c.CurrentValueIncremental());

        changes.Should().HaveCount(1);
        context.Entry(person2).Navigation(nameof(Person.Friends)).IsLoaded.Should().BeTrue();
        context.Entry(person2).Navigation(nameof(Person.Relatives)).IsLoaded.Should().BeTrue();
    }
}
