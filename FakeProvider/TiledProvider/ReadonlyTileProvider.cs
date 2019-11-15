#region Using
using OTAPI.Tile;
using System;
using Terraria;
#endregion
namespace FakeProvider
{
    public sealed class ReadonlyTileProvider<T> : INamedTileCollection
    {
        #region Data

        private StructTile[,] Data;
        public string Name { get; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Layer { get; }
        public bool Enabled { get; private set; } = true;

        #endregion
        #region Constructor

        public ReadonlyTileProvider(string Name, int X, int Y, int Width, int Height, int Layer = 0)
        {
            this.Name = Name;
            this.Data = new StructTile[Width, Height];
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
            this.Layer = Layer;
        }

        #region ITileCollection

        public ReadonlyTileProvider(string Name, int X, int Y, int Width, int Height,
                ITileCollection CopyFrom, int Layer = 0)
            : this(Name, X, Y, Width, Height, Layer)
        {
            if (CopyFrom != null)
                for (int i = X; i < X + Width; i++)
                    for (int j = Y; j < Y + Height; j++)
                    {
                        ITile t = CopyFrom[i, j];
                        if (t != null)
                            new ReadonlyTileReference<T>(Data, (i - X - this.X), (j - Y - this.Y)).ForceCopyFrom(t);
                    }
        }

        #endregion
        #region ITile[,]

        public ReadonlyTileProvider(string Name, int X, int Y, int Width, int Height,
                ITile[,] CopyFrom, int Layer = 0)
            : this(Name, X, Y, Width, Height, Layer)
        {
            if (CopyFrom != null)
                for (int i = X; i < X + Width; i++)
                    for (int j = Y; j < Y + Height; j++)
                    {
                        ITile t = CopyFrom[i, j];
                        if (t != null)
                            new ReadonlyTileReference<T>(Data, (i - X - this.X), (j - Y - this.Y)).ForceCopyFrom(t);
                    }
        }

        #endregion

        #endregion

        #region operator[,]

        public ITile this[int X, int Y]
        {
            get => new ReadonlyTileReference<T>(Data, (X - this.X), (Y - this.Y));
            set { }
        }

        #endregion

        #region XYWH

        public (int X, int Y, int Width, int Height) XYWH() =>
            (X, Y, Width, Height);

        #endregion
        #region SetXYWH

        public void SetXYWH(int X, int Y, int Width, int Height)
        {
            this.X = X;
            this.Y = Y;
            if ((this.Width != Width) || (this.Height != Height))
            {
                StructTile[,] newData = new StructTile[Width, Height];
                for (int i = 0; i < Width; i++)
                    for (int j = 0; j < Height; j++)
                        if ((i < this.Width) && (j < this.Height))
                            newData[i, j] = Data[i, j];
                this.Data = newData;
                this.Width = Width;
                this.Height = Height;
            }
        }

        #endregion
        #region Enable

        public void Enable()
        {
            if (!Enabled)
            {
                Enabled = true;
                FakeProvider.Tile.UpdateProviderIndexes(this);
#warning NotImplemented
                new NotImplementedException("Draw on enable");
            }
        }

        #endregion
        #region Disable

        public void Disable()
        {
            if (Enabled)
            {
                Enabled = false;
                FakeProvider.Tile.UpdateProviderIndexes(X, Y, Width, Height);
#warning NotImplemented
                new NotImplementedException("Draw on disable");
            }
        }

        #endregion

        #region Draw

        public void Draw(bool section = false)
        {
            if (section)
            {
                NetMessage.SendData((int)PacketTypes.TileSendSection, -1, -1, null, X, Y, Width, Height);
                int sx1 = Netplay.GetSectionX(X), sy1 = Netplay.GetSectionY(Y);
                int sx2 = Netplay.GetSectionX(X + Width - 1), sy2 = Netplay.GetSectionY(Y + Height - 1);
                NetMessage.SendData((int)PacketTypes.TileFrameSection, -1, -1, null, sx1, sy1, sx2, sy2);
            }
            else
                NetMessage.SendData((int)PacketTypes.TileSendSquare, -1, -1, null, Math.Max(Width, Height), X, Y);
        }

        #endregion

        #region Intersect

        internal void Intersect(int X, int Y, int Width, int Height,
            out int RX, out int RY, out int RWidth, out int RHeight)
        {
            int ex1 = this.X + this.Width;
            int ex2 = X + Width;
            int ey1 = this.Y + this.Height;
            int ey2 = Y + Height;
            int maxSX = (this.X > X) ? this.X : X;
            int maxSY = (this.Y > Y) ? this.Y : Y;
            int minEX = (ex1 < ex2) ? ex1 : ex2;
            int minEY = (ey1 < ey2) ? ey1 : ey2;
            RX = maxSX;
            RY = maxSY;
            RWidth = minEX - maxSX;
            RHeight = minEY - maxSY;
        }

        #endregion
        #region IsIntersecting

        internal bool IsIntersecting(int X, int Y, int Width, int Height) =>
            ((X < (this.X + this.Width)) && (this.X < (X + Width))
            && (Y < (this.Y + this.Height)) && (this.Y < (Y + Height)));

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