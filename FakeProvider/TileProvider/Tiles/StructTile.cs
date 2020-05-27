using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FakeProvider
{
    [StructLayout(LayoutKind.Sequential, Size = 14, Pack = 1)]
    public struct StructTile
    {
        public ushort wall;
        public byte liquid;
        public byte bTileHeader;
        public byte bTileHeader2;
        public byte bTileHeader3;
        public ushort type;
        public short sTileHeader;
        public short frameX;
        public short frameY;
    }
}
