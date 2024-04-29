using BlazingPizza.Data;
using BlazingPizza.Server;
using Microsoft.EntityFrameworkCore;

namespace BlazingPizza.Components;

public class PizzaClient(PizzaStoreContext db)
{
    public Task<PizzaSpecial?> FindSpecial(int id)
        => db.Specials.SingleOrDefaultAsync(x => x.Id == id);

    public async Task<IReadOnlyList<PizzaSpecial>> GetSpecials()
        => await db.Specials.ToListAsync();

    public Task<List<Topping>> GetToppings()
        => db.Toppings.ToListAsync();
}
