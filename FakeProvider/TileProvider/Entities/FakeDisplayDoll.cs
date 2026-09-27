#region Using
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Tile_Entities;
using Terraria.ID;
#endregion
namespace FakeProvider
{
    public class FakeDisplayDoll : TEDisplayDoll, IFake
    {
        #region Data

        public TileProvider Provider { get; }
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
        internal static ushort[] _TileTypes = new ushort[]
        {
            TileID.DisplayDoll
        };
        public ushort[] TileTypes => _TileTypes;
        public int RelativeX { get; set; }
        public int RelativeY { get; set; }

        #endregion

        #region Constructor

        public FakeDisplayDoll(TileProvider Provider, int Index, int X, int Y, Item[] Items = null, Item[] Dyes = null,
            Item[] Misc = null, byte Pose = 0)
        {
            this.Provider = Provider;
            this.ID = Index;
            this.RelativeX = X;
            this.RelativeY = Y;
            this.Position = new Point16(X, Y);
            this.type = EntityTypeID;
            CopyItems(Items, _equip);
            CopyItems(Dyes, _dyes);
            CopyItems(Misc, _misc);
            this._pose = Pose;
        }

        private static void CopyItems(Item[] source, Item[] destination)
        {
            if (source == null)
                return;
            for (int i = 0; i < destination.Length && i < source.Length; i++)
                destination[i] = source[i] ?? new Item();
        }

        #endregion
    }
}
