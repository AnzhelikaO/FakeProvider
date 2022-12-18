using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;

namespace FakeProvider
{
	[StructLayout(LayoutKind.Sequential, Size = 14, Pack = 1)]
	internal struct StructTile
	{
		public byte bTileHeader;
		public byte bTileHeader2;
		public byte bTileHeader3;
		// collisionType is not a field
		public short frameX;
		public short frameY;
		public byte liquid;
		public ushort sTileHeader;
		public ushort type;
		public ushort wall;
	}
}
