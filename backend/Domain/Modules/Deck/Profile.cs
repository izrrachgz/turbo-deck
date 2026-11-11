using Domain.Entities;

namespace Domain.Modules.Deck;

public sealed class Profile : BaseEntity
{
    public required string Name { get; set; }

    public int SortOrder { get; set; }

    public int SelectedPageNumber { get; set; } = 1;
}
