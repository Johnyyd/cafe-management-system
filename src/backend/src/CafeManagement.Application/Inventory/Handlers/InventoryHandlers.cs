using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Application.Common.Behaviors;
using CafeManagement.Application.Inventory.Commands;
using CafeManagement.Application.Inventory.Responses;
using CafeManagement.Application.Inventory.Queries;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Inventory;
using FluentResults;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CafeManagement.Application.Inventory.Handlers;

public class CreateInventoryItemHandler : IRequestHandler<CreateInventoryItemCommand, Result<InventoryItemResponse>>
{
    private readonly IInventoryItemRepository _inventoryItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateInventoryItemHandler(IInventoryItemRepository inventoryItemRepository, IUnitOfWork unitOfWork)
    {
        _inventoryItemRepository = inventoryItemRepository;
        _unitOfWork = unitOfWork;
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

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
    private readonly IUnitOfWork _unitOfWork;

    public UpdateInventoryItemHandler(IInventoryItemRepository inventoryItemRepository, IUnitOfWork unitOfWork)
    {
        _inventoryItemRepository = inventoryItemRepository;
        _unitOfWork = unitOfWork;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
    private readonly IUnitOfWork _unitOfWork;

    public AdjustInventoryQuantityHandler(IInventoryItemRepository inventoryItemRepository, IUnitOfWork unitOfWork)
    {
        _inventoryItemRepository = inventoryItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AdjustInventoryQuantityCommand request, CancellationToken cancellationToken)
    {
        var item = await _inventoryItemRepository.GetByIdAsync(request.Id);
        if (item == null)
            return Result.Fail("Inventory Item not found");

        var result = item.AdjustQuantity(
            request.QuantityChange,
            request.Reason,
            request.AdjustedBy);

        if (result.IsFailed)
            return Result.Fail(result.Errors);

        await _inventoryItemRepository.UpdateAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}

public class SetInventoryQuantityHandler : IRequestHandler<SetInventoryQuantityCommand, Result>
{
    private readonly IInventoryItemRepository _inventoryItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetInventoryQuantityHandler(IInventoryItemRepository inventoryItemRepository, IUnitOfWork unitOfWork)
    {
        _inventoryItemRepository = inventoryItemRepository;
        _unitOfWork = unitOfWork;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}

public class DeactivateInventoryItemHandler : IRequestHandler<DeactivateInventoryItemCommand, Result>
{
    private readonly IInventoryItemRepository _inventoryItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateInventoryItemHandler(IInventoryItemRepository inventoryItemRepository, IUnitOfWork unitOfWork)
    {
        _inventoryItemRepository = inventoryItemRepository;
        _unitOfWork = unitOfWork;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
        var items = await _inventoryItemRepository.GetByShopIdAsync(request.ShopId, cancellationToken);
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
        var items = await _inventoryItemRepository.GetLowStockItemsAsync(cancellationToken);
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
        var items = await _inventoryItemRepository.GetOutOfStockItemsAsync(cancellationToken);
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