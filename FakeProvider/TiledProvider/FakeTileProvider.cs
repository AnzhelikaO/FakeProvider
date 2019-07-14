#region Using
using OTAPI.Tile;
using System;
#endregion
namespace FakeManager
{
    public class FakeTileProvider : ITileCollection, IDisposable
    {
        #region Data

        private StructTile[,] Data { get; set; }
        public int Width { get; }
        public int Height { get; }

        #endregion
        #region Constructor

        public FakeTileProvider(int Width, int Height)
        {
            this.Data = new StructTile[Width, Height];
            this.Width = Width;
            this.Height = Height;
        }
        
        #endregion

        #region operator[,]

        public ITile this[int X, int Y]
        {
            get => new TileReference(Data, X, Y);
            set => new TileReference(Data, X, Y).CopyFrom(value);
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            if (Data == null)
                return;
            int w = Data.GetLength(0), h = Data.GetLength(1);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    Data[x, y].bTileHeader = 0;
                    Data[x, y].bTileHeader2 = 0;
                    Data[x, y].bTileHeader3 = 0;
                    Data[x, y].frameX = 0;
                    Data[x, y].frameY = 0;
                    Data[x, y].liquid = 0;
                    Data[x, y].type = 0;
                    Data[x, y].wall = 0;
                }
            Data = null;
        }

        #endregion
    }
}