using System.Net.Http.Json;
using BlazingPizza.Data;
using BlazingPizza.Server;
using Microsoft.EntityFrameworkCore;

namespace BlazingPizza.Components;

public class OrdersClient(PizzaStoreContext db)
{
    public async Task<IEnumerable<OrderWithStatus>> GetOrders()
    {
        var orders = await db.Orders
            //.Where(o => o.UserId == userId) // TODO: filter by current user
            .Include(o => o.DeliveryLocation)
            .Include(o => o.Pizzas).ThenInclude(p => p.Special)
            .Include(o => o.Pizzas).ThenInclude(p => p.Toppings).ThenInclude(t => t.Topping)
            .OrderByDescending(o => o.CreatedTime)
            .ToListAsync();

        return orders.Select(o => OrderWithStatus.FromOrder(o)).ToList();
    }

    public async Task<OrderWithStatus> GetOrder(int orderId)
    {
        var order = await db.Orders
            .Where(o => o.OrderId == orderId)
            //.Where(o => o.UserId == PizzaApiExtensions.GetUserId(HttpContext))
            .Include(o => o.DeliveryLocation)
            .Include(o => o.Pizzas).ThenInclude(p => p.Special)
            .Include(o => o.Pizzas).ThenInclude(p => p.Toppings).ThenInclude(t => t.Topping)
            .SingleOrDefaultAsync();

        return order is not null
            ? OrderWithStatus.FromOrder(order)
            : new();
    }

    public async Task<int> PlaceOrder(Order order)
    {
        order.CreatedTime = DateTime.Now;
        order.DeliveryLocation = new LatLong(51.5001, -0.1239);
        //order.UserId = PizzaApiExtensions.GetUserId(HttpContext);

        // Enforce existence of Pizza.SpecialId and Topping.ToppingId
        // in the database - prevent the submitter from making up
        // new specials and toppings
        foreach (var pizza in order.Pizzas)
        {
            pizza.SpecialId = pizza.Special?.Id ?? 0;
            pizza.Special = null;

            foreach (var topping in pizza.Toppings)
            {
                topping.ToppingId = topping.Topping?.Id ?? 0;
                topping.Topping = null;
            }
        }

        db.Orders.Attach(order);
        await db.SaveChangesAsync();

        // In the background, send push notifications if possible
        // var subscription = await db.NotificationSubscriptions.Where(e => e.UserId == PizzaApiExtensions.GetUserId(HttpContext)).SingleOrDefaultAsync();
        // if (subscription != null)
        // {
        //     _ = TrackAndSendNotificationsAsync(order, subscription);
        // }

        return order.OrderId;
    }

    public async Task SubscribeToNotifications(NotificationSubscription subscription)
    {
        //var response = await httpClient.PutAsJsonAsync("notifications/subscribe", subscription);
        //response.EnsureSuccessStatusCode();
    }
}
