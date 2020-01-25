#region Using
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

        #region Dispose

        public void Dispose()
        {
            lock (Locker)
            {
                foreach (INamedTileCollection provider in Providers)
                    (provider as IDisposable).Dispose();
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
                TileCollection.Draw(true);
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
                    provider.Draw(true);
                }
            }
            if (Cleanup)
                GC.Collect();
            return true;
        }

        #endregion
        #region Move

        public bool Move(string Name, int X, int Y)
        {
            INamedTileCollection provider;
            lock (Locker)
            {
                provider = Providers.FirstOrDefault(p => (p.Name == Name));
                if (provider == null)
                    return false;
            }
            Remove(Name);
            provider.SetXYWH(X, Y, provider.Width, provider.Height);
            Add(provider);
            return true;
        }

        #endregion
        #region Clear

        public void Clear(INamedTileCollection except = null)
        {
            lock (Locker)
            {
                foreach (INamedTileCollection provider in Providers.ToArray())
                    if (provider != except)
                        Remove(provider.Name, false);
            }
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
                for (int i = X; i < X + Width; i++)
                    for (int j = Y; j < Y + Height; j++)
                        if (OffsetX + i < Width && OffsetY + j < Height)
                            Tiles[OffsetX + i, OffsetY + j] = FakeProvider.VoidTile;

                for (short providerIndex = 0; providerIndex < Providers.Count; providerIndex++)
                {
                    INamedTileCollection provider = Providers[providerIndex];
                    if (provider.Enabled)
                    {
                        Intersect(provider, X, Y, Width, Height, out int x, out int y, out int w, out int h);
                        for (int i = 0; i < w; i++)
                            for (int j = 0; j < h; j++)
                                if (OffsetX + x + i < Width && OffsetY + y + j < Height)
                                    Tiles[OffsetX + x + i, OffsetY + y + j] = provider[i, j];
                    }
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
                int layer = TileCollection.Layer;
                int x = TileCollection.X;
                int y = TileCollection.Y;
                int w = TileCollection.Width;
                int h = TileCollection.Height;
                for (int i = 0; i < w; i++)
                    for (int j = 0; j < h; j++)
                        if (OffsetX + x + i < Width && OffsetY + y + j < Height)
                        {
                            IProviderTile tile = (IProviderTile)Tiles[OffsetX + x + i, OffsetY + y + j];
                            // If layer is equal then there might be a problem...
                            if (tile == null || tile.Provider.Layer <= layer || !tile.Provider.Enabled)
                                Tiles[OffsetX + x + i, OffsetY + y + j] = TileCollection[i, j];
                        }
            }
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

        #region GetEnumerator

        public IEnumerator<INamedTileCollection> GetEnumerator() =>
            Providers.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            Providers.GetEnumerator();

        #endregion
    }
}