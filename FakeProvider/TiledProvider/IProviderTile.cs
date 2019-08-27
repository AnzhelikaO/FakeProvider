using OTAPI.Tile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeProvider
{
    public interface IProviderTile : ITile
    {
        INamedTileCollection Provider { get; }
        int Layer { get; }
    }
}
