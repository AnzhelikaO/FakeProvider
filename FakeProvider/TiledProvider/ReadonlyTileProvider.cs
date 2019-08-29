#region Using
using OTAPI.Tile;
using System;
using System.Runtime.InteropServices;
using Terraria;
#endregion
namespace FakeProvider
{
    [StructLayout(LayoutKind.Sequential, Size = 15, Pack = 1)]
    public sealed class ReadonlyTileProvider : INamedTileCollection
    {
        #region Data

        public TileProviderCollection Parent { get; private set; }
        private IProviderTile[,] Tiles;
        public short Index { get; internal set; }
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
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
            this.Layer = Layer;
            this.Tiles = new IProviderTile[Width, Height];
        }

        #region ITileCollection

        public ReadonlyTileProvider(string Name, int X, int Y, int Width, int Height,
                ITileCollection CopyFrom, int Layer = 0)
            : this(Name, X, Y, Width, Height, Layer)
        {
            int copyWidth = CopyFrom.Width;
            int copyHeight = CopyFrom.Height;
            if (CopyFrom != null)
                for (int i = 0; i < Width; i++)
                    for (int j = 0; j < Height; j++)
                        if (i < copyWidth && j < copyHeight)
                        {
                            ITile t = CopyFrom[i, j];
                            if (t != null)
                                Tiles[i, j] = new ReadonlyFakeTile(Index, t);
                        }
        }

        #endregion
        #region ITile[,]

        public ReadonlyTileProvider(string Name, int X, int Y, int Width, int Height,
                ITile[,] CopyFrom, int Layer = 0)
            : this(Name, X, Y, Width, Height, Layer)
        {
            int copyWidth = CopyFrom.GetLength(0);
            int copyHeight = CopyFrom.GetLength(1);
            if (CopyFrom != null)
                for (int i = 0; i < Width; i++)
                    for (int j = 0; j < Height; j++)
                        if (i < copyWidth && j < copyHeight)
                        {
                            ITile t = CopyFrom[i, j];
                            if (t != null)
                                Tiles[i, j] = new ReadonlyFakeTile(Index, t);
                        }
        }

        #endregion

        #endregion

        #region operator[,]

        /// <summary>
        /// Get/set tile relative to provider position
        /// </summary>
        public ITile this[int X, int Y]
        {
            get => Tiles[X, Y];
            set
            {
                if (Tiles[X, Y] == null)
                    Tiles[X, Y] = new FakeTile(Index);
                Tiles[X, Y]?.CopyFrom(value);
            }
        }

        #endregion

        #region SetupParent

        public void SetupParent(TileProviderCollection Parent, short Index)
        {
            Console.WriteLine("SetupParent for: " + Name);
            Console.WriteLine("Parent value: " + Parent);
            if (this.Parent != null)
                throw new InvalidOperationException();
            this.Parent = Parent;
            this.Index = Index;
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
                int oldWidth = this.Width;
                int oldHeight = this.Height;
                IProviderTile[,] Tiles = new IProviderTile[Width, Height];
                for (int i = 0; i < Width; i++)
                    for (int j = 0; j < Height; j++)
                        if ((i < oldWidth) && (j < oldHeight))
                            Tiles[i, j] = this.Tiles[i, j];
                this.Tiles = Tiles;
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
                Apply();
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
                FakeProvider.Tile.UpdateTiles(X, Y, Width, Height);
#warning NotImplemented
                new NotImplementedException("Draw on disable");
            }
        }

        #endregion

        #region Apply

        public void Apply()
        {
            if (!Enabled)
                return;
            int offsetX = Parent.OffsetX;
            int offsetY = Parent.OffsetY;
            for (int i = 0; i < Width; i++)
                for (int j = 0; j < Height; j++)
                {
                    IProviderTile tile = Parent.Tiles[X + i + offsetX, Y + j + offsetY];
                    IProviderTile providerTile = Tiles[i, j];
                    if ((tile == null || tile.Layer <= Layer)
                            && providerTile != null)
                        Parent.Tiles[i + offsetX, j + offsetY] = providerTile;
                }
        }

        #endregion
        #region Draw

        public void Draw(bool section = false)
        {
            int x = Parent.OffsetX + X;
            int y = Parent.OffsetY + Y;
            if (section)
            {
                NetMessage.SendData((int)PacketTypes.TileSendSection, -1, -1, null, x, y, Width, Height);
                int sx1 = Netplay.GetSectionX(x), sy1 = Netplay.GetSectionY(y);
                int sx2 = Netplay.GetSectionX(x + Width - 1), sy2 = Netplay.GetSectionY(y + Height - 1);
                NetMessage.SendData((int)PacketTypes.TileFrameSection, -1, -1, null, sx1, sy1, sx2, sy2);
            }
            else
                NetMessage.SendData((int)PacketTypes.TileSendSquare, -1, -1, null, Math.Max(Width, Height), x, y);
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
            Tiles = null;
        }

        #endregion
    }
}