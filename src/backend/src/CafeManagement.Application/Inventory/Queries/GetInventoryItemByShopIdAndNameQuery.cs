using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Domain.Inventory;
using FluentResults;
using MediatR;
using CafeManagement.Application.Inventory.Responses;
using MongoDB.Bson;
using System.Threading;
using System.Threading.Tasks;

namespace CafeManagement.Application.Inventory.Queries;

public record GetInventoryItemByShopIdAndNameQuery(ObjectId ShopId, string ItemName) : IRequest<Result<InventoryItemResponse>>;
