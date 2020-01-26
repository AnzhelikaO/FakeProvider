using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace FakeProvider
{
    public class FakeChest : Chest
    {
        public INamedTileCollection Provider { get; }
        public int Index { get; internal set; }
        public int RelativeX { get; set; }
        public int RelativeY { get; set; }

        public FakeChest(INamedTileCollection Provider, int Index, int X, int Y, Item[] Items = null)
        {
            this.Provider = Provider;
            this.Index = Index;
            this.RelativeX = X;
            this.RelativeY = Y;
            x = Provider.ProviderCollection.OffsetX + Provider.X + X;
            y = Provider.ProviderCollection.OffsetY + Provider.Y + Y;
            item = Items ?? new Item[40];
            for (int i = 0; i < 40; i++)
                item[i] = item[i] ?? new Item();
        }
    }
}
