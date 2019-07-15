#region Using
using OTAPI.Tile;
using System;
using System.Collections.Generic;
using System.Linq;
#endregion
namespace FakeProvider
{
    public class MainTileProvider : ITileCollection, IDisposable
    {
        #region Data

        private static List<ITileCollection2> Provider = new List<ITileCollection2>();
        private static short[,] ProviderIndex;

        /// <summary> World width visible by client. </summary>
        public int Width { get; }
        /// <summary> World height visible by client. </summary>
        public int Height { get; }
        /// <summary> Real loaded world width. </summary>
        public int WorldWidth { get; }
        /// <summary> Real loaded world height. </summary>
        public int WorldHeight { get; }
        private object Locker { get; } = new object();

        #endregion
        #region Constructor

        public MainTileProvider(int X, int Y,
            int Width, int Height, int WorldWidth, int WorldHeight)
        {

            this.Width = Width;
            this.Height = Height;
            this.WorldWidth = WorldWidth;
            this.WorldHeight = WorldHeight;
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
            foreach (ITileCollection2 provider in Provider)
                (provider as IDisposable).Dispose();
        }

        #endregion
        
        #region operator[]

        public ITileCollection2 this[string Name] =>
            Provider.FirstOrDefault(p => (p.Name == Name));

        #endregion

        #region Add

        public FakeTileRectangle Add(object Key, int X, int Y,
            int Width, int Height, ITileCollection CopyFrom = null)
        {
            lock (Locker)
            {
                if (Data.ContainsKey(Key))
                    throw new ArgumentException($"Key '{Key}' is already in use.");
                FakeTileRectangle fake = new FakeTileRectangle(this,
                    Key, X, Y, Width, Height, CopyFrom);
                Data.Add(Key, fake);
                Order.Add(Key);
                return fake;
            }
        }

        #endregion
        #region Remove

        public bool Remove(object Key, bool Cleanup = true)
        {
            lock (Locker)
            {
                if (!Data.ContainsKey(Key))
                    return false;
                FakeTileRectangle o = Data[Key];
                Data.Remove(Key);
                Order.Remove(Key);
                int x = o.X, y = o.Y;
                int w = (o.X + o.Width - 1), h = (o.Y + o.Height - 1);
                int sx1 = Netplay.GetSectionX(x), sy1 = Netplay.GetSectionY(y);
                int sx2 = Netplay.GetSectionX(w), sy2 = Netplay.GetSectionY(h);
                o.Tile.Dispose();
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
    }
}