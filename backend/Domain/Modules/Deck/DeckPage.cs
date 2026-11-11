using Domain.Entities;

namespace Domain.Modules.Deck;

public sealed class DeckPage : BaseEntity
{
    public long ProfileId { get; set; }

    public required string Name { get; set; }

    public int Position { get; set; }
}
