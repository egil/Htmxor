using System.ComponentModel.DataAnnotations;

namespace BlazingPizza.Components.Shared;

public record class ConfigurePizzaFormData
{
	[Required]
	public int SpecialId { get; set; }

	[Required, Range(Pizza.MinimumSize, Pizza.MaximumSize)]
	public int Size { get; set; }

	public List<int>? Toppings { get; set; }

	public int SelectTopping { get; set; }

	public int RemoveTopping { get; set; }
}