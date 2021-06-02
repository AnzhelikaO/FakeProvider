using OTAPI.Tile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeProvider
{
    public class TileCollection : ITileCollection
    {
		protected ITile[,] Tiles;
		public int Width => Tiles.GetLength(0);
		public int Height => Tiles.GetLength(1);

		public TileCollection(ITile[,] collection)
		{
			Tiles = collection;
		}

		public virtual ITile this[int x, int y]
		{
			get
			{
				return Tiles[x, y];
			}
			set
			{
				Tiles[x, y] = value;
			}
		}
	}
}
