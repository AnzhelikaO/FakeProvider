using System.Runtime.InteropServices;
namespace FakeManager
{
	[StructLayout(LayoutKind.Sequential, Size = 13, Pack = 1)]
    public struct StructTile
    {
        public byte wall;
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