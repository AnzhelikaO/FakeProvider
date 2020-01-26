#region Using
using OTAPI.Tile;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Terraria;
using Terraria.ID;
#endregion
namespace FakeProvider
{
    public sealed class ReadonlyTileProvider<T> : INamedTileCollection
    {
        #region Data

        public TileProviderCollection ProviderCollection { get; }
        private ReadonlyTile<T>[,] Data;
        public string Name { get; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Layer { get; }
        public bool Enabled { get; private set; } = true;
        private List<FakeSign> _Signs = new List<FakeSign>();
        public ReadOnlyCollection<FakeSign> Signs => new ReadOnlyCollection<FakeSign>(_Signs);
        private object Locker = new object();

        #endregion
        #region Constructor

        public ReadonlyTileProvider(TileProviderCollection ProviderCollection, string Name, int X, int Y,
            int Width, int Height, int Layer = 0)
        {
            this.ProviderCollection = ProviderCollection;
            this.Name = Name;
            this.Data = new ReadonlyTile<T>[Width, Height];
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
            this.Layer = Layer;

            for (int x = 0; x < this.Width; x++)
                for (int y = 0; y < this.Height; y++)
                    Data[x, y] = new ReadonlyTile<T>();
        }

        #region ITileCollection

        public ReadonlyTileProvider(TileProviderCollection ProviderCollection, string Name, int X, int Y,
            int Width, int Height, ITileCollection CopyFrom, int Layer = 0)
        {
            this.ProviderCollection = ProviderCollection;
            this.Name = Name;
            this.Data = new ReadonlyTile<T>[Width, Height];
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
            this.Layer = Layer;

            if (CopyFrom != null)
                for (int i = X; i < X + Width; i++)
                    for (int j = Y; j < Y + Height; j++)
                    {
                        ITile t = CopyFrom[i, j];
                        if (t != null)
                            Data[i - X, j - Y] = new ReadonlyTile<T>(t);
                    }
        }

        #endregion
        #region ITile[,]

        public ReadonlyTileProvider(TileProviderCollection ProviderCollection, string Name, int X, int Y,
            int Width, int Height, ITile[,] CopyFrom, int Layer = 0)
        {
            this.ProviderCollection = ProviderCollection;
            this.Name = Name;
            this.Data = new ReadonlyTile<T>[Width, Height];
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
                        Data[i, j] = new ReadonlyTile<T>(t);
                }
        }

        #endregion

        #endregion

        #region operator[,]

        ITile ITileCollection.this[int X, int Y]
        {
            get => Data[X, Y];
            set { }
        }

        public IProviderTile this[int X, int Y]
        {
            get => Data[X, Y];
            set { }
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
                ReadonlyTile<T>[,] newData = new ReadonlyTile<T>[Width, Height];
                for (int i = 0; i < Width; i++)
                    for (int j = 0; j < Height; j++)
                        if ((i < this.Width) && (j < this.Height))
                            newData[i, j] = Data[i, j];
                        else
                            newData[i, j] = new ReadonlyTile<T>();
                this.Data = newData;
                this.Width = Width;
                this.Height = Height;
            }
        }

        #endregion
        #region Move

        public void Move(int X, int Y)
        {
            Disable();
            SetXYWH(X, Y, this.Width, this.Height);
            Enable();
        }

        #endregion
        #region Enable

        public void Enable()
        {
            if (!Enabled)
            {
                Enabled = true;
                ProviderCollection.UpdateProviderReferences(this);
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
                // Remove signs, chests, entities
                ProviderCollection.UpdateRectangleReferences(X, Y, Width, Height);
                Draw(true);
            }
        }

        #endregion
        #region HideSignsChestsEntities

        public void HideSignsChestsEntities()
        {
            lock (Locker)
            {
                foreach (FakeSign sign in _Signs)
                    HideSign(sign);
            }
        }

        #endregion
        #region ShowSignsChestsEntities

        public void ShowSignsChestsEntities()
        {
            lock (Locker)
            {
                foreach (FakeSign sign in _Signs)
                    UpdateSign(sign);
            }
        }

        #endregion

        #region AddSign

        public FakeSign AddSign(int X, int Y, string Text)
        {
            FakeSign sign = new FakeSign(this, -1, X, Y, Text);
            lock (Locker)
            {
                if (_Signs.Find(s => s.x == sign.x && s.y == sign.y) != null)
                    throw new Exception("Sign with such coordinates already exists in this tile provider.");
                _Signs.Add(sign);
            }
            UpdateSigns();
            return sign;
        }

        #endregion
        #region RemoveSign

        public void RemoveSign(FakeSign Sign)
        {
            lock (Locker)
            {
                HideSign(Sign);
                if (!_Signs.Remove(Sign))
                    throw new Exception("No such sign in this tile provider.");
            }
        }

        #endregion
        #region UpdateSigns

        public void UpdateSigns()
        {
            lock (Locker)
                foreach (FakeSign sign in _Signs)
                    UpdateSign(sign);
        }

        /*public void UpdateSigns(int X, int Y, int Width, int Height)
        {
            lock (Locker)
                foreach (FakeSign sign in _Signs.Where(s =>
                    s.RelativeX)
                {
                    int signX = this.X + sign.RelativeX;
                    int signY = this.Y + sign.RelativeY;
                    if (ProviderCollection.GetTileSafe(signX, signY).Provider == this
                            && !ApplySign(sign))
                        break;
                }
        }*/

        #endregion
        #region UpdateSign

        public bool UpdateSign(FakeSign Sign)
        {
            if (ProviderCollection.GetTileSafe(this.X + Sign.RelativeX, this.Y + Sign.RelativeY).Provider == this)
                return ApplySign(Sign);
            else
                HideSign(Sign);
            return true;
        }

        #endregion
        #region ApplySign

        private bool ApplySign(FakeSign Sign)
        {
            if (Sign.Index >= 0 && Main.sign[Sign.Index] == Sign)
            {
                Sign.x = ProviderCollection.OffsetX + this.X + Sign.RelativeX;
                Sign.y = ProviderCollection.OffsetY + this.Y + Sign.RelativeY;
                return true;
            }
            else
            {
                bool applied = false;
                for (int i = 0; i < 1000; i++)
                {
                    if (Main.sign[i] != null && Main.sign[i].x == Sign.x && Main.sign[i].y == Sign.y)
                        Main.sign[i] = null;
                    if (!applied && Main.sign[i] == null)
                    {
                        applied = true;
                        Main.sign[i] = Sign;
                        Sign.Index = i;

                        // DEBUG
                        ReadonlyTile<T> t = Data[Sign.x - FakeProvider.OffsetX - this.X, Sign.y - FakeProvider.OffsetY - this.Y];
                        Tile t2 = new Tile();
                        t2.active(true);
                        t2.type = (ushort)TileID.Signs;
                        t.ForceCopyFrom(t2);
                    }
                }
                return applied;
            }
        }

        #endregion
        #region HideSign

        private void HideSign(FakeSign sign)
        {
            Console.WriteLine($"Removing FakeSign at {sign.x}, {sign.y} from {Name}");
            if (sign.Index >= 0 && Main.sign[sign.Index] == sign)
            {
                Console.WriteLine($"Removing success!!!");
                Main.sign[sign.Index] = null;
            }
        }

        #endregion

        #region Draw

        public void Draw(bool Section = true)
        {
            if (Section)
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