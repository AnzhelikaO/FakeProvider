using Terraria;
namespace FakeProvider
{
    public class FakeChest : Chest, IFake
    {
        public INamedTileCollection Provider { get; }
        public int Index { get; set; }
        public int X
        {
            get => x;
            set => x = value;
        }
        public int Y
        {
            get => y;
            set => y = value;
        }
        public int RelativeX { get; set; }
        public int RelativeY { get; set; }

        public FakeChest(INamedTileCollection Provider, int Index, int X, int Y, Item[] Items = null)
        {
            this.Provider = Provider;
            this.Index = Index;
            this.RelativeX = X;
            this.RelativeY = Y;
            this.x = Provider.ProviderCollection.OffsetX + Provider.X + X;
            this.y = Provider.ProviderCollection.OffsetY + Provider.Y + Y;
            this.item = Items ?? new Item[40];
            for (int i = 0; i < 40; i++)
                this.item[i] = this.item[i] ?? new Item();
        }
    }
}
