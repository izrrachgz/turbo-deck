#nullable enable
namespace Domain.Abstractions
{
    /// <summary>
    /// Represents an entity that can be soft-deleted.
    /// </summary>
    public interface ISoftDeletableEntity
    {
        /// <summary>
        /// Date and time when the entity was soft-deleted, when applicable.
        /// </summary>
        DateTime? DeletedUtc { get; set; }
    }
}
