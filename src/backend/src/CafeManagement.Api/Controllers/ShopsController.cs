using CafeManagement.Application.Common.Dtos;
using CafeManagement.Application.Shops.Commands;
using CafeManagement.Application.Shops.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace CafeManagement.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/shops")]
public class ShopsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ShopsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ObjectId), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateShop([FromBody] CreateShopCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return CreatedAtAction(nameof(GetShopById), new { id = result.Value }, result.Value);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ShopDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShopById(ObjectId id, CancellationToken cancellationToken)
    {
        var query = new GetShopByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        if (result.IsFailed)
            return NotFound(result.Errors);

        return Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ShopDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShops([FromQuery] GetShopsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return Ok(result.Value);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateShop(ObjectId id, [FromBody] UpdateShopCommand command, CancellationToken cancellationToken)
    {
        var updateCommand = command with { Id = id };
        var result = await _mediator.Send(updateCommand, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateShop(ObjectId id, CancellationToken cancellationToken)
    {
        var command = new DeactivateShopCommand(id);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
            return NotFound(result.Errors);

        return NoContent();
    }

    [HttpGet("{id}/hours")]
    [ProducesResponseType(typeof(IReadOnlyList<OperatingHoursDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOperatingHours(ObjectId id, CancellationToken cancellationToken)
    {
        var query = new GetShopOperatingHoursQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        if (result.IsFailed)
            return NotFound(result.Errors);

        return Ok(result.Value);
    }

    [HttpPut("{id}/hours")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOperatingHours(ObjectId id, [FromBody] UpdateOperatingHoursCommand command, CancellationToken cancellationToken)
    {
        var updateCommand = command with { ShopId = id };
        var result = await _mediator.Send(updateCommand, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return NoContent();
    }
}