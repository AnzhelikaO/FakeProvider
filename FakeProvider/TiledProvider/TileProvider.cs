#region Using
using OTAPI.Tile;
using System;
using System.Reflection;
using Terraria;
#endregion
namespace FakeProvider
{
    public sealed class TileProvider<T> : INamedTileCollection
    {
        #region Data

        private Tile<T>[,] Data;
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
            this.Data = new Tile<T>[Width, Height];
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
            this.Layer = Layer;

            for (int x = 0; x < this.Width; x++)
                for (int y = 0; y < this.Height; y++)
                    Data[x, y] = new Tile<T>();
        }

        #region ITileCollection

        public TileProvider(string Name, int X, int Y, int Width, int Height,
                ITileCollection CopyFrom, int Layer = 0)
        {
            this.Name = Name;
            this.Data = new Tile<T>[Width, Height];
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
                        Data[i - X, j - Y] = new Tile<T>(t);
                }
        }

        #endregion
        #region ITile[,]

        public TileProvider(string Name, int X, int Y, int Width, int Height,
                ITile[,] CopyFrom, int Layer = 0)
        {
            this.Name = Name;
            this.Data = new Tile<T>[Width, Height];
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
            this.Layer = Layer;

            for (int i = 0; i < Width; i++)
                for (int j = 0; j < Height; j++)
                {
                    ITile t = CopyFrom[i, j];
                    if (t != null)
                        Data[i, j] = new Tile<T>(t);
                }
        }

        #endregion

        #endregion

        #region operator[,]

        ITile ITileCollection.this[int X, int Y]
        {
            get => Data[X, Y];
            set => Data[X, Y].CopyFrom(value);
        }

        public IProviderTile this[int X, int Y]
        {
            get => Data[X, Y];
            set => Data[X, Y].CopyFrom(value);
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
                            newData[i, j] = Data[i, j];
                        else
                            newData[i, j] = new Tile<T>();
                this.Data = newData;
                this.Width = Width;
                this.Height = Height;
            }
        }

        #endregion
        #region Move

        public void Move(int X, int Y)
        {
            FakeProvider.Tile.Remove(Name);
            SetXYWH(X, Y, this.Width, this.Height);
            FakeProvider.Tile.Add(this);
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
            if (Data == null)
                return;
            Data = null;
        }

        #endregion
    }
}