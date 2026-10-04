using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Domain.Inventory;
using FluentResults;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using CafeManagement.Application.Inventory.Responses;

namespace CafeManagement.Application.Inventory.Queries;

public record GetLowStockItemsQuery : IRequest<Result<IReadOnlyList<InventoryItemResponse>>>;