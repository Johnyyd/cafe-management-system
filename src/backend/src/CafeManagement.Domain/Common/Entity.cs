using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CafeManagement.Domain.Common;

public abstract class Entity<TId> where TId : notnull
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public TId Id { get; protected set; } = default!;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;

    [BsonElement("deletedAt")]
    public DateTime? DeletedAt { get; protected set; }

    [BsonElement("createdBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId? CreatedBy { get; protected set; }

    [BsonElement("updatedBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId? UpdatedBy { get; protected set; }

    protected Entity() { }

    protected Entity(TId id)
    {
        Id = id;
    }

    public void MarkAsDeleted(ObjectId? deletedBy = null)
    {
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        if (deletedBy.HasValue)
        {
            UpdatedBy = deletedBy;
        }
    }

    public void SetAuditInfo(ObjectId? createdBy = null, ObjectId? updatedBy = null)
    {
        if (createdBy.HasValue)
        {
            CreatedBy = createdBy;
        }
        if (updatedBy.HasValue)
        {
            UpdatedBy = updatedBy;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public bool IsDeleted => DeletedAt.HasValue;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (Id.Equals(default(TId)) || other.Id.Equals(default(TId)))
            return false;

        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity<TId>? a, Entity<TId>? b)
    {
        if (a is null && b is null)
            return true;
        if (a is null || b is null)
            return false;
        return a.Equals(b);
    }

    public static bool operator !=(Entity<TId>? a, Entity<TId>? b) => !(a == b);
}