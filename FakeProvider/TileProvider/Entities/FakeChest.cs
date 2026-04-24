#region Using
using System.Reflection;
using Terraria;
using Terraria.ID;
#endregion
namespace FakeProvider
{
    public class FakeChest : Chest, IFake
    {
        #region Data

        // x, y and index fields are readonly in Terraria, so use reflection to update them.
        private static readonly FieldInfo ChestIndexField = typeof(Chest)
            .GetField("index", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo XField = typeof(Chest)
            .GetField("x", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo YField = typeof(Chest)
            .GetField("y", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public TileProvider Provider { get; }
        public int Index
        {
            get => index;
            set => ChestIndexField.SetValue(this, value);
        }
        public int X
        {
            get => x;
            set => XField.SetValue(this, value);
        }
        public int Y
        {
            get => y;
            set => YField.SetValue(this, value);
        }
        internal static ushort[] _TileTypes = new ushort[]
        {
            TileID.Containers,
            TileID.Containers2,
            TileID.Dressers
        };
        public ushort[] TileTypes => _TileTypes;
        public int RelativeX { get; set; }
        public int RelativeY { get; set; }

        #endregion

        #region Constructor

        public FakeChest(TileProvider Provider, int Index, int X, int Y, Item[] Items = null) : base(Provider.X + X, Provider.Y + Y)
        {
            this.Provider = Provider;
            this.Index = Index;
            this.RelativeX = X;
            this.RelativeY = Y;
            this.item = Items ?? new Item[40];
            for (int i = 0; i < 40; i++)
                this.item[i] = this.item[i] ?? new Item();
        }

        #endregion
    }
}
