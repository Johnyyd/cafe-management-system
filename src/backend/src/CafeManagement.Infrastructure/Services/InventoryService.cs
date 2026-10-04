using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Inventory;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CafeManagement.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryItemRepository _inventoryItemRepository;

    public InventoryService(IInventoryItemRepository inventoryItemRepository)
    {
        _inventoryItemRepository = inventoryItemRepository;
    }

    public async Task<Result<Domain.Inventory.InventoryItem>> CreateAsync(ObjectId shopId, string itemName, string unit, decimal quantity, decimal reorderLevel, ObjectId? supplierId = null, ObjectId? createdBy = null)
    {
        var result = InventoryItem.Create(shopId, itemName, unit, quantity, reorderLevel, supplierId, createdBy);

        if (result.IsFailed)
            return Result.Fail<Domain.Inventory.InventoryItem>(result.Errors);

        var item = result.Value;
        await _inventoryItemRepository.AddAsync(item, CancellationToken.None);
        return Result.Ok(item);
    }

    public async Task<Result<Domain.Inventory.InventoryItem>> UpdateAsync(ObjectId id, string itemName, string unit, decimal reorderLevel, ObjectId? supplierId, ObjectId? updatedBy = null)
    {
        var existing = await _inventoryItemRepository.GetByIdAsync(id);
        if (existing == null)
            return Result.Fail<Domain.Inventory.InventoryItem>(DomainErrors.General.NotFound("Inventory Item", id));

        var result = existing.Update(itemName, unit, reorderLevel, supplierId, updatedBy);

        if (result.IsFailed)
            return Result.Fail<Domain.Inventory.InventoryItem>(result.Errors);

        await _inventoryItemRepository.UpdateAsync(existing, CancellationToken.None);
        return Result.Ok(existing);
    }

    public async Task<Result> AdjustQuantityAsync(ObjectId id, decimal quantityChange, string reason, ObjectId? adjustedBy = null)
    {
        var existing = await _inventoryItemRepository.GetByIdAsync(id);
        if (existing == null)
            return Result.Fail("Inventory Item Not Found");

        var result = existing.AdjustQuantity(quantityChange, reason, adjustedBy);

        if (result.IsFailed)
            return Result.Fail(result.Errors);

        await _inventoryItemRepository.UpdateAsync(existing, CancellationToken.None);
        return Result.Ok();
    }

    public async Task<Result> SetQuantityAsync(ObjectId id, decimal quantity, ObjectId? updatedBy = null)
    {
        var existing = await _inventoryItemRepository.GetByIdAsync(id);
        if (existing == null)
            return Result.Fail("Inventory Item Not Found");

        var result = existing.SetQuantity(quantity, updatedBy);

        if (result.IsFailed)
            return Result.Fail(result.Errors);

        await _inventoryItemRepository.UpdateAsync(existing, CancellationToken.None);
        return Result.Ok();
    }

    public async Task<Result> DeactivateAsync(ObjectId id, ObjectId? deletedBy = null)
    {
        var existing = await _inventoryItemRepository.GetByIdAsync(id);
        if (existing == null)
            return Result.Fail("Inventory Item Not Found");

        var result = existing.Deactivate(deletedBy);

        if (result.IsFailed)
            return Result.Fail(result.Errors);

        await _inventoryItemRepository.UpdateAsync(existing, CancellationToken.None);
        return Result.Ok();
    }

    public async Task<IReadOnlyList<Domain.Inventory.InventoryItem>> GetLowStockItemsAsync()
    {
        return await _inventoryItemRepository.GetLowStockItemsAsync();
    }

    public async Task<IReadOnlyList<Domain.Inventory.InventoryItem>> GetOutOfStockItemsAsync()
    {
        return await _inventoryItemRepository.GetOutOfStockItemsAsync();
    }

    public async Task<IReadOnlyList<string>> GetItemNamesAsync(ObjectId shopId)
    {
        return await _inventoryItemRepository.GetItemNamesAsync(shopId);
    }

    public async Task<Result<Domain.Inventory.InventoryItem>> GetByShopIdAndNameAsync(ObjectId shopId, string name)
    {
        var existing = await _inventoryItemRepository.GetByShopIdAndNameAsync(shopId, name);
        if (existing == null)
            return Result.Fail<Domain.Inventory.InventoryItem>(DomainErrors.General.NotFound("Inventory Item", new object[] { shopId, name }));

        return Result.Ok(existing);
    }
}