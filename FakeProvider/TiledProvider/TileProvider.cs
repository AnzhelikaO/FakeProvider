#region Using
using OTAPI.Tile;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
#endregion
namespace FakeProvider
{
    public sealed class TileProvider<T> : INamedTileCollection
    {
        #region Data

        public TileProviderCollection ProviderCollection { get; internal set; }
        private Tile<T>[,] Data;
        public string Name { get; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Layer { get; }
        public bool Enabled { get; private set; } = true;
        private List<FakeSign> _Signs = new List<FakeSign>();
        public ReadOnlyCollection<FakeSign> Signs => new ReadOnlyCollection<FakeSign>(_Signs);
        private List<FakeChest> _Chests = new List<FakeChest>();
        public ReadOnlyCollection<FakeChest> Chests => new ReadOnlyCollection<FakeChest>(_Chests);
        private object Locker = new object();

        #endregion
        #region Constructor

        public TileProvider(TileProviderCollection ProviderCollection, string Name, int X, int Y,
            int Width, int Height, int Layer = 0)
        {
            this.ProviderCollection = ProviderCollection;
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

        public TileProvider(TileProviderCollection ProviderCollection, string Name, int X, int Y,
            int Width, int Height, ITileCollection CopyFrom, int Layer = 0)
        {
            this.ProviderCollection = ProviderCollection;
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

        public TileProvider(TileProviderCollection ProviderCollection, string Name, int X, int Y,
            int Width, int Height, ITile[,] CopyFrom, int Layer = 0)
        {
            this.ProviderCollection = ProviderCollection;
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
                foreach (FakeChest chest in _Chests)
                    HideChest(chest);
            }
        }

        #endregion
        #region UpdateSignsChestsEntities

        public void UpdateSignsChestsEntities()
        {
            UpdateSigns();
            UpdateChests();
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
                        Tile<T> t = Data[Sign.x - FakeProvider.OffsetX - this.X, Sign.y - FakeProvider.OffsetY - this.Y];
                        t.active(true);
                        t.type = (ushort)TileID.Signs;
                    }
                }
                return applied;
            }
        }

        #endregion
        #region HideSign

        private void HideSign(FakeSign sign)
        {
            if (sign.Index >= 0 && Main.sign[sign.Index] == sign)
                Main.sign[sign.Index] = null;
        }

        #endregion

        #region AddChest

        public FakeChest AddChest(int X, int Y, Item[] Items = null)
        {
            FakeChest chest = new FakeChest(this, -1, X, Y, Items);
            lock (Locker)
            {
                if (_Chests.Find(c => c.x == chest.x && c.y == chest.y) != null)
                    throw new Exception("Sign with such coordinates already exists in this tile provider.");
                _Chests.Add(chest);
            }
            UpdateChests();
            return chest;
        }

        #endregion
        #region RemoveChest

        public void RemoveChest(FakeChest Chest)
        {
            lock (Locker)
            {
                HideChest(Chest);
                if (!_Chests.Remove(Chest))
                    throw new Exception("No such sign in this tile provider.");
            }
        }

        #endregion
        #region UpdateChests

        public void UpdateChests()
        {
            lock (Locker)
                foreach (FakeChest chest in _Chests)
                    UpdateChest(chest);
        }

        #endregion
        #region UpdateChest

        public bool UpdateChest(FakeChest Chest)
        {
            if (ProviderCollection.GetTileSafe(this.X + Chest.RelativeX, this.Y + Chest.RelativeY).Provider == this)
                return ApplyChest(Chest);
            else
                HideChest(Chest);
            return true;
        }

        #endregion
        #region ApplyChest

        private bool ApplyChest(FakeChest Chest)
        {
            if (Chest.Index >= 0 && Main.chest[Chest.Index] == Chest)
            {
                Chest.x = ProviderCollection.OffsetX + this.X + Chest.RelativeX;
                Chest.y = ProviderCollection.OffsetY + this.Y + Chest.RelativeY;
                return true;
            }
            else
            {
                bool applied = false;
                for (int i = 0; i < 1000; i++)
                {
                    if (Main.chest[i] != null && Main.chest[i].x == Chest.x && Main.chest[i].y == Chest.y)
                        Main.chest[i] = null;
                    if (!applied && Main.chest[i] == null)
                    {
                        applied = true;
                        Main.chest[i] = Chest;
                        Chest.Index = i;

                        // DEBUG
                        Tile<T> t = Data[Chest.x - FakeProvider.OffsetX - this.X, Chest.y - FakeProvider.OffsetY - this.Y];
                        t.active(true);
                        t.type = (ushort)TileID.Containers;
                    }
                }
                return applied;
            }
        }

        #endregion
        #region HideSign

        private void HideChest(FakeChest Chest)
        {
            if (Chest.Index >= 0 && Main.chest[Chest.Index] == Chest)
                Main.chest[Chest.Index] = null;
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
            Disable();
            Data = null;
        }

        #endregion
    }
}