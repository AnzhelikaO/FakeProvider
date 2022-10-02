using Terraria;
namespace FakeProvider
{
    public interface IProviderTile : ITile
    {
        INamedTileCollection Provider { get; }
    }
}
