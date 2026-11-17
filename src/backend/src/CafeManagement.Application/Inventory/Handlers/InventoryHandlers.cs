using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Application.Inventory.Commands;
using CafeManagement.Application.Inventory.Responses;
using CafeManagement.Application.Inventory.Queries;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Inventory;
using CafeManagement.Domain.Inventory.Events;
using FluentResults;
using MediatR;
using MongoDB.Bson;

namespace CafeManagement.Application.Inventory.Handlers;

public class CreateInventoryItemHandler : IRequestHandler<CreateInventoryItemCommand, Result<InventoryItemResponse>>
{
    private readonly IInventoryItemRepository _inventoryItemRepository;

    public CreateInventoryItemHandler(IInventoryItemRepository inventoryItemRepository)
    {
        _inventoryItemRepository = inventoryItemRepository;
    }

    public async Task<Result<InventoryItemResponse>> Handle(CreateInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var result = InventoryItem.Create(
            request.ShopId,
            request.ItemName,
            request.Unit,
            request.Quantity,
            request.ReorderLevel,
            request.SupplierId,
            request.CreatedBy);

        if (result.IsFailed)
            return Result.Fail<InventoryItemResponse>(result.Errors);

        var item = result.Value;
        await _inventoryItemRepository.AddAsync(item, cancellationToken);

        var response = new InventoryItemResponse(
            item.Id,
            item.ShopId,
            item.ItemName,
            item.Unit,
            item.Quantity,
            item.ReorderLevel,
            item.SupplierId,
            item.IsLowStock(),
            item.IsOutOfStock());

        return Result.Ok(response);
    }
}

public class UpdateInventoryItemHandler : IRequestHandler<UpdateInventoryItemCommand, Result<InventoryItemResponse>>
{
    private readonly IInventoryItemRepository _inventoryItemRepository;

    public UpdateInventoryItemHandler(IInventoryItemRepository inventoryItemRepository)
    {
        _inventoryItemRepository = inventoryItemRepository;
    }

    public async Task<Result<InventoryItemResponse>> Handle(UpdateInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _inventoryItemRepository.GetByIdAsync(request.Id);
        if (item == null)
            return Result.Fail<InventoryItemResponse>(DomainErrors.General.NotFound("Inventory Item", request.Id));

        var result = item.Update(
            request.ItemName,
            request.Unit,
            request.ReorderLevel,
            request.SupplierId,
            request.UpdatedBy);

        if (result.IsFailed)
            return Result.Fail<InventoryItemResponse>(result.Errors);

        await _inventoryItemRepository.UpdateAsync(item, cancellationToken);

        var response = new InventoryItemResponse(
            item.Id,
            item.ShopId,
            item.ItemName,
            item.Unit,
            item.Quantity,
            item.ReorderLevel,
            item.SupplierId,
            item.IsLowStock(),
            item.IsOutOfStock());

        return Result.Ok(response);
    }
}

public class AdjustInventoryQuantityHandler : IRequestHandler<AdjustInventoryQuantityCommand, Result>
{
    private readonly IInventoryItemRepository _inventoryItemRepository;

    public AdjustInventoryQuantityHandler(IInventoryItemRepository inventoryItemRepository)
    {
        _inventoryItemRepository = inventoryItemRepository;
    }

    public async Task<Result> Handle(AdjustInventoryQuantityCommand request, CancellationToken cancellationToken)
    {
        // Use atomic MongoDB $inc operation to prevent race conditions
        var result = await _inventoryItemRepository.AdjustQuantityAtomicAsync(
            request.Id,
            request.QuantityChange,
            request.AdjustedBy,
            cancellationToken);

        if (result.IsFailed)
            return Result.Fail(result.Errors);

        // Raise domain event manually since we bypassed the domain entity
        var updatedItem = result.Value;
        updatedItem.AddDomainEvent(new InventoryQuantityAdjustedEvent(
            updatedItem.Id,
            updatedItem.ShopId,
            updatedItem.ItemName,
            updatedItem.Quantity - request.QuantityChange, // old quantity
            updatedItem.Quantity,                          // new quantity
            request.QuantityChange,
            request.Reason,
            request.AdjustedBy));

        return Result.Ok();
    }
}

public class SetInventoryQuantityHandler : IRequestHandler<SetInventoryQuantityCommand, Result>
{
    private readonly IInventoryItemRepository _inventoryItemRepository;

    public SetInventoryQuantityHandler(IInventoryItemRepository inventoryItemRepository)
    {
        _inventoryItemRepository = inventoryItemRepository;
    }

    public async Task<Result> Handle(SetInventoryQuantityCommand request, CancellationToken cancellationToken)
    {
        var item = await _inventoryItemRepository.GetByIdAsync(request.Id);
        if (item == null)
            return Result.Fail("Inventory Item not found");

        var result = item.SetQuantity(
            request.Quantity,
            request.UpdatedBy);

        if (result.IsFailed)
            return Result.Fail(result.Errors);

        await _inventoryItemRepository.UpdateAsync(item, cancellationToken);

        return Result.Ok();
    }
}

public class DeactivateInventoryItemHandler : IRequestHandler<DeactivateInventoryItemCommand, Result>
{
    private readonly IInventoryItemRepository _inventoryItemRepository;

    public DeactivateInventoryItemHandler(IInventoryItemRepository inventoryItemRepository)
    {
        _inventoryItemRepository = inventoryItemRepository;
    }

    public async Task<Result> Handle(DeactivateInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _inventoryItemRepository.GetByIdAsync(request.Id);
        if (item == null)
            return Result.Fail("Inventory Item not found");

        var result = item.Deactivate(
            request.DeletedBy);

        if (result.IsFailed)
            return Result.Fail(result.Errors);

        await _inventoryItemRepository.UpdateAsync(item, cancellationToken);

        return Result.Ok();
    }
}

public class DeleteInventoryItemHandler : IRequestHandler<DeleteInventoryItemCommand, Result>
{
    private readonly IInventoryItemRepository _inventoryItemRepository;

    public DeleteInventoryItemHandler(IInventoryItemRepository inventoryItemRepository)
    {
        _inventoryItemRepository = inventoryItemRepository;
    }

    public async Task<Result> Handle(DeleteInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _inventoryItemRepository.GetByIdAsync(request.Id, cancellationToken);
        if (item == null)
            return Result.Fail(DomainErrors.General.NotFound("Inventory Item", request.Id));

        await _inventoryItemRepository.DeleteAsync(request.Id, cancellationToken);

        return Result.Ok();
    }
}

public class GetInventoryItemsHandler : IRequestHandler<GetInventoryItemsQuery, Result<IReadOnlyList<InventoryItemResponse>>>
{
    private readonly IInventoryItemRepository _inventoryItemRepository;

    public GetInventoryItemsHandler(IInventoryItemRepository inventoryItemRepository)
    {
        _inventoryItemRepository = inventoryItemRepository;
    }

    public async Task<Result<IReadOnlyList<InventoryItemResponse>>> Handle(GetInventoryItemsQuery request, CancellationToken cancellationToken)
    {
        var items = request.ShopId == ObjectId.Empty
            ? await _inventoryItemRepository.ListAsync(cancellationToken)
            : await _inventoryItemRepository.GetByShopIdAsync(request.ShopId, cancellationToken);
        var list = items.Select(i => new InventoryItemResponse(
            i.Id,
            i.ShopId,
            i.ItemName,
            i.Unit,
            i.Quantity,
            i.ReorderLevel,
            i.SupplierId,
            i.IsLowStock(),
            i.IsOutOfStock())).ToList();

        return Result.Ok<IReadOnlyList<InventoryItemResponse>>(list);
    }
}

public class GetLowStockItemsHandler : IRequestHandler<GetLowStockItemsQuery, Result<IReadOnlyList<InventoryItemResponse>>>
{
    private readonly IInventoryItemRepository _inventoryItemRepository;

    public GetLowStockItemsHandler(IInventoryItemRepository inventoryItemRepository)
    {
        _inventoryItemRepository = inventoryItemRepository;
    }

    public async Task<Result<IReadOnlyList<InventoryItemResponse>>> Handle(GetLowStockItemsQuery request, CancellationToken cancellationToken)
    {
        var items = await _inventoryItemRepository.GetLowStockItemsAsync(request.ShopId, cancellationToken);
        var list = items.Select(i => new InventoryItemResponse(
            i.Id,
            i.ShopId,
            i.ItemName,
            i.Unit,
            i.Quantity,
            i.ReorderLevel,
            i.SupplierId,
            i.IsLowStock(),
            i.IsOutOfStock())).ToList();

        return Result.Ok<IReadOnlyList<InventoryItemResponse>>(list);
    }
}

public class GetOutOfStockItemsHandler : IRequestHandler<GetOutOfStockItemsQuery, Result<IReadOnlyList<InventoryItemResponse>>>
{
    private readonly IInventoryItemRepository _inventoryItemRepository;

    public GetOutOfStockItemsHandler(IInventoryItemRepository inventoryItemRepository)
    {
        _inventoryItemRepository = inventoryItemRepository;
    }

    public async Task<Result<IReadOnlyList<InventoryItemResponse>>> Handle(GetOutOfStockItemsQuery request, CancellationToken cancellationToken)
    {
        var items = await _inventoryItemRepository.GetOutOfStockItemsAsync(request.ShopId, cancellationToken);
        var list = items.Select(i => new InventoryItemResponse(
            i.Id,
            i.ShopId,
            i.ItemName,
            i.Unit,
            i.Quantity,
            i.ReorderLevel,
            i.SupplierId,
            i.IsLowStock(),
            i.IsOutOfStock())).ToList();

        return Result.Ok<IReadOnlyList<InventoryItemResponse>>(list);
    }
}

public class GetInventoryItemByShopIdAndNameHandler : IRequestHandler<GetInventoryItemByShopIdAndNameQuery, Result<InventoryItemResponse>>
{
    private readonly IInventoryItemRepository _inventoryItemRepository;

    public GetInventoryItemByShopIdAndNameHandler(IInventoryItemRepository inventoryItemRepository)
    {
        _inventoryItemRepository = inventoryItemRepository;
    }

    public async Task<Result<InventoryItemResponse>> Handle(GetInventoryItemByShopIdAndNameQuery request, CancellationToken cancellationToken)
    {
        var item = await _inventoryItemRepository.GetByShopIdAndNameAsync(request.ShopId, request.ItemName, cancellationToken);
        if (item == null)
            return Result.Fail<InventoryItemResponse>(DomainErrors.General.NotFound("Inventory Item", new object[] { request.ShopId, request.ItemName }));

        var response = new InventoryItemResponse(
            item.Id,
            item.ShopId,
            item.ItemName,
            item.Unit,
            item.Quantity,
            item.ReorderLevel,
            item.SupplierId,
            item.IsLowStock(),
            item.IsOutOfStock());

        return Result.Ok(response);
    }
}

public class GetInventoryItemByIdHandler : IRequestHandler<GetInventoryItemByIdQuery, Result<InventoryItemResponse>>
{
    private readonly IInventoryItemRepository _inventoryItemRepository;

    public GetInventoryItemByIdHandler(IInventoryItemRepository inventoryItemRepository)
    {
        _inventoryItemRepository = inventoryItemRepository;
    }

    public async Task<Result<InventoryItemResponse>> Handle(GetInventoryItemByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _inventoryItemRepository.GetByIdAsync(request.Id, cancellationToken);
        if (item == null)
            return Result.Fail<InventoryItemResponse>(DomainErrors.General.NotFound("Inventory Item", request.Id));

        var response = new InventoryItemResponse(
            item.Id,
            item.ShopId,
            item.ItemName,
            item.Unit,
            item.Quantity,
            item.ReorderLevel,
            item.SupplierId,
            item.IsLowStock(),
            item.IsOutOfStock());

        return Result.Ok(response);
    }
}

public class GetItemNamesHandler : IRequestHandler<GetItemNamesQuery, Result<IReadOnlyList<string>>>
{
    private readonly IInventoryItemRepository _inventoryItemRepository;

    public GetItemNamesHandler(IInventoryItemRepository inventoryItemRepository)
    {
        _inventoryItemRepository = inventoryItemRepository;
    }

    public async Task<Result<IReadOnlyList<string>>> Handle(GetItemNamesQuery request, CancellationToken cancellationToken)
    {
        var names = await _inventoryItemRepository.GetItemNamesAsync(request.ShopId, cancellationToken);
        return Result.Ok<IReadOnlyList<string>>(names);
    }
}