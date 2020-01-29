using OTAPI.Tile;
namespace FakeProvider
{
    public interface IProviderTile : ITile
    {
        INamedTileCollection Provider { get; }
    }
}
