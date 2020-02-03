#region Using
using OTAPI.Tile;
using System;
using System.Collections.ObjectModel;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Tile_Entities;
#endregion
namespace FakeProvider
{
    public interface INamedTileCollection : ITileCollection, IDisposable
    {
        IProviderTile this[int x, int y] { get; set; }
        IProviderTile GetIncapsulatedTile(int X, int Y);
        void SetIncapsulatedTile(int X, int Y, ITile Tile);
        INamedTileCollection Tile { get; }

        TileProviderCollection ProviderCollection { get; }
        string Name { get; }
        int X { get; }
        int Y { get; }
        int Index { get; }
        int Order { get; }
        int Layer { get; }
        bool Enabled { get; }
        ReadOnlyCollection<IFake> Entities { get; }

        (int X, int Y, int Width, int Height) XYWH(int DeltaX = 0, int DeltaY = 0);
        (int X, int Y, int Width, int Height) ClampXYWH();
        void SetXYWH(int X, int Y, int Width, int Height, bool Draw = true);
        void Move(int X, int Y, bool Draw = true);
        void Resize(int Width, int Height, bool Draw = true);
        void Draw(bool Section = true);
        void Enable(bool Draw = true);
        void Disable(bool Draw = true);
        void SetTop(bool Draw = true);
        void Update();
        void Clear();
        void CopyFrom(INamedTileCollection provider);


        FakeSign AddSign(int X, int Y, string Text);
        FakeChest AddChest(int X, int Y, Item[] Items = null);
        FakeTrainingDummy AddTrainingDummy(int X, int Y);
        FakeItemFrame AddItemFrame(int X, int Y, Item Item = null);
        FakeLogicSensor AddLogicSensor(int X, int Y, TELogicSensor.LogicCheckType LogicCheckType);
        IFake AddEntity(IFake Entity);
        FakeSign AddEntity(Sign Entity, bool replace = false);
        FakeChest AddEntity(Chest Entity, bool replace = false);
        IFake AddEntity(TileEntity Entity, bool replace = false);
        FakeTrainingDummy AddEntity(TETrainingDummy Entity, bool replace = false);
        FakeItemFrame AddEntity(TEItemFrame Entity, bool replace = false);
        FakeLogicSensor AddEntity(TELogicSensor Entity, bool replace = false);

        void RemoveEntity(IFake Entity);
        void UpdateEntities();
        void HideEntities();
        void ScanEntities();
    }
}
