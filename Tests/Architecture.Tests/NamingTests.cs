using System.Text.RegularExpressions;

namespace Architecture.Tests;

public sealed class NamingTests
{
    [Fact]
    public void TestMethods_All_FollowSubjectScenarioExpected()
    {

        var testAssemblies = new[]
        {
            typeof(NamingTests).Assembly,
            typeof(Shared.Tests.AuthMapperTests).Assembly,
            typeof(Api.Tests.AuthControllerTests).Assembly,
        };

        foreach (var assembly in testAssemblies)
        {
            var methods = assembly.GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttributes(typeof(FactAttribute), true).Length > 0
                         || m.GetCustomAttributes(typeof(TheoryAttribute), true).Length > 0);

            foreach (var method in methods)
                Assert.Matches(@"^[A-Z]\w+_[A-Z]\w+_[A-Z]\w+$", method.Name);
        }

    }
}
