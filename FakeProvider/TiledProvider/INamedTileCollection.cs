#region Using
using OTAPI.Tile;
using System;
using Terraria;
using Terraria.GameContent.Tile_Entities;
#endregion
namespace FakeProvider
{
    public interface INamedTileCollection : ITileCollection, IDisposable
    {
        IProviderTile this[int x, int y] { get; set; }
        IProviderTile GetIncapsulatedTile(int X, int Y);
        void SetIncapsulatedTile(int X, int Y, ITile Tile);

        TileProviderCollection ProviderCollection { get; }
        string Name { get; }
        int X { get; }
        int Y { get; }
        int Layer { get; }
        bool Enabled { get; }

        void Initialize(TileProviderCollection ProviderCollection, string Name, int X, int Y,
            int Width, int Height, int Layer = 0);
        void Initialize(TileProviderCollection ProviderCollection, string Name, int X, int Y,
            int Width, int Height, ITileCollection CopyFrom, int Layer = 0);
        void Initialize(TileProviderCollection ProviderCollection, string Name, int X, int Y,
            int Width, int Height, ITile[,] CopyFrom, int Layer = 0);

        (int X, int Y, int Width, int Height) XYWH(int DeltaX = 0, int DeltaY = 0);
        (int X, int Y, int Width, int Height) ClampXYWH();
        void SetXYWH(int X, int Y, int Width, int Height, bool Draw = true);
        void Move(int X, int Y, bool Draw = true);
        void Resize(int Width, int Height, bool Draw = true);
        void Draw(bool Section = true);
        void Enable(bool Draw = true);
        void Disable(bool Draw = true);
        void SetTop(bool Draw = true);

        FakeSign AddSign(int X, int Y, string Text);
        FakeChest AddChest(int X, int Y, Item[] Items = null);
        FakeTrainingDummy AddTrainingDummy(int X, int Y);
        FakeItemFrame AddItemFrame(int X, int Y, Item Item = null);
        FakeLogicSensor AddLogicSensor(int X, int Y, TELogicSensor.LogicCheckType LogicCheckType);
        void RemoveEntity(IFake Entity);
        void UpdateEntities();
        void HideEntities();
        void ScanEntities();
    }
}
