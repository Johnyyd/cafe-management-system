using CafeManagement.Application.Common.Dtos;
using CafeManagement.Application.Shifts.Commands;
using CafeManagement.Application.Shifts.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Threading;
using System.Threading.Tasks;

namespace CafeManagement.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/shifts")]
[Authorize]
public class ShiftsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ShiftsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ShiftDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShifts([FromQuery] GetShiftsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ObjectId), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateShift([FromBody] CreateShiftCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("{id}/start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartShift(ObjectId id, CancellationToken cancellationToken)
    {
        var command = new StartShiftCommand(id);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return NoContent();
    }

    [HttpPost("{id}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteShift(ObjectId id, CancellationToken cancellationToken)
    {
        var command = new CompleteShiftCommand(id);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return NoContent();
    }

    [HttpPost("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelShift(ObjectId id, [FromBody] CancelShiftRequest? request, CancellationToken cancellationToken)
    {
        var command = new CancelShiftCommand(id, request?.Reason);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return NoContent();
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateShift(ObjectId id, [FromBody] UpdateShiftRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateShiftCommand(id, request.StaffId, request.StartTime, request.EndTime, request.Notes);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteShift(ObjectId id, CancellationToken cancellationToken)
    {
        var command = new DeleteShiftCommand(id);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
            return NotFound(result.Errors);

        return NoContent();
    }
}

public record CancelShiftRequest(string? Reason);
public record UpdateShiftRequest(ObjectId StaffId, DateTime StartTime, DateTime EndTime, string? Notes);
