namespace Api.Authorization;

public sealed class HasRightAttribute(string rightCode) : AuthorizeAttribute($"{HasRightPolicyProvider.PolicyPrefix}{rightCode}");
