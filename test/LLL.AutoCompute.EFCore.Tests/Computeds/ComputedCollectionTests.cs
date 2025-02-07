﻿using FluentAssertions;

namespace LLL.AutoCompute.EFCore.Tests.Computeds;

public class ComputedCollectionTests
{
    [Fact]
    public async Task TestCreateAndUpdateChainedItems()
    {
        using var context = await GetDbContextAsync();

        var customerA = context.Customers.Find(CommerceDbContext.CustomerAId)!;
        
        VerifyInitialStateCustomerA(customerA);

        var order1 = context.Orders.Find(1)!;

        // Clone order 1
        var order2 = new Order
        {
            Customer = customerA,
            CloneFrom = order1
        };

        context.Orders.Add(order2);

        // Clone order 2
        var order3 = new Order
        {
            Customer = customerA,
            CloneFrom = order2
        };

        context.Orders.Add(order3);

        await context.SaveChangesAsync();

        // Verify computeds were updated
        var order2ItemA = order2.Items.First(i => i.Product!.Id == CommerceDbContext.ProductAId);
        order2ItemA.UnitPrice.Should().Be(10);
        order2ItemA.Total.Should().Be(20);
        order2.Total.Should().Be(20);

        var order3ItemA = order3.Items.First(i => i.Product!.Id == CommerceDbContext.ProductAId);
        order3ItemA.UnitPrice.Should().Be(10);
        order3ItemA.Total.Should().Be(20);
        order3.Total.Should().Be(20);

        customerA.OrderCount.Should().Be(3);
        customerA.TotalSpent.Should().Be(60);

        // Modify order 1
        order1.Items.Add(new OrderItem
        {
            Product = context.Products.Find(CommerceDbContext.ProductBId)!,
            Quantity = 3
        });

        await context.SaveChangesAsync();

        // Verify computeds were updated
        var order2ItemB = order2.Items.First(i => i.Product!.Id == CommerceDbContext.ProductBId);
        order2ItemB.UnitPrice.Should().Be(5);
        order2ItemB.Total.Should().Be(15);
        order2.Total.Should().Be(35);
        order2.Items.Should().Contain(order2ItemA); // Verify item A was reused and not recreated

        var order3ItemB = order2.Items.First(i => i.Product!.Id == CommerceDbContext.ProductBId);
        order3ItemB.UnitPrice.Should().Be(5);
        order3ItemB.Total.Should().Be(15);
        order3.Total.Should().Be(35);
        order3.Items.Should().Contain(order3ItemA); // Verify item A was reused and not recreated

        customerA.OrderCount.Should().Be(3);
        customerA.TotalSpent.Should().Be(105);

        // Verify collections were not loaded to compute incremental properties
        context.Entry(customerA).Navigation(nameof(Customer.Orders)).IsLoaded.Should().BeFalse();
    }

    private static async Task<CommerceDbContext> GetDbContextAsync()
    {
        return await TestDbContext.Create<CommerceDbContext>();
    }

    private static void VerifyInitialStateCustomerA(Customer customerA)
    {
        customerA.OrderCount.Should().Be(1);
        customerA.TotalSpent.Should().Be(20);
    }
}
