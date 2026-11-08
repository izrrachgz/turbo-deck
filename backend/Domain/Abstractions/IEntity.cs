#nullable enable
namespace Domain.Abstractions
{
    /// <summary>
    /// Represents a persisted entity with a typed primary identifier.
    /// </summary>
    /// <typeparam name="TKey">Type of the entity primary identifier.</typeparam>
    public interface IEntity<TKey>
    {
        /// <summary>
        /// Primary identifier associated with the entity.
        /// </summary>
        TKey Id { get; set; }
    }
}
