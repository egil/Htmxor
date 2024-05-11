using System.Globalization;
using System.Net;
using Htmxor.TestAssets.Alba;

namespace Htmxor;

public class HtmxorComponentRequestHostTest : TestAppTestBase
{
	public HtmxorComponentRequestHostTest(TestAppFixture fixture) : base(fixture)
	{
	}

	[Theory]
	[InlineData(false, "boolValue", true)]
	[InlineData(false, "boolValue", false)]
	[InlineData(false, "datetimeValue", "2024-05-03T09:07:54.5004759+00:00")]
	[InlineData(false, "decimalValue", "-79228162514264337593543950335")]
	[InlineData(false, "decimalValue", "123.45")]
	[InlineData(false, "decimalValue", "79228162514264337593543950335")]
	[InlineData(false, "doubleValue", double.E)]
	[InlineData(false, "doubleValue", double.MinValue)]
	[InlineData(false, "doubleValue", double.MaxValue)]
	[InlineData(false, "floatValue", float.Epsilon)]
	[InlineData(false, "floatValue", float.MinValue)]
	[InlineData(false, "floatValue", float.MaxValue)]
	[InlineData(false, "intValue", 42)]
	[InlineData(false, "intValue", int.MinValue)]
	[InlineData(false, "intValue", int.MaxValue)]
	[InlineData(false, "longValue", 72036854775807)]
	[InlineData(false, "longValue", long.MinValue)]
	[InlineData(false, "longValue", long.MaxValue)]
	[InlineData(false, "guidValue", "e46fb987-6eac-4a1d-9744-ec68878a7a2e")]
	[InlineData(false, "guidValue", "00000000-0000-0000-0000-000000000000")]
	[InlineData(false, "stringValue", "foo-bar")]
	[InlineData(true, "boolValue", true)]
	[InlineData(true, "boolValue", false)]
	[InlineData(true, "datetimeValue", "2024-05-03T09:07:54.5004759+00:00")]
	[InlineData(true, "decimalValue", "-79228162514264337593543950335")]
	[InlineData(true, "decimalValue", "123.45")]
	[InlineData(true, "decimalValue", "79228162514264337593543950335")]
	[InlineData(true, "doubleValue", double.E)]
	[InlineData(true, "doubleValue", double.MinValue)]
	[InlineData(true, "doubleValue", double.MaxValue)]
	[InlineData(true, "floatValue", float.Epsilon)]
	[InlineData(true, "floatValue", float.MinValue)]
	[InlineData(true, "floatValue", float.MaxValue)]
	[InlineData(true, "intValue", 42)]
	[InlineData(true, "intValue", int.MinValue)]
	[InlineData(true, "intValue", int.MaxValue)]
	[InlineData(true, "longValue", 72036854775807)]
	[InlineData(true, "longValue", long.MinValue)]
	[InlineData(true, "longValue", long.MaxValue)]
	[InlineData(true, "guidValue", "e46fb987-6eac-4a1d-9744-ec68878a7a2e")]
	[InlineData(true, "guidValue", "00000000-0000-0000-0000-000000000000")]
	[InlineData(true, "stringValue", "foo-bar")]
	public async Task Route_values_in_component<T>(bool isHtmxRequest, string id, T value)
	{
		await Host.Scenario(s =>
		{
			var invarientValue = Convert.ToString(value, CultureInfo.InvariantCulture);
			s.Get.Url($"/AllRouteParams/{id}/{invarientValue}");
			s.WithHxHeaders(isHtmxRequest: isHtmxRequest);

			s.StatusCodeShouldBe(HttpStatusCode.OK);
			s.ContentShouldHaveElementsEqualTo(
				$"#{id}",
				$"<div id='{id}'>{invarientValue}</div>");
		});
	}
}
