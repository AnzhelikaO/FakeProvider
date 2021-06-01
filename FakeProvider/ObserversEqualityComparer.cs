using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace FakeProvider
{
    public class ObserversEqualityComparer : IEqualityComparer<IEnumerable<RemoteClient>>
    {
        public bool Equals(IEnumerable<RemoteClient> b1, IEnumerable<RemoteClient> b2) =>
            Enumerable.SequenceEqual(b1, b2);
        public int GetHashCode(IEnumerable<RemoteClient> bx) => 0;
    }
}
