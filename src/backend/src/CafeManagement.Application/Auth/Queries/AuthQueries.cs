using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Common;
using FluentResults;
using MediatR;

namespace CafeManagement.Application.Auth.Queries;

public record GetCurrentUserQuery : IRequest<Result<UserDto>>;
