using FluentAssertions;

namespace LLL.AutoCompute.EFCore.Tests.Changes;

public class ChangeTrackingTests
{
    [Fact]
    public async Task Property()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;

        person.FirstName = "Modified";

        var changes = await context.GetChangesAsync(
            (Person p) => p.AsComputedUntracked().AsComputedTracked().FirstName + " " + p.LastName,
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
    public async Task Collection()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var person = context!.Set<Person>().Find(PersonDbContext.PersonAId)!;
        await context.Entry(person).Collection(nameof(Person.Pets)).LoadAsync();
        person.Pets.Clear();

        var changes = await context.GetChangesAsync(
            (Person p) => p.AsComputedUntracked().AsComputedTracked().Pets.Count(p => p.Type == PetType.Cat),
            default,
            c => c.Void());
        changes.Should().HaveCount(1);
        
        changes = await context.GetChangesAsync(
            (Person p) => p.AsComputedUntracked().Pets.Count(p => p.Type == PetType.Cat),
            default,
            c => c.Void());
        changes.Should().BeEmpty();        
    }

    [Fact]
    public async Task CollectionItemProperty()
    {
        using var context = await TestDbContext.Create<PersonDbContext>();

        var pet = context!.Set<Pet>().Find(PersonDbContext.PersonAPet1Id)!;
        pet.Type = PetType.Other;

        var changes = await context.GetChangesAsync(
            (Person p) => p.Pets.Count(p => p.AsComputedUntracked().AsComputedTracked().Type == PetType.Cat && p.Color == "Black"),
            default,
            c => c.Void());
        changes.Should().HaveCount(1);

        changes = await context.GetChangesAsync(
            (Person p) => p.Pets.Count(p => p.AsComputedUntracked().Type == PetType.Cat && p.Color == "Black"),
            default,
            c => c.Void());
        changes.Should().BeEmpty();
    }
}
