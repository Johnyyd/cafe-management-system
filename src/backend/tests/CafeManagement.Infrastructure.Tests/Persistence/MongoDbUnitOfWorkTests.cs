using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Infrastructure.Persistence;
using FluentAssertions;
using MongoDB.Driver;
using Moq;
using System.Threading;
using Xunit;

namespace CafeManagement.Infrastructure.Tests.Persistence;

public class MongoDbUnitOfWorkTests
{
    private readonly Mock<IMongoDbContext> _contextMock;
    private readonly Mock<IClientSessionHandle> _sessionMock;

    public MongoDbUnitOfWorkTests()
    {
        _contextMock = new Mock<IMongoDbContext>();
        _sessionMock = new Mock<IClientSessionHandle>();

        _contextMock.Setup(c => c.StartSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_sessionMock.Object);
    }

    [Fact]
    public async Task StartTransactionAsync_StartsTransaction()
    {
        // Arrange
        var unitOfWork = new MongoDbUnitOfWork(_contextMock.Object);
        _contextMock.Setup(c => c.StartTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await unitOfWork.StartTransactionAsync();

        // Assert
        _contextMock.Verify(c => c.StartTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartTransactionAsync_DelegatesToContext()
    {
        // Arrange
        var unitOfWork = new MongoDbUnitOfWork(_contextMock.Object);
        _contextMock.Setup(c => c.StartTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await unitOfWork.StartTransactionAsync();

        // Assert - UnitOfWork delegates to context, which handles session creation internally
        _contextMock.Verify(c => c.StartTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CommitTransactionAsync_CommitsTransaction()
    {
        // Arrange
        var unitOfWork = new MongoDbUnitOfWork(_contextMock.Object);
        _contextMock.Setup(c => c.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await unitOfWork.CommitTransactionAsync();

        // Assert
        _contextMock.Verify(c => c.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AbortTransactionAsync_AbortsTransaction()
    {
        // Arrange
        var unitOfWork = new MongoDbUnitOfWork(_contextMock.Object);
        _contextMock.Setup(c => c.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await unitOfWork.AbortTransactionAsync();

        // Assert
        _contextMock.Verify(c => c.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var unitOfWork = new MongoDbUnitOfWork(_contextMock.Object);

        // Act & Assert
        var act = () => unitOfWork.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        // Arrange
        var unitOfWork = new MongoDbUnitOfWork(_contextMock.Object);

        // Act
        unitOfWork.Dispose();
        unitOfWork.Dispose(); // Second call should not throw

        // Assert - no exception thrown
    }
}