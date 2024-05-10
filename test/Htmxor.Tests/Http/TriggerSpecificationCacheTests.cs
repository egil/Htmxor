using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using FluentAssertions.Primitives;

namespace Htmxor.Http;

public class TriggerSpecificationCacheTests
{
	[Fact]
	public void TriggerSpecificationCache_Revealed_ReturnsCorrectJson()
	{
		// Arrange
		var cache = new TriggerSpecificationCache(
			Trigger.Revealed()
		);

		// Act
		var json = JsonSerializer.Serialize(new { triggerSpecsCache = cache },
			new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });

		// Assert
		json.Should().BeJsonSemanticallyEqualTo(
			"{\"triggerSpecsCache\":{\"revealed\":[{\"trigger\":\"revealed\"}]}}"
		);
	}

	[Fact]
	public void TriggerSpecificationCache_OnEventWithFrom_ReturnsCorrectJson()
	{
		// Arrange
		var cache = new TriggerSpecificationCache(
			Trigger.OnEvent("newContact").From("body")
		);

		// Act
		var json = JsonSerializer.Serialize(new { triggerSpecsCache = cache },
			new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });

		// Assert
		json.Should().BeJsonSemanticallyEqualTo(
			"{\"triggerSpecsCache\":{\"newContact from:body\":[{\"trigger\":\"newContact\",\"from\":\"body\"}]}}"
		);
	}

	[Fact]
	public void TriggerSpecificationCache_OnEventWithMultipleModifiers_ReturnsCorrectJson()
	{
		// Arrange
		var cache = new TriggerSpecificationCache(
			Trigger.OnEvent("keyup").Changed().Delay(TimeSpan.FromMilliseconds(500))
				.Or()
				.OnEvent("mouseenter").Once()
		);

		// Act
		var json = JsonSerializer.Serialize(new { triggerSpecsCache = cache },
			new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });

		// Assert
		json.Should().BeJsonSemanticallyEqualTo(
			"{\"triggerSpecsCache\":{\"keyup changed delay:500ms, mouseenter once\":[{\"trigger\":\"keyup\",\"changed\":true,\"delay\":500},{\"trigger\":\"mouseenter\",\"once\":true}]}}"
		);
	}

	[Fact]
	public void TriggerSpecificationCache_EveryAndOnEvent_ReturnsCorrectJson()
	{
		// Arrange
		var cache = new TriggerSpecificationCache(
			Trigger.Every(TimeSpan.FromSeconds(30))
				.Or()
				.OnEvent("newContact").From("closest (form input)")
		);

		// Act
		var json = JsonSerializer.Serialize(new { triggerSpecsCache = cache },
			new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });

		// Assert
		json.Should().BeJsonSemanticallyEqualTo(
			"{\"triggerSpecsCache\":{\"every 30s, newContact from:closest (form input)\":[{\"trigger\":\"every\",\"pollInterval\":30000},{\"trigger\":\"newContact\",\"from\":\"closest (form input)\"}]}}"
		);
	}

	[Fact]
	public void TriggerSpecificationCache_ComplexTriggers_ReturnsCorrectJson()
	{
		// Arrange
		var cache = new TriggerSpecificationCache(
			Trigger.Revealed(),
			Trigger.OnEvent("newContact").From("body"),
			Trigger.OnEvent("keyup").Changed().Delay(TimeSpan.FromMilliseconds(500))
				.Or()
				.OnEvent("mouseenter").Once(),
			Trigger.Every(TimeSpan.FromSeconds(30))
				.Or()
				.OnEvent("newContact").From("closest (form input)")
		);

		// Act
		var json = JsonSerializer.Serialize(new { triggerSpecsCache = cache },
			new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });

		// Assert
		json.Should().BeJsonSemanticallyEqualTo(
			"{\"triggerSpecsCache\":{\"revealed\":[{\"trigger\":\"revealed\"}],\"newContact from:body\":[{\"trigger\":\"newContact\",\"from\":\"body\"}],\"keyup changed delay:500ms, mouseenter once\":[{\"trigger\":\"keyup\",\"changed\":true,\"delay\":500},{\"trigger\":\"mouseenter\",\"once\":true}],\"every 30s, newContact from:closest (form input)\":[{\"trigger\":\"every\",\"pollInterval\":30000},{\"trigger\":\"newContact\",\"from\":\"closest (form input)\"}]}}"
		);
	}
}
