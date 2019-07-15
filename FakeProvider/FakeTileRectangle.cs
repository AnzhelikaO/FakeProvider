#region Using
using System;
using Terraria;
using OTAPI.Tile;
using System.Collections.Generic;
#endregion
namespace FakeProvider
{
    public class FakeTileRectangle : ICloneable
    {
        #region Data

        public object Key { get; }
        public TileProvider Tile { get; protected set; }
        public int X { get; protected set; }
        public int Y { get; protected set; }
        public int Width { get; protected set; }
        public int Height { get; protected set; }
        public FakeCollection Collection { get; }

        //public bool IsPersonal => Collection.IsPersonal;

        #endregion
        #region Constructor

        public FakeTileRectangle(FakeCollection Collection,
            object Key, int X, int Y, int Width, int Height)
        {
            this.Key = Key;
            this.Collection = Collection;
            this.Tile = new TileProvider(Width, Height);
            for (int i = 0; i < Width; i++)
                for (int j = 0; j < Height; j++)
                    if (this.Tile[i, j] == null)
                        this.Tile[i, j] = new Tile();
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
        }

        #region ITileCollection

        public FakeTileRectangle(FakeCollection Collection, object Key,
                int X, int Y, int Width, int Height, ITileCollection CopyFrom)
            : this(Collection, Key, X, Y, Width, Height)
        {
            if (CopyFrom != null)
                for (int i = X; i < X + Width; i++)
                    for (int j = Y; j < Y + Height; j++)
                    {
                        ITile t = CopyFrom[i, j];
                        if (t != null)
                            this.Tile[i - X, j - Y].CopyFrom(t);
                    }
        }

        #endregion
        #region ITile[,]

        public FakeTileRectangle(FakeCollection Collection, object Key,
            int X, int Y, int Width, int Height, ITile[,] Tile)
            : this(Collection, Key, X, Y, Width, Height)
        {
            if (Tile != null)
                for (int i = X; i < X + Width; i++)
                    for (int j = Y; j < Y + Height; j++)
                    {
                        ITile t = Tile[i, j];
                        if (t != null)
                            this.Tile[i - X, j - Y].CopyFrom(t);
                    }
        }

        #endregion

        #endregion

        #region operator[,]

        public ITile this[int X, int Y]
        {
            get => Tile[X, Y];
            set => Tile[X, Y] = value;
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
                TileProvider newTile = new TileProvider(Width, Height);
                for (int i = 0; i < Width; i++)
                    for (int j = 0; j < Height; j++)
                        newTile[i, j] = ((i < this.Width) && (j < this.Height))
                                            ? Tile[i, j]
                                            : new Tile();
                Tile = newTile;
                this.Width = Width;
                this.Height = Height;
            }
        }

        #endregion
        #region Update

        // Do not remove.
        public void Update() { }

        #endregion

        #region Intersect

        protected internal void Intersect(int X, int Y, int Width, int Height,
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

        protected internal bool IsIntersecting(int X, int Y, int Width, int Height) =>
            ((X < (this.X + this.Width)) && (this.X < (X + Width))
            && (Y < (this.Y + this.Height)) && (this.Y < (Y + Height)));

        #endregion

        #region ApplyTiles

        protected internal void ApplyTiles(ITile[,] Tiles, int AbsoluteX, int AbsoluteY)
        {
            Intersect(AbsoluteX, AbsoluteY, Tiles.GetLength(0), Tiles.GetLength(1),
                out int x1, out int y1, out int w, out int h);
            int x2 = (x1 + w), y2 = (y1 + h);

            for (int i = x1; i < x2; i++)
                for (int j = y1; j < y2; j++)
                {
                    ITile tile = Tile[i - X, j - Y];
                    if (tile != null)
                        Tiles[(i - AbsoluteX), (j - AbsoluteY)] = tile;
                }
        }

        #endregion
        #region ApplySigns

        protected internal void ApplySigns(Dictionary<int, Sign> Signs,
            int AbsoluteX, int AbsoluteY, int Width, int Height,
            bool ClearIntersectingSigns = false)
        {
            Intersect(AbsoluteX, AbsoluteY, Width, Height,
                out int x1, out int y1, out int w, out int h);
            int x2 = (x1 + w), y2 = (y1 + h);

            if (ClearIntersectingSigns)
                foreach (int key in Signs.Keys)
                {
                    int x = Signs[key].x, y = Signs[key].y;
                    if ((x >= x1) && (x < x2) && (y >= y1) && (y < y2))
                        Signs.Remove(key);
                }

            lock (FakeSigns)
                foreach (KeyValuePair<int, Sign> pair in FakeSigns)
                {
                    int x = pair.Value.x, y = pair.Value.y;
                    if ((x >= x1) && (x < x2) && (y >= y1) && (y < y2))
                        Signs.Add(pair.Key, pair.Value);
                }
        }

        #endregion
        #region ApplyChest

        protected internal void ApplyChest(ref Chest Chest, int AbsoluteX, int AbsoluteY)
        {
            lock (FakeChests)
                foreach (Chest chest in FakeChests)
                    if ((chest.x == AbsoluteX) && (chest.y == AbsoluteY))
                    {
                        Chest = chest;
                        return;
                    }
        }

        #endregion

        public object Clone() =>
            new FakeTileRectangle(Collection, Key, X, Y, Width, Height, Tile);
    }
}