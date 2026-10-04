using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Application.Inventory.Responses;
using CafeManagement.Domain.Inventory;
using FluentResults;
using MediatR;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CafeManagement.Application.Inventory.Queries;

public record GetInventoryItemsQuery(ObjectId ShopId) : IRequest<Result<IReadOnlyList<InventoryItemResponse>>>;