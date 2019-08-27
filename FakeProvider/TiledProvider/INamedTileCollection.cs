#region Using
using OTAPI.Tile;
using System;
#endregion
namespace FakeProvider
{
    public interface INamedTileCollection : ITileCollection, IDisposable
    {
        TileProviderCollection Parent { get; }
        short Index { get; }
        string Name { get; }
        int X { get; set; }
        int Y { get; set; }
        int Layer { get; }
        bool Enabled { get; }

        void SetupParent(TileProviderCollection parent, short index);
        void Apply();
        void Draw(bool section=false);
    }
}