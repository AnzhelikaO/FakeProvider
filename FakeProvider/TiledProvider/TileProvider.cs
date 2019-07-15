#region Using
using OTAPI.Tile;
using System;
#endregion
namespace FakeManager
{
    public class TileProvider : ITileCollection2, IDisposable
    {
        #region Data

        private StructTile[,] Data;
        public string Name { get; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; }
        public int Height { get; }

        #endregion
        #region Constructor

        public TileProvider(string Name, int X, int Y, int Width, int Height)
        {
            this.Name = Name;
            this.Data = new StructTile[Width, Height];
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
        }
        
        #endregion

        #region operator[,]

        public ITile this[int X, int Y]
        {
            get => new TileReference(Data, (X - this.X), (Y - this.Y));
            set => new TileReference(Data, (X - this.X), (Y - this.Y)).CopyFrom(value);
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