using System.Text.Json;
using Htmxor.Serialization;
using Htmxor.TestAssets.FluentAssertions;

namespace Htmxor.Configuration;

public class HtmxConfigTest
{
	[Fact]
	public void HtmxConfig_serializer()
	{
		var config = new HtmxConfig
		{
			AddedClass = "added-class",
			AllowEval = true,
			AllowScriptTags = true,
			AttributesToSettle = ["attr1", "attr2"],
			DefaultFocusScroll = true,
			DefaultSettleDelay = TimeSpan.FromHours(1),
			DefaultSwapDelay = TimeSpan.FromMinutes(1),
			DefaultSwapStyle = SwapStyle.beforebegin,
			DisableSelector = "disable-selector",
			GetCacheBusterParam = true,
			GlobalViewTransitions = true,
			HistoryCacheSize = 1234,
			HistoryEnabled = true,
			IgnoreTitle = true,
			IncludeIndicatorStyles = true,
			IndicatorClass = "indicator-class",
			InlineScriptNonce = "inline-script-nonce",
			MethodsThatUseUrlParams = ["GET", "POST", "DELETE"],
			RefreshOnHistoryMiss = true,
			RequestClass = "request-class",
			ScrollBehavior = ScrollBehavior.smooth,
			ScrollIntoViewOnBoost = true,
			SelfRequestsOnly = true,
			SettlingClass = "settling-class",
			SwappingClass = "swapping-class",
			Timeout = TimeSpan.FromSeconds(30),
			UseTemplateFragments = true,
			WithCredentials = true,
			WsBinaryType = "ws-binary-type",
			WsReconnectDelay = "full-jitter",
			TriggerSpecsCache = [Trigger.Revealed()],
		};
		var json = JsonSerializer.Serialize(config, HtmxorJsonSerializerContext.Default.HtmxConfig);

		json.Should().BeJsonSemanticallyEqualTo("""
            {
                "addedClass": "added-class",
                "allowEval": true,
                "allowScriptTags": true,
                "attributesToSettle": [
                    "attr1",
                    "attr2"
                ],
                "defaultFocusScroll": true,
                "defaultSwapStyle": "beforebegin",
                "defaultSwapDelay": 60000,
                "defaultSettleDelay": 3600000,
                "disableSelector": "disable-selector",
                "getCacheBusterParam": true,
                "globalViewTransitions": true,
                "historyCacheSize": 1234,
                "historyEnabled": true,
                "ignoreTitle": true,
                "includeIndicatorStyles": true,
                "indicatorClass": "indicator-class",
                "inlineScriptNonce": "inline-script-nonce",
                "methodsThatUseUrlParams": [
                    "GET",
                    "POST",
                    "DELETE"
                ],
                "refreshOnHistoryMiss": true,
                "requestClass": "request-class",
                "scrollBehavior": "smooth",
                "scrollIntoViewOnBoost": true,
                "selfRequestsOnly": true,
                "settlingClass": "settling-class",
                "swappingClass": "swapping-class",
                "timeout": 30000,
                "useTemplateFragments": true,
                "withCredentials": true,
                "wsBinaryType": "ws-binary-type",
                "wsReconnectDelay": "full-jitter",
                "triggerSpecsCache": {
                    "revealed": [
                        {
                          "trigger": "revealed"
                        }
                    ]
                }
            }
            """);
	}

	[Fact]
	public void HtmxConfig_default_swap_style_serializer()
	{
		var config = new HtmxConfig
		{
			DefaultSwapStyle = SwapStyle.Default,
		};

		config.DefaultSwapStyle.Should().BeNull();
	}

	[Fact]
	public void TriggerSpecificationCache_Revealed_ReturnsCorrectJson()
	{
		var config = new HtmxConfig
		{
			TriggerSpecsCache = [Trigger.Revealed()],
		};
		var json = JsonSerializer.Serialize(config, HtmxorJsonSerializerContext.Default.HtmxConfig);

		json.Should()
			.BeJsonSemanticallyEqualTo("""
            {
                "selfRequestsOnly": true,
                "triggerSpecsCache": {
                    "revealed": [
                        {
                          "trigger": "revealed"
                        }
                    ]
                }
            }
            """);
	}

	[Fact]
	public void TriggerSpecificationCache_OnEventWithFrom_ReturnsCorrectJson()
	{
		var config = new HtmxConfig
		{
			TriggerSpecsCache = [Trigger.OnEvent("newContact").From("body")],
		};
		var json = JsonSerializer.Serialize(config, HtmxorJsonSerializerContext.Default.HtmxConfig);

		json.Should()
			.BeJsonSemanticallyEqualTo("""
            {
                "selfRequestsOnly": true,
                "triggerSpecsCache": {
                    "newContact from:body": [
                        {
                            "trigger": "newContact",
                            "from": "body"
                        }
                    ]
                }
            }
            """);
	}


	[Fact]
	public void TriggerSpecificationCache_EveryAndOnEvent_ReturnsCorrectJson()
	{
		var config = new HtmxConfig
		{
			TriggerSpecsCache = [
				Trigger
					.Every(TimeSpan.FromSeconds(30))
					.Or()
					.OnEvent("newContact")
					.From("closest (form input)")]
		};
		var json = JsonSerializer.Serialize(config, HtmxorJsonSerializerContext.Default.HtmxConfig);

		json.Should()
			.BeJsonSemanticallyEqualTo("""
            {
                "selfRequestsOnly": true,
                "triggerSpecsCache": {
                    "every 30s, newContact from:closest (form input)": [
                        {
                            "trigger": "every",
                            "pollInterval": 30000
                        },
                        {
                            "trigger": "newContact",
                            "from": "closest (form input)"
                        }
                    ]
                }
            }
            """);
	}

	[Fact]
	public void TriggerSpecificationCache_ComplexTriggers_ReturnsCorrectJson()
	{
		var config = new HtmxConfig
		{
			TriggerSpecsCache = [
				Trigger.Revealed(),
				Trigger.OnEvent("newContact").From("body"),
				Trigger.OnEvent("keyup").Changed().Delay(TimeSpan.FromMilliseconds(500))
					.Or()
					.OnEvent("mouseenter").Once(),
				Trigger.Every(TimeSpan.FromSeconds(30))
					.Or()
					.OnEvent("newContact").From("closest (form input)")]
		};
		var json = JsonSerializer.Serialize(config, HtmxorJsonSerializerContext.Default.HtmxConfig);

		json.Should()
			.BeJsonSemanticallyEqualTo("""
            {
                "selfRequestsOnly": true,
                "triggerSpecsCache": {
                    "revealed": [
                        {
                            "trigger": "revealed"
                        }
                    ],
                    "newContact from:body": [
                        {
                            "trigger": "newContact",
                            "from": "body"
                        }
                    ],
                    "keyup changed delay:500ms, mouseenter once": [
                        {
                            "trigger": "keyup",
                            "changed": true,
                            "delay": 500
                        },
                        {
                            "trigger": "mouseenter",
                            "once": true
                        }
                    ],
                    "every 30s, newContact from:closest (form input)": [
                        {
                            "trigger": "every",
                            "pollInterval": 30000
                        },
                        {
                            "trigger": "newContact",
                            "from": "closest (form input)"
                        }
                    ]
                }
            }
            """);
	}
}

