using BlazingPizza.Data;
using BlazingPizza.Server;
using Microsoft.EntityFrameworkCore;

namespace BlazingPizza.Components;

public class PizzaClient(PizzaStoreContext db)
{
    public Task<List<PizzaSpecial>> GetSpecials()
        => db.Specials.ToListAsync();

    public Task<List<Topping>> GetToppings()
        => db.Toppings.ToListAsync();
}
