namespace BlazingPizza.Components.Shared;

public record class OrderFormData
{
    public List<ConfigurePizzaFormData>? Pizzas { get; set; }
}