#region Using
using Microsoft.Xna.Framework;
using OTAPI.Tile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Terraria;
#endregion
namespace FakeProvider
{
    public class TileProviderCollection : ITileCollection, IDisposable, IEnumerable<INamedTileCollection>
    {
        #region Data

        // ImmutableList?????????????????????????????????????????????????????????????????????????????????????
        // ??????????????????????????????????????????????????????????????????????????????????????????????????
        // ????????????????????????????????????????????????
        private static List<INamedTileCollection> Providers = new List<INamedTileCollection>();
        private static ITile[,] Tiles;

        /// <summary> World width visible by client. </summary>
        public int Width { get; }
        /// <summary> World height visible by client. </summary>
        public int Height { get; }
        /// <summary> Horizontal offset of the loaded world. </summary>
        
        // TODO: I completely messed up offset.
        public int OffsetX { get; }
        /// <summary> Vertical offset of the loaded world. </summary>
        public int OffsetY { get; }
        /// <summary> Tile to be visible outside of all providers. </summary>
        private object Locker { get; } = new object();

        #endregion
        #region Constructor

        public TileProviderCollection(int Width, int Height,
            int OffsetX, int OffsetY)
        {
            this.Width = Width;
            this.Height = Height;
            this.OffsetX = OffsetX;
            this.OffsetY = OffsetY;

            Tiles = new IProviderTile[this.Width, this.Height];
            for (int x = 0; x < this.Width; x++)
                for (int y = 0; y < this.Height; y++)
                    Tiles[x, y] = FakeProvider.VoidTile;
        }

        #endregion

        #region operator[,]

        public ITile this[int X, int Y]
        {
            get => Tiles[X - OffsetX, Y - OffsetY];
            set => Tiles[X - OffsetX, Y - OffsetY].CopyFrom(value);
        }

        #endregion
        #region GetTileSafe

        // Offset????
        public IProviderTile GetTileSafe(int X, int Y) => X >= 0 && Y >= 0 && X < Width && Y < Height
            ? (IProviderTile)this[X, Y]
            : FakeProvider.VoidTile;

        #endregion

        #region Dispose

        public void Dispose()
        {
            lock (Locker)
            {
                foreach (INamedTileCollection provider in Providers)
                    provider.Dispose();
            }
        }

        #endregion

        #region operator[]

        public INamedTileCollection this[string Name] =>
            Providers.FirstOrDefault(p => (p.Name == Name));

        #endregion

        #region Add

        internal void Add(INamedTileCollection Provider)
        {
            lock (Locker)
            {
                if (Providers.Any(p => (p.Name == Provider.Name)))
                    throw new ArgumentException($"Tile collection '{Provider.Name}' " +
                        "is already in use. Name must be unique.");
                SetTop(Provider);
                Provider.Enable(false);
            }
        }

        #endregion
        #region Remove

        public bool Remove(string Name, bool Draw = true, bool Cleanup = true)
        {
            lock (Locker)
                using (INamedTileCollection provider = Providers.FirstOrDefault(p => (p.Name == Name)))
                {
                    if (provider == null)
                        return false;
                    provider.Disable(Draw);
                    Providers.Remove(provider);
                }
            if (Cleanup)
                GC.Collect();
            return true;
        }

        #endregion
        #region Clear

        public void Clear(INamedTileCollection except = null)
        {
            lock (Locker)
                foreach (INamedTileCollection provider in Providers.ToArray())
                    if (provider != except)
                        Remove(provider.Name, true, false);
            GC.Collect();
        }

        #endregion
        #region SetTop

        internal void SetTop(INamedTileCollection Provider)
        {
            lock (Locker)
            {
                Providers.Remove(Provider);
                int index = Providers.FindIndex(p => (p.Layer > Provider.Layer));
                if (index == -1)
                    index = Providers.Count;
                Providers.Insert(index, Provider);
            }
        }

        #endregion

        #region Intersect

        internal static void Intersect(INamedTileCollection Provider, int X, int Y, int Width, int Height,
            out int RX, out int RY, out int RWidth, out int RHeight)
        {
            int ex1 = Provider.X + Provider.Width;
            int ex2 = X + Width;
            int ey1 = Provider.Y + Provider.Height;
            int ey2 = Y + Height;
            int maxSX = (Provider.X > X) ? Provider.X : X;
            int maxSY = (Provider.Y > Y) ? Provider.Y : Y;
            int minEX = (ex1 < ex2) ? ex1 : ex2;
            int minEY = (ey1 < ey2) ? ey1 : ey2;
            RX = maxSX;
            RY = maxSY;
            RWidth = minEX - maxSX;
            RHeight = minEY - maxSY;
        }

        #endregion
        #region UpdateRectangleReferences

        public void UpdateRectangleReferences(int X, int Y, int Width, int Height)
        {
            lock (Locker)
            {
                (X, Y, Width, Height) = Clamp(X, Y, Width, Height);
                for (int i = X; i < X + Width; i++)
                    for (int j = Y; j < Y + Height; j++)
                        Tiles[i, j] = FakeProvider.VoidTile;

                Queue<INamedTileCollection> providers = new Queue<INamedTileCollection>();
                foreach (INamedTileCollection provider in Providers)
                {
                    if (provider.Enabled)
                    {
                        // Update tiles
                        Intersect(provider, X, Y, Width, Height, out int x, out int y,
                            out int width, out int height);
                        int dx = x - provider.X;
                        int dy = y - provider.Y;
                        for (int i = 0; i < width; i++)
                            for (int j = 0; j < height; j++)
                                Tiles[x + i, y + j] = provider[dx + i, dy + j];

                        if (width > 0 && height > 0)
                            providers.Enqueue(provider);
                    }
                }

                // We are updating all the stuff only after tiles update since signs,
                // chests and entities apply only in case the tile on top is from this provider.
                while (providers.Count > 0)
                {
                    INamedTileCollection provider = providers.Dequeue();
                    provider.UpdateSignsChestsEntities();
                }
            }
        }

        #endregion
        #region UpdateProviderReferences

        public void UpdateProviderReferences(INamedTileCollection Provider)
        {
            if (!Provider.Enabled)
                return;
            lock (Locker)
            {
                // Scanning rectangle where this provider is/will appear.
                ScanRectangle(Provider.X, Provider.Y, Provider.Width, Provider.Height, Provider);

                // Update tiles
                int layer = Provider.Layer;
                (int x, int y, int width, int height) = Provider.ClampXYWH();

                for (int i = 0; i < width; i++)
                    for (int j = 0; j < height; j++)
                    {
                        IProviderTile tile = (IProviderTile)Tiles[x + i, y + j];
                        // TODO: If layer is equal then there might be a problem...
                        if (tile == null || tile.Provider.Layer <= layer || !tile.Provider.Enabled)
                            Tiles[x + i, y + j] = Provider[i, j];
                    }

                foreach (INamedTileCollection provider in Providers)
                {
                    Intersect(provider, x, y, width, height,
                        out int x2, out int y2, out int width2, out int height2);
                    if (width2 > 0 && height2 > 0)
                        provider.UpdateSignsChestsEntities();
                }
            }
        }

        #endregion
        #region HideFakeSignsChestsEntities

        public void HideFakeSignsChestsEntities()
        {
            lock (Locker)
                foreach (INamedTileCollection provider in Providers)
                    if (provider.Name != FakeProvider.WorldProviderName)
                        provider.HideSignsChestsEntities();
        }

        #endregion
        #region UpdateFakeSignsChestsEntities

        public void UpdateFakeSignsChestsEntities()
        {
            lock (Locker)
                foreach (INamedTileCollection provider in Providers)
                    if (provider.Name != FakeProvider.WorldProviderName)
                        provider.UpdateSignsChestsEntities();
        }

        #endregion
        #region ScanRectangle

        public void ScanRectangle(int X, int Y, int Width, int Height, INamedTileCollection IgnoreProvider = null)
        {
            lock (Locker)
                foreach (INamedTileCollection provider in Providers)
                    if (provider != IgnoreProvider)
                    {
                        Intersect(provider, X, Y, Width, Height, out int x, out int y, out int width, out int height);
                        if (width > 0 && height > 0)
                            provider.Scan();
                    }
        }

        #endregion

        #region Clamp

        public (int x, int y, int width, int height) Clamp(int X, int Y, int Width, int Height) =>
            (Helper.Clamp(X, 0, this.Width),
            Helper.Clamp(Y, 0, this.Height),
            Helper.Clamp(Width, 0, this.Width - X),
            Helper.Clamp(Height, 0, this.Height - Y));

        #endregion

        #region GetEnumerator

        public IEnumerator<INamedTileCollection> GetEnumerator() =>
            Providers.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            Providers.GetEnumerator();

        #endregion
    }
}