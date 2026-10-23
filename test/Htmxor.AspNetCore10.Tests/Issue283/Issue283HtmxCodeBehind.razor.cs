using Htmxor;
namespace Htmxor.AspNetCore10.Issue283;
[HtmxRoute("/issue-283/htmx/codebehind", Methods = ["GET"])]
[HtmxLayout(typeof(Issue283OverrideLayout), Priority = 10)]
public partial class Issue283HtmxCodeBehind;
