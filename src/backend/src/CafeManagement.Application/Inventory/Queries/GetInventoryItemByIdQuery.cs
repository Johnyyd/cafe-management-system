using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Application.Inventory.Responses;
using CafeManagement.Domain.Inventory;
using FluentResults;
using MediatR;
using MongoDB.Bson;
using System.Threading;
using System.Threading.Tasks;

namespace CafeManagement.Application.Inventory.Queries;

public record GetInventoryItemByIdQuery(ObjectId Id) : IRequest<Result<InventoryItemResponse>>;