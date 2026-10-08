namespace Firmeza.Domain.Events;

public abstract class BaseEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();

    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
