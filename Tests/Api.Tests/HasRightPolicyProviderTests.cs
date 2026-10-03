using Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;

namespace Api.Tests;

public sealed class HasRightPolicyProviderTests
{
    [Fact]
    public async Task GetPolicy_WithRightName_RequiresAuthenticationAndTheRight()
    {
        var provider = new HasRightPolicyProvider(Options.Create(new AuthorizationOptions()));

        var policy = await provider.GetPolicyAsync("HasRight:Catalog.Products.Read");

        Assert.NotNull(policy);
        Assert.Contains(policy!.Requirements, requirement => requirement is DenyAnonymousAuthorizationRequirement);
        Assert.Contains(policy.Requirements, requirement =>
            requirement is HasRightRequirement { RightCode: "Catalog.Products.Read" });
    }

    [Fact]
    public async Task GetPolicy_WithNamedPolicy_UsesDefaultProvider()
    {
        var options = new AuthorizationOptions();
        options.AddPolicy("FeaturePolicy", policy => policy.RequireClaim("feature", "enabled"));
        var provider = new HasRightPolicyProvider(Options.Create(options));

        var policy = await provider.GetPolicyAsync("FeaturePolicy");

        Assert.NotNull(policy);
        Assert.Contains(policy!.Requirements, requirement =>
            requirement is ClaimsAuthorizationRequirement { ClaimType: "feature" });
    }
}
