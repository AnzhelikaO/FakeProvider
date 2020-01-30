#region Using
using OTAPI.Tile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#endregion
namespace FakeProvider
{
    public class TileProviderCollection : ITileCollection, IDisposable
    {
        #region Data

        public const string VoidProviderName = "__void__";
        private List<INamedTileCollection> _Providers = new List<INamedTileCollection>();
        /// <summary> List of all registered providers. </summary>
        public INamedTileCollection[] Providers
        {
            get
            {
                lock (Locker)
                    return _Providers.ToArray();
            }
        }
        /// <summary> <see cref="ProviderIndexes"/>[X, Y] is an index of provider at point (X, Y). </summary>
        private ushort[,] ProviderIndexes;
        /// <summary> World width visible by client. </summary>
        public int Width { get; protected set; }
        /// <summary> World height visible by client. </summary>
        public int Height { get; protected set; }
        /// <summary> Horizontal offset of the loaded world. </summary>

        // TODO: I completely messed up offset.
        public int OffsetX { get; protected set; }
        /// <summary> Vertical offset of the loaded world. </summary>
        public int OffsetY { get; protected set; }
        /// <summary> Tile to be visible outside of all providers. </summary>
        protected object Locker { get; set; } = new object();
        protected INamedTileCollection Void { get; set; }
        public IProviderTile VoidTile { get; protected set; }

        #endregion
        #region Constructor

        public TileProviderCollection() : base() { }

        #endregion

        #region Initialize

        public void Initialize(int Width, int Height, int OffsetX, int OffsetY)
        {
            if (ProviderIndexes != null)
                throw new Exception("Attempt to reinitialize.");

            this.Width = Width;
            this.Height = Height;
            this.OffsetX = OffsetX;
            this.OffsetY = OffsetY;

            ProviderIndexes = new ushort[this.Width, this.Height];
            Void = FakeProvider.CreateReadonlyTileProvider(VoidProviderName, 0, 0, 1, 1,
                new ITile[,] { { new Terraria.Tile() } }, Int32.MinValue);
            VoidTile = Void[0, 0];
        }

        #endregion

        #region operator[,]

        public ITile this[int X, int Y]
        {
            get
            {
                X -= OffsetX;
                Y -= OffsetY;
                return _Providers[ProviderIndexes[X, Y]].GetIncapsulatedTile(X, Y);
            }
            set
            {
                X -= OffsetX;
                Y -= OffsetY;
                _Providers[ProviderIndexes[X, Y]].SetIncapsulatedTile(X, Y, value);
            }
        }

        #endregion
        #region GetTileSafe

        // Offset????
        public IProviderTile GetTileSafe(int X, int Y) => X >= 0 && Y >= 0 && X < Width && Y < Height
            ? (IProviderTile)this[X, Y]
            : VoidTile;

        #endregion

        #region Dispose

        public void Dispose()
        {
            lock (Locker)
            {
                foreach (INamedTileCollection provider in _Providers)
                    provider.Dispose();
            }
        }

        #endregion

        #region operator[]

        public INamedTileCollection this[string Name]
        {
            get
            {
                lock (Locker)
                    return _Providers.FirstOrDefault(p => (p.Name == Name));
            }
        }

        #endregion

        #region Add

        internal void Add(INamedTileCollection Provider)
        {
            lock (Locker)
            {
                if (_Providers.Any(p => (p.Name == Provider.Name)))
                    throw new ArgumentException($"Tile collection '{Provider.Name}' " +
                        "is already in use. Name must be unique.");
                PlaceProviderOnTopOfLayer(Provider);
                Provider.Enable(false);
            }
        }

        #endregion
        #region Remove

        public bool Remove(string Name, bool Draw = true, bool Cleanup = true)
        {
            if (Name == VoidProviderName)
                throw new InvalidOperationException("You cannot remove void provider.");

            lock (Locker)
                using (INamedTileCollection provider = _Providers.FirstOrDefault(p => (p.Name == Name)))
                {
                    if (provider == null)
                        return false;
                    provider.Disable(Draw);
                    _Providers.Remove(provider);
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
                foreach (INamedTileCollection provider in _Providers.ToArray())
                    if (provider != except)
                        Remove(provider.Name, true, false);
            GC.Collect();
        }

        #endregion
        #region SetTop

        public bool SetTop(string Name, bool Draw = true)
        {
            lock (Locker)
            {
                INamedTileCollection provider = _Providers.FirstOrDefault(p => (p.Name == Name));
                if (provider == null)
                    return false;
                provider.SetTop(Draw);
                return true;
            }
        }

        #endregion

        #region PlaceProviderOnTopOfLayer

        internal void PlaceProviderOnTopOfLayer(INamedTileCollection Provider)
        {
            lock (Locker)
            {
                _Providers.Remove(Provider);
                int index = _Providers.FindIndex(p => (p.Layer > Provider.Layer));
                if (index == -1)
                    index = _Providers.Count;
                _Providers.Insert(index, Provider);
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
                        ProviderIndexes[i, j] = 0;

                Queue<INamedTileCollection> providers = new Queue<INamedTileCollection>();
                for (ushort k = 0; k < _Providers.Count; k++)
                {
                    INamedTileCollection provider = _Providers[k];
                    if (provider.Enabled)
                    {
                        // Update tiles
                        Intersect(provider, X, Y, Width, Height, out int x, out int y,
                            out int width, out int height);
                        int dx = x - provider.X;
                        int dy = y - provider.Y;
                        for (int i = 0; i < width; i++)
                            for (int j = 0; j < height; j++)
                                ProviderIndexes[x + i, y + j] = k;

                        if (width > 0 && height > 0)
                            providers.Enqueue(provider);
                    }
                }

                // We are updating all the stuff only after tiles update since signs,
                // chests and entities apply only in case the tile on top is from this provider.
                while (providers.Count > 0)
                {
                    INamedTileCollection provider = providers.Dequeue();
                    provider.UpdateEntities();
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
                ushort providerIndex = (ushort)_Providers.IndexOf(Provider);

                // Scanning rectangle where this provider is/will appear.
                ScanRectangle(Provider.X, Provider.Y, Provider.Width, Provider.Height, Provider);

                // Update tiles
                int layer = Provider.Layer;
                (int x, int y, int width, int height) = Provider.ClampXYWH();

                for (int i = 0; i < width; i++)
                    for (int j = 0; j < height; j++)
                    {
                        ushort providerIndexOnTop = ProviderIndexes[x + i, y + j];
                        IProviderTile tile = _Providers[providerIndexOnTop][x + i, y + j];
                        // TODO: If layer is equal then there might be a problem...
                        if (tile == null || tile.Provider.Layer <= layer || !tile.Provider.Enabled)
                            ProviderIndexes[x + i, y + j] = providerIndex;
                    }

                foreach (INamedTileCollection provider in _Providers)
                {
                    Intersect(provider, x, y, width, height,
                        out int x2, out int y2, out int width2, out int height2);
                    if (width2 > 0 && height2 > 0)
                        provider.UpdateEntities();
                }
            }
        }

        #endregion
        #region HideEntities

        public void HideEntities()
        {
            lock (Locker)
                foreach (INamedTileCollection provider in _Providers)
                    if (provider.Name != FakeProvider.WorldProviderName)
                        provider.HideEntities();
        }

        #endregion
        #region UpdateEntities

        public void UpdateEntities()
        {
            lock (Locker)
                foreach (INamedTileCollection provider in _Providers)
                    if (provider.Name != FakeProvider.WorldProviderName)
                        provider.UpdateEntities();
        }

        #endregion
        #region ScanRectangle

        public void ScanRectangle(int X, int Y, int Width, int Height, INamedTileCollection IgnoreProvider = null)
        {
            lock (Locker)
                foreach (INamedTileCollection provider in _Providers)
                    if (provider != IgnoreProvider)
                    {
                        Intersect(provider, X, Y, Width, Height, out int x, out int y, out int width, out int height);
                        if (width > 0 && height > 0)
                            provider.ScanEntities();
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
    }
}
