using Api.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Resources.HTTP.Catalog.GET;
using Shared.Resources.HTTP.Catalog.POST;
using Shared.Resources.HTTP.Catalog.PUT;
using Shared.Resources.HTTP.Common;
using Shared.Services.Catalog;

namespace Api.Controllers.Catalog;

[ApiController]
[ApiVersion("1.0")]
[Tags(OpenApiTagNames.Catalog)]
[Route("api/products")]
[Produces("application/json")]
public class ProductsController(IProductService service) : AuthenticatedController
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<GetProduct>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<GetProduct>>>> GetProducts(CancellationToken ct)
    {
        var products = await service.GetProducts(ct);

        return Ok(ApiResponse<List<GetProduct>>.Ok(products));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<GetProduct>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<GetProduct>>> GetProduct([FromRoute] Guid id, CancellationToken ct)
    {
        var product = await service.GetProduct(id, ct);

        return Ok(ApiResponse<GetProduct>.Ok(product));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<GetProduct>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<GetProduct>>> PostProduct([FromBody] PostProductRequest request, CancellationToken ct)
    {
        var product = await service.PostProduct(request, ct);

        return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, ApiResponse<GetProduct>.Created(product));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<GetProduct>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<GetProduct>>> PutProduct([FromRoute] Guid id, [FromBody] PutProductRequest request, CancellationToken ct)
    {
        var product = await service.PutProduct(id, request, ct);

        return Ok(ApiResponse<GetProduct>.Ok(product));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse>> DeleteProduct([FromRoute] Guid id, CancellationToken ct)
    {
        await service.DeleteProduct(id, ct);

        return Ok(ApiResponse.Ok("Product deleted."));
    }
}
