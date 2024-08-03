using FluentAssertions;

namespace LLL.AutoCompute.EFCore.Tests.Changes;

public class StopTrackingTests
{
    [Fact]
    public async void Property()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;

        person.FirstName = "Modified";

        var changes = await context.GetChangesAsync(
            (Person p) => p.FirstName + " " + p.LastName,
            default,
            c => c.Void());
        changes.Should().HaveCount(1);

        changes = await context.GetChangesAsync(
            (Person p) => p.AsComputedUntracked().FirstName + " " + p.LastName,
            default,
            c => c.Void());
        changes.Should().BeEmpty();
    }

    [Fact]
    public async void Collection()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(1)!;
        await context.Entry(person).Collection(nameof(Person.Pets)).LoadAsync();
        person.Pets.Clear();

        var changes = await context.GetChangesAsync(
            (Person p) => p.Pets.Count(p => p.Type == "Cat"),
            default,
            c => c.Void());
        changes.Should().HaveCount(1);
        
        changes = await context.GetChangesAsync(
            (Person p) => p.AsComputedUntracked().Pets.Count(p => p.Type == "Cat"),
            default,
            c => c.Void());
        changes.Should().BeEmpty();        
    }

    [Fact]
    public async void CollectionItemProperty()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(1)!;
        pet.Type = "Modified";

        var changes = await context.GetChangesAsync(
            (Person p) => p.Pets.Count(p => p.Type == "Cat" && p.Color == "Black"),
            default,
            c => c.Void());
        changes.Should().HaveCount(1);

        changes = await context.GetChangesAsync(
            (Person p) => p.Pets.Count(p => p.AsComputedUntracked().Type == "Cat" && p.Color == "Black"),
            default,
            c => c.Void());
        changes.Should().BeEmpty();
    }
}
