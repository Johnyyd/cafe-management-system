using CafeManagement.Application.Common.Dtos;
using CafeManagement.Application.Orders.Commands;
using CafeManagement.Application.Orders.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace CafeManagement.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orders")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ObjectId), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return CreatedAtAction(nameof(GetOrderById), new { id = result.Value }, result.Value);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(ObjectId id, CancellationToken cancellationToken)
    {
        var query = new GetOrderByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        if (result.IsFailed)
            return NotFound(result.Errors);

        return Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders([FromQuery] GetOrdersQuery query, CancellationToken cancellationToken)
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
    public async Task<IActionResult> UpdateOrder(ObjectId id, [FromBody] UpdateOrderCommand command, CancellationToken cancellationToken)
    {
        var updateCommand = command with { Id = id };
        var result = await _mediator.Send(updateCommand, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return NoContent();
    }

    [HttpPost("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrder(ObjectId id, CancellationToken cancellationToken)
    {
        var command = new CancelOrderCommand(id);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return NoContent();
    }

    [HttpPost("{id}/payment")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessPayment(ObjectId id, [FromBody] ProcessPaymentCommand command, CancellationToken cancellationToken)
    {
        var updateCommand = command with { Id = id };
        var result = await _mediator.Send(updateCommand, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return NoContent();
    }

    [HttpPut("{id}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrderStatus(ObjectId id, [FromBody] UpdateOrderStatusCommand command, CancellationToken cancellationToken)
    {
        var updateCommand = command with { Id = id };
        var result = await _mediator.Send(updateCommand, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return NoContent();
    }

    [HttpPost("{id}/items")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddOrderItem(ObjectId id, [FromBody] AddOrderItemCommand command, CancellationToken cancellationToken)
    {
        var updateCommand = command with { OrderId = id };
        var result = await _mediator.Send(updateCommand, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return NoContent();
    }

    [HttpPut("{id}/items/{itemId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrderItem(ObjectId id, ObjectId itemId, [FromBody] UpdateOrderItemCommand command, CancellationToken cancellationToken)
    {
        var updateCommand = command with { OrderId = id, OrderItemId = itemId };
        var result = await _mediator.Send(updateCommand, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return NoContent();
    }

    [HttpDelete("{id}/items/{itemId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveOrderItem(ObjectId id, ObjectId itemId, CancellationToken cancellationToken)
    {
        var command = new RemoveOrderItemCommand(id, itemId);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
            return BadRequest(result.Errors);

        return NoContent();
    }

    [HttpGet("{id}/receipt")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderReceipt(ObjectId id, CancellationToken cancellationToken)
    {
        var query = new GetOrderReceiptQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        if (result.IsFailed)
            return NotFound(result.Errors);

        return Ok(result.Value);
    }
}