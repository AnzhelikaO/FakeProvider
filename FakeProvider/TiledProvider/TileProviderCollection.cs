﻿#region Using
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

        private static List<INamedTileCollection> Providers = new List<INamedTileCollection>();
        private static short[,] ProviderIndex;

        /// <summary> World width visible by client. </summary>
        public int Width { get; }
        /// <summary> World height visible by client. </summary>
        public int Height { get; }
        /// <summary> Horizontal offset of the loaded world. </summary>
        public int OffsetX { get; }
        /// <summary> Vertical offset of the loaded world. </summary>
        public int OffsetY { get; }
        private object Locker { get; } = new object();

        #endregion
        #region Constructor

        public TileProviderCollection(int Width, int Height, int OffsetX, int OffsetY)
        {
            this.Width = Width;
            this.Height = Height;
            this.OffsetX = OffsetX;
            this.OffsetY = OffsetY;
        }

        #endregion

        #region operator[,]

        public ITile this[int X, int Y]
        {
            get =>;
            set =>;
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
                UpdateProviderIndexes(TileCollection);
            }
        }

        #endregion
        #region Remove

        public bool Remove(string Name, bool Cleanup = true)
        {
            lock (Locker)
            {
                INamedTileCollection provider = Providers.FirstOrDefault(p => (p.Name == Name));
                if (provider == null)
                    return false;
                Providers.Remove(provider);
                UpdateProviderIndexes(provider);
                int x = provider.X, y = provider.Y;
                int w = provider.Width, h = provider.Height;
                int x2 = (provider.X + w - 1), y2 = (provider.Y + h - 1);
                int sx1 = Netplay.GetSectionX(x), sy1 = Netplay.GetSectionY(y);
                int sx2 = Netplay.GetSectionX(x2), sy2 = Netplay.GetSectionY(y2);
                provider.Dispose();
                if (Cleanup)
                    GC.Collect();
                NetMessage.SendData((int)PacketTypes.TileSendSection,
                    -1, -1, null, x, y, w, h);
                NetMessage.SendData((int)PacketTypes.TileFrameSection,
                    -1, -1, null, sx1, sy1, sx2, sy2);
                return true;
            }
        }

        #endregion
        #region Clear

        public void Clear()
        {
            lock (Locker)
            {
                List<object> keys = new List<object>(Data.Keys);
                foreach (object key in keys)
                    Remove(key, false);
                GC.Collect();
            }
        }

        #endregion

        #region Intersect

        internal static void Intersect(INamedTileCollection First, INamedTileCollection Second,
            out int RX, out int RY, out int RWidth, out int RHeight)
        {
            int ex1 = Second.X + Second.Width;
            int ex2 = First.X + First.Width;
            int ey1 = Second.Y + Second.Height;
            int ey2 = First.Y + First.Height;
            int maxSX = (Second.X > First.X) ? Second.X : First.X;
            int maxSY = (Second.Y > First.Y) ? Second.Y : First.Y;
            int minEX = (ex1 < ex2) ? ex1 : ex2;
            int minEY = (ey1 < ey2) ? ey1 : ey2;
            RX = maxSX;
            RY = maxSY;
            RWidth = minEX - maxSX;
            RHeight = minEY - maxSY;
        }

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
        #region IsIntersecting

        internal static bool IsIntersecting(INamedTileCollection First, INamedTileCollection Second) =>
            ((First.X < (Second.X + Second.Width)) && (Second.X < (First.X + First.Width))
            && (First.Y < (Second.Y + Second.Height)) && (Second.Y < (First.Y + First.Height)));

        internal static bool IsIntersecting(INamedTileCollection Provider, int X, int Y, int Width, int Height) =>
            ((Provider.X < (X + Width)) && (X < (Provider.X + Provider.Width))
            && (Provider.Y < (Y + Height)) && (Y < (Provider.Y + Provider.Height)));

        #endregion
        #region UpdateProviderIndexes

        public void UpdateProviderIndexes(int X, int Y, int Width, int Height)
        {
            lock (Locker)
                for (short a = 0; a < Providers.Count; a++)
                {
                    INamedTileCollection provider = Providers[a];
                    if (IsIntersecting(provider, X, Y, Width, Height))
                    {
                        Intersect(provider, X, Y, Width, Height, out int x, out int y, out int w, out int h);
                        for (int i = x; i < x + w; i++)
                            for (int j = y; j < y + h; j++)
                                ProviderIndex[i + OffsetX, j + OffsetY] = a;
                    }
                }
        }

        public void UpdateProviderIndexes(INamedTileCollection TileCollection)
        {
            short index = (short)Providers.FindIndex(p => (p == TileCollection));
            int layer = TileCollection.Layer;
            for (int i = TileCollection.X; i < TileCollection.X + TileCollection.Width; i++)
                for (int j = TileCollection.Y; j < TileCollection.Y + TileCollection.Height; j++)
                    if (Providers[ProviderIndex[i + OffsetX, j + OffsetY]].Layer < layer)
                        ProviderIndex[i + OffsetX, j + OffsetY] = index;
        }

        #endregion
        
        #region SetTop

        public void SetTop(object Key)
        {
            lock (Locker)
            {
                if (!Order.Remove(Key))
                    throw new KeyNotFoundException(Key.ToString());
                Order.Add(Key);
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