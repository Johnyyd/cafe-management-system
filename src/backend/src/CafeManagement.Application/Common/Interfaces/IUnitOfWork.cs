using System.Threading;
using System.Threading.Tasks;

namespace CafeManagement.Application.Common.Interfaces;

/// <summary>
/// Unit of Work abstraction for managing database transactions.
/// This allows the Application layer to use transactions without depending on Infrastructure.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task StartTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task AbortTransactionAsync(CancellationToken cancellationToken = default);
}