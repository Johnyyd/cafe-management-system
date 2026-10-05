using CafeManagement.Application.Common.Dtos;
using CafeManagement.Application.Menu.Commands;
using CafeManagement.Application.Menu.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace CafeManagement.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/menu")]
[Authorize]
public class MenuController : ControllerBase
{
    private readonly IMediator _mediator;

    public MenuController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ObjectId), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMenuItem([FromBody] CreateMenuItemCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return CreatedAtAction(nameof(GetMenuItemById), new { id = result.Value }, result.Value);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MenuItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMenuItemById(ObjectId id, CancellationToken cancellationToken)
    {
        var query = new GetMenuItemByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        if (result.IsFailed)
            return NotFound(result.Errors);

        return Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MenuItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenuItems([FromQuery] GetMenuItemsQuery query, CancellationToken cancellationToken)
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
    public async Task<IActionResult> UpdateMenuItem(ObjectId id, [FromBody] UpdateMenuItemCommand command, CancellationToken cancellationToken)
    {
        var updateCommand = command with { Id = id };
        var result = await _mediator.Send(updateCommand, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return NoContent();
    }

    [HttpPut("{id}/availability")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMenuItemAvailability(ObjectId id, [FromBody] UpdateMenuItemAvailabilityCommand command, CancellationToken cancellationToken)
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
    public async Task<IActionResult> DeactivateMenuItem(ObjectId id, CancellationToken cancellationToken)
    {
        var command = new DeactivateMenuItemCommand(id);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
            return NotFound(result.Errors);

        return NoContent();
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var query = new GetCategoriesQuery();
        var result = await _mediator.Send(query, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return Ok(result.Value);
    }
}