using MediatR;

namespace CafeManagement.Domain.Common.Events;

public interface IDomainEvent : INotification
{
    DateTime OccurredAt { get; }
    string EventType { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string EventType { get; init; } = string.Empty;

    protected DomainEvent()
    {
        EventType = GetType().Name;
    }
}