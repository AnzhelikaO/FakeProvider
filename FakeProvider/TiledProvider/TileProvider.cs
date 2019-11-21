#region Using
using OTAPI.Tile;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Terraria;
#endregion
namespace FakeProvider
{
    public sealed class TileProvider<T> : INamedTileCollection
    {
        #region Data

        private unsafe TileStruct* Tiles;
        public string Name { get; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Layer { get; }
        public bool Enabled { get; private set; } = true;

        #endregion
        #region Constructor

        public TileProvider(string Name, int X, int Y, int Width, int Height, int Layer = 0)
        {
            this.Name = Name;
            unsafe
            {
                int size = Width * Height * sizeof(TileStruct);
                Tiles = (TileStruct*)Marshal.AllocHGlobal(size);
                Marshal.Copy(new byte[size], 0, (IntPtr)Tiles, size);
            }
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
            this.Layer = Layer;
        }

        #region ITileCollection

        public TileProvider(string Name, int X, int Y, int Width, int Height,
                ITileCollection CopyFrom, int Layer = 0)
        {
            this.Name = Name;
            this.Tiles = new Tile<T>[Width, Height];
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
            this.Layer = Layer;

            for (int i = X; i < X + Width; i++)
                for (int j = Y; j < Y + Height; j++)
                {
                    ITile t = CopyFrom[i, j];
                    if (t != null)
                        Tiles[i - X, j - Y] = new Tile<T>(t);
                }
        }

        #endregion
        #region ITile[,]

        public TileProvider(string Name, int X, int Y, int Width, int Height,
                ITile[,] CopyFrom, int Layer = 0)
        {
            this.Name = Name;
            this.Tiles = new Tile<T>[Width, Height];
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
            this.Layer = Layer;

            for (int i = X; i < X + Width; i++)
                for (int j = Y; j < Y + Height; j++)
                {
                    ITile t = CopyFrom[i, j];
                    if (t != null)
                        Tiles[i - X, j - Y] = new Tile<T>(t);
                }
        }

        #endregion

        #endregion

        #region operator[,]

        ITile ITileCollection.this[int X, int Y]
        {
            get
            {
                unsafe
                {
                    int x = X - this.X, y = Y - this.Y;
                    if (x >= 0 && y >= 0 && x < Width && y < Height)
                        return new Tile<T>(Tiles + x * Height + y);
                    else
                        return null;
                }
            }
            set
            {
                unsafe
                {
                    int x = X - this.X, y = Y - this.Y;
                    if (x >= 0 && y >= 0 && x < Width && y < Height)
                        new Tile<T>(Tiles + x * Height + y).CopyFrom(value);
                }
            }
        }

        public IProviderTile this[int X, int Y]
        {
            get => Tiles[X - this.X, Y - this.Y];
            set => Tiles[X - this.X, Y - this.Y].CopyFrom(value);
        }

        #endregion

        #region XYWH

        public (int X, int Y, int Width, int Height) XYWH() => (X, Y, Width, Height);

        #endregion
        #region SetXYWH

        public void SetXYWH(int X, int Y, int Width, int Height)
        {
            this.X = X;
            this.Y = Y;
            if ((this.Width != Width) || (this.Height != Height))
            {
                Tile<T>[,] newData = new Tile<T>[Width, Height];
                for (int i = 0; i < Width; i++)
                    for (int j = 0; j < Height; j++)
                        if ((i < this.Width) && (j < this.Height))
                            newData[i, j] = Tiles[i, j];
                        else
                            newData[i, j] = new Tile<T>();
                this.Tiles = newData;
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
                FakeProvider.Tile.UpdateProviderReferences(this);
                Draw(true);
            }
        }

        #endregion
        #region Disable

        public void Disable()
        {
            if (Enabled)
            {
                Enabled = false;
                FakeProvider.Tile.UpdateRectangleReferences(X, Y, Width, Height);
                Draw(true);
            }
        }

        #endregion

        #region Draw

        public void Draw(bool section=false)
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

        #region Dispose

        public void Dispose()
        {
            if (Tiles == null)
                return;
            Tiles = null;
        }

        #endregion
    }
}