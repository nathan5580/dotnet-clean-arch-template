using Databases.Core;
using Microsoft.EntityFrameworkCore;

namespace Architecture.Tests;

public sealed class StructuralRulesTests
{
    private static readonly Assembly ApiAssembly = typeof(Api.Controllers.Auth.AuthController).Assembly;
    private static readonly Assembly ServicesAssembly = typeof(Shared.Services.Auth.AuthService).Assembly;
    private static readonly Assembly MappingAssembly = typeof(Shared.Mapping.Auth.AuthMapper).Assembly;
    private static readonly Assembly ResourcesAssembly = typeof(Shared.Resources.HTTP.Common.ApiResponse).Assembly;
    private static readonly Assembly JobsAssembly = typeof(Shared.Jobs.QuartzExtensions).Assembly;
    private static readonly Assembly CoreAssembly = typeof(Databases.Core.Entities.ApplicationUser).Assembly;

    private static string Describe(TestResult result) =>
        result.IsSuccessful ? "" : "Offending types: " + string.Join(", ", result.FailingTypeNames ?? []);

    private static void AssertSealed(Assembly assembly, string suffix)
    {

        var result = Types.InAssembly(assembly)
            .That().AreClasses().And().AreNotAbstract().And().HaveNameEndingWith(suffix)
            .Should().BeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));

    }

    [Fact]
    public void Services_Concrete_AreSealed() => AssertSealed(ServicesAssembly, "Service");

    [Fact]
    public void Mappers_Concrete_AreSealed() => AssertSealed(MappingAssembly, "Mapper");

    [Fact]
    public void Jobs_Concrete_AreSealed() => AssertSealed(JobsAssembly, "Job");

    [Fact]
    public void Validators_Concrete_AreSealed() => AssertSealed(ResourcesAssembly, "Validator");

    [Fact]
    public void AuthorizationHandlers_Concrete_AreSealed() => AssertSealed(ApiAssembly, "Handler");

    [Fact]
    public void Controllers_Concrete_AreNotSealed()
    {

        var result = Types.InAssembly(ApiAssembly)
            .That().AreClasses().And().AreNotAbstract().And().HaveNameEndingWith("Controller")
            .Should().NotBeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));

    }

    [Fact]
    public void Entities_All_AreNotSealed()
    {

        var entities = CoreAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Entities") == true && t is { IsClass: true, IsAbstract: false });

        foreach (var type in entities)
            Assert.False(type.IsSealed, $"{type.Name} must not be sealed — EF Core proxies require open entity types.");

    }

    [Fact]
    public void HttpModels_All_AreRecords()
    {

        var httpTypes = ResourcesAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("HTTP") == true && t is { IsClass: true, IsAbstract: false });

        foreach (var type in httpTypes)
            Assert.True(type.GetMethod("<Clone>$") is not null, $"{type.FullName} must be a record.");

    }

    [Fact]
    public void Types_None_UseDtoSuffix()
    {

        foreach (var assembly in new[] { ApiAssembly, ServicesAssembly, ResourcesAssembly, MappingAssembly })
            foreach (var type in assembly.GetTypes().Where(t => !t.Name.StartsWith('<')))
                Assert.False(type.Name.EndsWith("Dto"), $"{type.FullName} must not use the 'Dto' suffix.");

    }

    [Fact]
    public void Databases_DoNotDependOn_ApiOrServices()
    {

        var result = Types.InAssembly(CoreAssembly)
            .Should().NotHaveDependencyOnAny("Api", "Shared.Services")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));

    }

    [Fact]
    public void SharedLibraries_DoNotDependOn_Api()
    {

        foreach (var assembly in new[] { ServicesAssembly, MappingAssembly, ResourcesAssembly })
        {
            var result = Types.InAssembly(assembly).Should().NotHaveDependencyOn("Api").GetResult();
            Assert.True(result.IsSuccessful, Describe(result));
        }

    }

    [Fact]
    public void ServiceAndControllerMethods_Have_NoAsyncSuffix()
    {

        var types = ServicesAssembly.GetTypes().Where(t => t.Name.EndsWith("Service") && t.IsClass)
            .Concat(ApiAssembly.GetTypes().Where(t => t.Name.EndsWith("Controller") && t.IsClass));

        foreach (var type in types)
            foreach (var method in type.GetMethods().Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType == type))
                Assert.False(method.Name.EndsWith("Async"), $"{type.Name}.{method.Name} must not end with 'Async'.");

    }

    [Fact]
    public void AsyncMethods_CancellationToken_IsNamedCtAndLast()
    {

        var types = ServicesAssembly.GetTypes().Where(t => t.IsClass)
            .Concat(ApiAssembly.GetTypes().Where(t => t.Name.EndsWith("Controller")));

        foreach (var type in types)
            foreach (var method in type.GetMethods().Where(m => m.DeclaringType == type))
            {
                var parameters = method.GetParameters();
                if (parameters.All(p => p.ParameterType != typeof(CancellationToken)))
                    continue;

                var last = parameters[^1];
                Assert.True(last.ParameterType == typeof(CancellationToken),
                    $"{type.Name}.{method.Name}: CancellationToken must be the last parameter.");
                Assert.Equal("ct", last.Name);
            }

    }

    [Fact]
    public void EntityEnumProperties_Use_StringConversion()
    {

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("arch-tests")
            .Options;

        using var context = new AppDbContext(options);

        foreach (var entity in context.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
            {
                var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (!clrType.IsEnum)
                    continue;

                var converter = property.GetValueConverter();
                Assert.True(converter is not null && converter.ProviderClrType == typeof(string),
                    $"{entity.ClrType.Name}.{property.Name} is an enum and must use .HasConversion<string>().");
            }

    }
}
