using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace FakeProvider
{
    public class ObserversEqualityComparer : IEqualityComparer<IEnumerable<INamedTileCollection>>
    {
        public bool Equals(IEnumerable<INamedTileCollection> b1, IEnumerable<INamedTileCollection> b2) =>
            b1 == b2 || Enumerable.SequenceEqual(b1, b2);
        public int GetHashCode(IEnumerable<INamedTileCollection> bx) => 0;
    }
}
