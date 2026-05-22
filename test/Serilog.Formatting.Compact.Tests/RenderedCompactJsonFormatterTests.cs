using Newtonsoft.Json.Linq;
using Serilog.Formatting.Compact.Tests.Support;
using System;
using System.Globalization;
using Xunit;


namespace Serilog.Formatting.Compact.Tests
{
    public class RenderedCompactJsonFormatterTests
    {
        JObject AssertValidJson(Action<ILogger> act)
        {
            return Assertions.AssertValidJson(new RenderedCompactJsonFormatter(), act);
        }

        [Fact]
        public void AnEmptyEventIsValidJson()
        {
            AssertValidJson(log => log.Information("No properties"));
        }

        [Fact]
        public void AMinimalEventIsValidJson()
        {
            var jobject = AssertValidJson(log => log.Information("One {Property}", 42));

            JToken m;
            Assert.True(jobject.TryGetValue("@m", out m));
            Assert.Equal("One 42", m.ToObject<string>());

            JToken i;
            Assert.True(jobject.TryGetValue("@i", out i));
            Assert.Equal(EventIdHash.Compute("One {Property}").ToString("x8"), i.ToObject<string>());

        }

        [Fact]
        public void MultiplePropertiesAreDelimited()
        {
            AssertValidJson(log => log.Information("Property {First} and {Second}", "One", "Two"));
        }

        [Fact]
        public void ExceptionsAreFormattedToValidJson()
        {
            AssertValidJson(log => log.Information(new DivideByZeroException(), "With exception"));
        }

        [Fact]
        public void ExceptionAndPropertiesAreValidJson()
        {
            AssertValidJson(log => log.Information(new DivideByZeroException(), "With exception and {Property}", 42));
        }

        [Fact]
        public void RenderingsAreValidJson()
        {
            AssertValidJson(log => log.Information("One {Rendering:x8}", 42));
        }

        [Fact]
        public void MultipleRenderingsAreDelimited()
        {
            AssertValidJson(log => log.Information("Rendering {First:x8} and {Second:x8}", 1, 2));
        }

        [Fact]
        public void AtPrefixedPropertyNamesAreEscaped()
        {
            // Not possible in message templates, but accepted this way
            var jobject = AssertValidJson(log => log.ForContext("@Mistake", 42)
                                                    .Information("Hello"));

            JToken val;
            Assert.True(jobject.TryGetValue("@@Mistake", out val));
            Assert.Equal(42, val.ToObject<int>());
        }

        [Fact]
        public void RespectsCustomFormatProviderForMessageRendering()
        {
            var frenchCulture = new CultureInfo("fr-FR");
            var formatter = new RenderedCompactJsonFormatter(null, frenchCulture);

            var jobject = Assertions.AssertValidJson(formatter, log => log.Information("Total: {Money:0.00}", 1.23));

            JToken m;
            Assert.True(jobject.TryGetValue("@m", out m));

            Assert.Equal("Total: 1,23", m.ToObject<string>());
        }

        [Fact]
        public void DefaultsToInvariantCultureWhenFormatProviderIsNull()
        {
            var formatter = new RenderedCompactJsonFormatter(null, null);

            var jobject = Assertions.AssertValidJson(formatter, log => log.Information("Total: {Money:0.00}", 1.23));

            JToken m;
            Assert.True(jobject.TryGetValue("@m", out m));

            // Should stay invariant (period) to match the v2 default
            Assert.Equal("Total: 1.23", m.ToObject<string>());
        }
    }
}
