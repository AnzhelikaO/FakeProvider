#region Using
using OTAPI.Tile;
using System;
#endregion
namespace FakeProvider
{
    public interface INamedTileCollection : ITileCollection, IDisposable
    {
        IProviderTile this[int x, int y] { get; set; }

        string Name { get; }
        int X { get; set; }
        int Y { get; set; }
        int Layer { get; }
        bool Enabled { get; }

        (int X, int Y, int Width, int Height) XYWH();
        void SetXYWH(int X, int Y, int Width, int Height);
        void Move(int X, int Y);
        void Draw(bool section);
    }
}