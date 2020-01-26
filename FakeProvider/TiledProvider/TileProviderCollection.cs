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

        public void Add(INamedTileCollection TileCollection)
        {
            lock (Locker)
            {
                if (Providers.Any(p => (p.Name == TileCollection.Name)))
                    throw new ArgumentException($"Tile collection '{TileCollection.Name}' " +
                        "is already in use. Name must be unique.");
                short index = (short)Providers.FindIndex(p => (p.Layer > TileCollection.Layer));
                if (index == -1)
                    index = (short)Providers.Count;
                Providers.Insert(index, TileCollection);
                UpdateProviderReferences(TileCollection);
            }
        }

        #endregion
        #region Remove

        public bool Remove(string Name, bool Cleanup = true)
        {
            lock (Locker)
            {
                using (INamedTileCollection provider = Providers.FirstOrDefault(p => (p.Name == Name)))
                {
                    if (provider == null)
                        return false;
                    Providers.Remove(provider);
                    UpdateRectangleReferences(provider.X, provider.Y, provider.Width, provider.Height);

                    // TODO: Where should this go?
                    provider.UpdateSigns();
                }
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
                        Remove(provider.Name, false);
            GC.Collect();
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
                    // Update signs
                    provider.UpdateSigns();
                    // Update chests

                    // Update entities
                }
            }
        }

        #endregion
        #region UpdateProviderReferences

        public void UpdateProviderReferences(INamedTileCollection TileCollection)
        {
            if (!TileCollection.Enabled)
                return;
            lock (Locker)
            {
                // Update tiles
                int layer = TileCollection.Layer;
                (int x, int y, int width, int height) = Clamp(TileCollection.X, TileCollection.Y,
                    TileCollection.Width, TileCollection.Height);

                for (int i = 0; i < width; i++)
                    for (int j = 0; j < height; j++)
                    {
                        IProviderTile tile = (IProviderTile)Tiles[x + i, y + j];
                        // If layer is equal then there might be a problem...
                        if (tile == null || tile.Provider.Layer <= layer || !tile.Provider.Enabled)
                            Tiles[x + i, y + j] = TileCollection[i, j];
                    }

                // Update signs
                TileCollection.UpdateSigns();
                //TileCollection.ApplySigns(x, y, width, height);

                // Update chests

                // Update entities

            }
        }

        #endregion
        #region HideSignsChestsEntities

        public void HideSignsChestsEntities()
        {
            lock (Locker)
                foreach (INamedTileCollection provider in Providers)
                    provider.HideSignsChestsEntities();
        }

        #endregion
        #region ShowSignsChestsEntities

        public void ShowSignsChestsEntities()
        {
            lock (Locker)
                foreach (INamedTileCollection provider in Providers)
                    provider.ShowSignsChestsEntities();
        }

        #endregion

        #region SetTop

        public void SetTop(string Name)
        {
            lock (Locker)
            {
                INamedTileCollection provider = Providers.FirstOrDefault(p => (p.Name == Name));
                if (provider == null)
                    return;
                Remove(provider.Name);
                Add(provider);
            }
        }

        #endregion

        #region Clamp

        private (int x, int y, int width, int height) Clamp(int X, int Y, int Width, int Height) =>
            (Clamp(X, 0, this.Width),
            Clamp(Y, 0, this.Height),
            Clamp(Width, 0, this.Width - X),
            Clamp(Height, 0, this.Height - Y));

        private int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);

        #endregion

        #region GetEnumerator

        public IEnumerator<INamedTileCollection> GetEnumerator() =>
            Providers.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            Providers.GetEnumerator();

        #endregion
    }
}