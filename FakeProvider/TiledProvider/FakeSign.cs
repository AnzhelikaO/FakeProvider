using Terraria;
namespace FakeProvider
{
    public class FakeSign : Sign, IFake
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

        public FakeSign(INamedTileCollection Provider, int Index, int X, int Y, string Text = "")
        {
            this.Provider = Provider;
            this.Index = Index;
            this.RelativeX = X;
            this.RelativeY = Y;
            this.x = Provider.ProviderCollection.OffsetX + Provider.X + X;
            this.y = Provider.ProviderCollection.OffsetY + Provider.Y + Y;
            this.text = Text;
        }
    }
}
