public sealed class ConventionTests
{
    [Fact]
    public void Services_AreSealed()
    {
        var serviceAssembly = typeof(Shared.Services.Auth.AuthService).Assembly;
        var serviceTypes = serviceAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Service") && t is { IsClass: true, IsAbstract: false })
            .ToList();

        foreach (var type in serviceTypes)
            Assert.True(type.IsSealed, $"{type.Name} in {type.Namespace} must be sealed.");
    }

    [Fact]
    public void Services_NoAsyncSuffix_OnPublicMethods()
    {
        var serviceAssembly = typeof(Shared.Services.Auth.AuthService).Assembly;
        var serviceTypes = serviceAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Service") && t is { IsClass: true });

        foreach (var type in serviceTypes)
        {
            var methods = type.GetMethods()
                .Where(m => m.IsPublic && !m.IsSpecialName);
            foreach (var method in methods)
            {
                Assert.False(method.Name.EndsWith("Async"),
                    $"{type.Name}.{method.Name} must not end with 'Async'.");
            }
        }
    }

    [Fact]
    public void HttpModels_AreRecords()
    {
        var resourceAssembly = typeof(Shared.Resources.HTTP.Common.ApiResponse).Assembly;
        var httpTypes = resourceAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("HTTP") == true && t is { IsClass: true, IsAbstract: false })
            .ToList();

        foreach (var type in httpTypes)
        {
            Assert.True(type.IsClass && type.GetMethod("<Clone>$") is not null,
                $"{type.Name} in {type.Namespace} must be a record type.");
        }
    }

    [Fact]
    public void HttpModels_NoDtoSuffix()
    {
        var resourceAssembly = typeof(Shared.Resources.HTTP.Common.ApiResponse).Assembly;
        var httpTypes = resourceAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("HTTP") == true);

        foreach (var type in httpTypes)
            Assert.False(type.Name.EndsWith("Dto"),
                $"{type.Name} must not use 'Dto' suffix.");
    }

    [Fact]
    public void Controllers_NoTryCatch()
    {
        var apiAssembly = typeof(Api.Controllers.Auth.AuthController).Assembly;
        var controllerTypes = apiAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller") && !t.IsAbstract);

        foreach (var type in controllerTypes)
        {
            var sourceCode = File.ReadAllText(GetSourceFilePath(type));
            // Rough check — controllers should not have try/catch blocks
            var tryCount = System.Text.RegularExpressions.Regex.Matches(sourceCode, @"\btry\s*\{").Count;
            Assert.True(tryCount == 0,
                $"{type.Name} contains {tryCount} try blocks. Controllers must not use try/catch.");
        }
    }

    [Fact]
    public void TestNaming_FollowsConvention()
    {
        var testAssembly = typeof(ConventionTests).Assembly;
        var testMethods = testAssembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttributes(typeof(FactAttribute), true).Length > 0
                     || m.GetCustomAttributes(typeof(Xunit.TheoryAttribute), true).Length > 0);

        foreach (var method in testMethods)
        {
            Assert.Matches(@"^[A-Z][a-zA-Z]+_[A-Z][a-zA-Z]+_[A-Z][a-zA-Z]+$", method.Name);
        }
    }

    private static string GetSourceFilePath(Type type)
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var relativePath = type.FullName!.Split(',')[0].Replace('.', '/');
        return Path.Combine(basePath, "..", "..", "..", "..", relativePath + ".cs");
    }
}
