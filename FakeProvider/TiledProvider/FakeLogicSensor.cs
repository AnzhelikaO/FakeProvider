#region Using
using Terraria.DataStructures;
using Terraria.GameContent.Tile_Entities;
#endregion
namespace FakeProvider
{
    public class FakeLogicSensor : TELogicSensor, IFake
    {
        public INamedTileCollection Provider { get; }
        public int Index
        {
            get => ID;
            set => ID = value;
        }
        public int X
        {
            get => Position.X;
            set => Position = new Point16((short)value, Position.Y);
        }
        public int Y
        {
            get => Position.Y;
            set => Position = new Point16(Position.X, (short)value);
        }
        public int RelativeX { get; set; }
        public int RelativeY { get; set; }

        public FakeLogicSensor(INamedTileCollection Provider, int Index, int X, int Y, LogicCheckType LogicCheckType)
        {
            this.Provider = Provider;
            this.ID = Index;
            this.RelativeX = X;
            this.RelativeY = Y;
            this.Position = new Point16(X, Y);
            this.logicCheck = LogicCheckType;
            this.type = 2;
        }
    }
}
