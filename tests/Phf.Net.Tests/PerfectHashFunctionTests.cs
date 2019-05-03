using System.Linq;
using FluentAssertions;
using Xunit;

namespace Phf.Net.Tests
{
    public class PerfectHashFunctionTests    
    {
        [Fact]
        public void SmokeTest()
        {
            var strings = Enumerable.Range(0, 1_000_000).Select(x => $"str{x}").ToArray();
            
            var function = PerfectHashFunction.Create(strings, 4, 80, 31337);
            var ids = strings.Select(x => function.Evaluate(x)).Distinct().ToArray();

            ids.Length.Should().Be(strings.Length);
        }
    }
}