#nullable enable
namespace Domain.Abstractions
{
    /// <summary>
    /// Represents an entity that tracks creation and modification timestamps.
    /// </summary>
    public interface IAuditableEntity
    {
        /// <summary>
        /// Date and time when the entity was created.
        /// </summary>
        DateTime CreatedUtc { get; set; }

        /// <summary>
        /// Date and time when the entity was last modified.
        /// </summary>
        DateTime ModifiedUtc { get; set; }
    }
}
