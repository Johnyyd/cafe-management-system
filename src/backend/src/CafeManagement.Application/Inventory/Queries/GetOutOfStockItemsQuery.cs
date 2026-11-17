using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Domain.Inventory;
using FluentResults;
using MediatR;
using MongoDB.Bson;
using CafeManagement.Application.Inventory.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace CafeManagement.Application.Inventory.Queries;

public record GetOutOfStockItemsQuery(ObjectId? ShopId = null) : IRequest<Result<IReadOnlyList<InventoryItemResponse>>>;
