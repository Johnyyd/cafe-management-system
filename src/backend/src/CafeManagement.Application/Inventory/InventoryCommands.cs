using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Application.Inventory.Responses;
using CafeManagement.Domain.Inventory;
using FluentResults;
using MediatR;
using MongoDB.Bson;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CafeManagement.Application.Inventory.Commands;

public record CreateInventoryItemCommand(
    ObjectId ShopId,
    string ItemName,
    string Unit,
    decimal Quantity,
    decimal ReorderLevel,
    ObjectId? SupplierId,
    ObjectId? CreatedBy
) : IRequest<Result<InventoryItemResponse>>;

public record UpdateInventoryItemCommand(
    ObjectId Id,
    string ItemName,
    string Unit,
    decimal ReorderLevel,
    ObjectId? SupplierId,
    ObjectId? UpdatedBy
) : IRequest<Result<InventoryItemResponse>>;

public record AdjustInventoryQuantityCommand(
    ObjectId Id,
    decimal QuantityChange,
    string Reason,
    ObjectId? AdjustedBy
) : IRequest<Result>;

public record SetInventoryQuantityCommand(
    ObjectId Id,
    decimal Quantity,
    ObjectId? UpdatedBy
) : IRequest<Result>;

public record DeactivateInventoryItemCommand(
    ObjectId Id,
    ObjectId? DeletedBy
) : IRequest<Result>;

public record DeleteInventoryItemCommand(
    ObjectId Id
) : IRequest<Result>;

