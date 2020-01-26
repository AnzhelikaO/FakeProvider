using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace FakeProvider
{
    public class FakeSign : Sign
    {
        public INamedTileCollection Provider { get; }
        public int Index { get; internal set; }
        public int RelativeX { get; set; }
        public int RelativeY { get; set; }

        public FakeSign(INamedTileCollection Provider, int Index, int X, int Y, string Text = "")
        {
            this.Provider = Provider;
            this.Index = Index;
            this.RelativeX = X;
            this.RelativeY = Y;
            x = Provider.ProviderCollection.OffsetX + Provider.X + X;
            y = Provider.ProviderCollection.OffsetY + Provider.Y + Y;
            text = Text;
        }
    }
}
