#region Using
using OTAPI.Tile;
using System;
using System.Collections.Generic;
using Terraria;
#endregion
namespace FakeProvider
{
    public interface INamedTileCollection : ITileCollection, IDisposable
    {
        IProviderTile this[int x, int y] { get; set; }

        TileProviderCollection ProviderCollection { get; }
        string Name { get; }
        int X { get; set; }
        int Y { get; set; }
        int Layer { get; }
        bool Enabled { get; }

        (int X, int Y, int Width, int Height) XYWH(int DeltaX = 0, int DeltaY = 0);
        (int X, int Y, int Width, int Height) ClampXYWH();
        void SetXYWH(int X, int Y, int Width, int Height);
        void Move(int X, int Y, bool Draw = true);
        void Draw(bool Section);
        void Enable(bool Draw = true);
        void Disable(bool Draw = true);
        void HideSignsChestsEntities();
        void UpdateSignsChestsEntities();
        void Scan();

        FakeSign AddSign(int X, int Y, string Text);
        void RemoveSign(FakeSign Sign);
        void UpdateSigns();

        FakeChest AddChest(int X, int Y, Item[] Items = null);
        void RemoveChest(FakeChest Sign);
        void UpdateChests();
    }
}