using System.Linq;
using FluentAssertions;
using Xunit;

namespace Phf.Net.Tests
{
    public class PerfectHashFunctionTests    
    {
        [Theory, CombinatorialData]
        public void SmokeTest(
            [CombinatorialValues(1, 10, 1_000, 31_337, 1_000_000)]int count,
            [CombinatorialValues(80, 100)] uint alpha,
            bool noDivision)
        {
            var strings = Enumerable.Range(0, count).Select(x => $"str{x}").ToArray();
            var settings = new PhfSettings {Alpha = alpha, NoDivision = noDivision, ItemsPerBucket = 4, Seed = 31337};
            
            var function = PerfectHashFunction.Create(strings, settings);
            var ids = strings.Select(x => function.Evaluate(x)).Distinct().ToArray();          

            ids.Length.Should().Be(strings.Length);
            ids.All(x => x < function.OutputArraySize).Should().BeTrue();
        }
    }
}