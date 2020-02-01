#region Using
using OTAPI.Tile;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Tile_Entities;
#endregion
namespace FakeProvider
{
    public sealed class TileProvider<T> : INamedTileCollection
    {
        #region Data

        public TileProviderCollection ProviderCollection { get; internal set; }
        private StructTile[,] Data;
        public INamedTileCollection Tile => this;
        public string Name { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Index { get; internal set; } = -1;
        public int Order { get; internal set; } = -1;
        public int Layer { get; private set; }
        public bool Enabled { get; private set; } = false;
        private List<IFake> _Entities = new List<IFake>();
        public ReadOnlyCollection<IFake> Entities => new ReadOnlyCollection<IFake>(_Entities);
        private object Locker = new object();

        #endregion
        #region Constructor

        internal TileProvider() { }

        #endregion
        #region Initialize

        internal void Initialize(string Name, int X, int Y, int Width, int Height, int Layer = 0)
        {
            this.Name = Name;
            this.Data = new StructTile[Width, Height];
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
            this.Layer = Layer;
        }

        internal void Initialize(string Name, int X, int Y, int Width, int Height, ITileCollection CopyFrom, int Layer = 0)
        {
            this.Name = Name;
            this.Data = new StructTile[Width, Height];
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
                        this[i, j].CopyFrom(t);
                }
        }

        internal void Initialize(string Name, int X, int Y, int Width, int Height, ITile[,] CopyFrom, int Layer = 0)
        {
            this.Name = Name;
            this.Data = new StructTile[Width, Height];
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
                        this[i, j].CopyFrom(t);
                }
        }

        #endregion

        #region operator[,]

        ITile ITileCollection.this[int X, int Y]
        {
            get => new TileReference<T>(Data, X, Y);
            set => new TileReference<T>(Data, X, Y).CopyFrom(value);
        }

        public IProviderTile this[int X, int Y]
        {
            get => new TileReference<T>(Data, X, Y);
            set => new TileReference<T>(Data, X, Y).CopyFrom(value);
        }

        #endregion
        #region GetIncapsulatedTile

        public IProviderTile GetIncapsulatedTile(int X, int Y) =>
            new TileReference<T>(Data, X - this.X, Y - this.Y);

        #endregion
        #region SetIncapsulatedTile

        public void SetIncapsulatedTile(int X, int Y, ITile Tile) =>
            new TileReference<T>(Data, X - this.X, Y - this.Y).CopyFrom(Tile);

        #endregion
        #region GetTileSafe

        public IProviderTile GetTileSafe(int X, int Y) => X >= 0 && Y >= 0 && X < Width && Y < Height
            ? this[X, Y]
            : ProviderCollection.VoidTile;

        #endregion

        #region XYWH

        public (int X, int Y, int Width, int Height) XYWH(int DeltaX = 0, int DeltaY = 0) =>
            (X + DeltaX, Y + DeltaY, Width, Height);

        #endregion
        #region ClampXYWH

        public (int X, int Y, int Width, int Height) ClampXYWH() =>
            (ProviderCollection.Clamp(X, Y, Width, Height));

        #endregion
        #region SetXYWH

        public void SetXYWH(int X, int Y, int Width, int Height, bool Draw = true)
        {
            bool wasEnabled = Enabled;
            if (wasEnabled)
                Disable(Draw);

            this.X = X;
            this.Y = Y;
            if ((this.Width != Width) || (this.Height != Height))
            {
                if (Width <= 0 || Height <= 0)
                    throw new ArgumentException("Invalid new width or height.");

                StructTile[,] newData = new StructTile[Width, Height];
                for (int i = 0; i < Width; i++)
                    for (int j = 0; j < Height; j++)
                        if ((i < this.Width) && (j < this.Height))
                            newData[i, j] = Data[i, j];
                this.Data = newData;
                this.Width = Width;
                this.Height = Height;
            }

            if (wasEnabled)
                Enable(Draw);
        }

        #endregion
        #region Move

        public void Move(int X, int Y, bool Draw = true) =>
            SetXYWH(X, Y, this.Width, this.Height, Draw);

        #endregion
        #region Resize

        public void Resize(int Width, int Height, bool Draw = true) =>
            SetXYWH(this.X, this.Y, Width, Height, Draw);

        #endregion
        #region Enable

        public void Enable(bool Draw = true)
        {
            if (!Enabled)
            {
                Enabled = true;
                ProviderCollection.UpdateProviderReferences(this);
                if (Draw)
                    this.Draw(true);
            }
        }

        #endregion
        #region Disable

        public void Disable(bool Draw = true)
        {
            if (Enabled)
            {
                Enabled = false;
                // Adding/removing manually added/removed signs, chests and entities
                ScanEntities();
                // Remove signs, chests, entities
                HideEntities();
                // Showing tiles, signs, chests and entities under the provider
                ProviderCollection.UpdateRectangleReferences(X, Y, Width, Height, Index);
                if (Draw)
                    this.Draw(true);
            }
        }

        #endregion
        #region SetTop

        public void SetTop(bool Draw = true)
        {
            ProviderCollection.PlaceProviderOnTopOfLayer(this);
            ProviderCollection.UpdateProviderReferences(this);
            if (Draw)
                this.Draw();
        }

        #endregion
        #region Update

        public void Update()
        {
        }

        #endregion

        #region AddSign

        public FakeSign AddSign(int X, int Y, string Text)
        {
            FakeSign sign = new FakeSign(this, -1, X, Y, Text);
            lock (Locker)
                _Entities.Add(sign);
            UpdateEntity(sign);
            return sign;
        }

        #endregion
        #region AddChest

        public FakeChest AddChest(int X, int Y, Item[] Items = null)
        {
            FakeChest chest = new FakeChest(this, -1, X, Y, Items);
            lock (Locker)
                _Entities.Add(chest);
            UpdateEntity(chest);
            return chest;
        }

        #endregion
        #region AddTrainingDummy

        public FakeTrainingDummy AddTrainingDummy(int X, int Y)
        {
            FakeTrainingDummy dummy = new FakeTrainingDummy(this, -1, X, Y);
            lock (Locker)
                _Entities.Add(dummy);
            UpdateEntity(dummy);
            return dummy;
        }

        #endregion
        #region AddItemFrame

        public FakeItemFrame AddItemFrame(int X, int Y, Item Item = null)
        {
            FakeItemFrame itemFrame = new FakeItemFrame(this, -1, X, Y, Item);
            lock (Locker)
                _Entities.Add(itemFrame);
            UpdateEntity(itemFrame);
            return itemFrame;
        }

        #endregion
        #region AddLogicSensor

        public FakeLogicSensor AddLogicSensor(int X, int Y, TELogicSensor.LogicCheckType LogicCheckType)
        {
            FakeLogicSensor sensor = new FakeLogicSensor(this, -1, X, Y, LogicCheckType);
            lock (Locker)
                _Entities.Add(sensor);
            UpdateEntity(sensor);
            return sensor;
        }

        #endregion
        #region AddEntity

        public FakeSign AddEntity(Sign Entity)
        {
            int x = Entity.x - ProviderCollection.OffsetX - this.X;
            int y = Entity.y - ProviderCollection.OffsetY - this.Y;
            FakeSign sign = new FakeSign(this, Array.IndexOf(Main.sign, Entity), x, y, Entity.text);
            lock (Locker)
                _Entities.Add(sign);
            UpdateEntity(sign);
            return sign;
        }

        public FakeChest AddEntity(Chest Entity)
        {
            int x = Entity.x - ProviderCollection.OffsetX - this.X;
            int y = Entity.y - ProviderCollection.OffsetY - this.Y;
            FakeChest chest = new FakeChest(this, Array.IndexOf(Main.chest, Entity), x, y, Entity.item);
            lock (Locker)
                _Entities.Add(chest);
            UpdateEntity(chest);
            return chest;
        }

        public IFake AddEntity(TileEntity Entity) =>
            Entity is TETrainingDummy trainingDummy
                ? (IFake)AddEntity(trainingDummy)
                : Entity is TEItemFrame itemFrame
                    ? (IFake)AddEntity(itemFrame)
                    : Entity is TELogicSensor logicSensor
                        ? (IFake)AddEntity(logicSensor)
                        : throw new ArgumentException($"Unknown entity type {Entity.GetType().Name}", nameof(Entity));

        public FakeTrainingDummy AddEntity(TETrainingDummy Entity)
        {
            int x = Entity.Position.X - ProviderCollection.OffsetX - this.X;
            int y = Entity.Position.Y - ProviderCollection.OffsetY - this.Y;
            TileEntity.ByID.Remove(Entity.ID);
            TileEntity.ByPosition.Remove(Entity.Position);
            FakeTrainingDummy fake = new FakeTrainingDummy(this, Entity.ID, x, y, Entity.npc);
            lock (Locker)
                _Entities.Add(fake);
            UpdateEntity(fake);
            return fake;
        }

        public FakeItemFrame AddEntity(TEItemFrame Entity)
        {
            int x = Entity.Position.X - ProviderCollection.OffsetX - this.X;
            int y = Entity.Position.Y - ProviderCollection.OffsetY - this.Y;
            TileEntity.ByID.Remove(Entity.ID);
            TileEntity.ByPosition.Remove(Entity.Position);
            FakeItemFrame fake = new FakeItemFrame(this, Entity.ID, x, y, Entity.item);
            lock (Locker)
                _Entities.Add(fake);
            UpdateEntity(fake);
            return fake;
        }

        public FakeLogicSensor AddEntity(TELogicSensor Entity)
        {
            int x = Entity.Position.X - ProviderCollection.OffsetX - this.X;
            int y = Entity.Position.Y - ProviderCollection.OffsetY - this.Y;
            TileEntity.ByID.Remove(Entity.ID);
            TileEntity.ByPosition.Remove(Entity.Position);
            FakeLogicSensor fake = new FakeLogicSensor(this, Entity.ID, x, y, Entity.logicCheck);
            lock (Locker)
                _Entities.Add(fake);
            UpdateEntity(fake);
            return fake;
        }

        #endregion
        #region RemoveEntity

        public void RemoveEntity(IFake Entity)
        {
            lock (Locker)
            {
                HideEntity(Entity);
                if (!_Entities.Remove(Entity))
                    throw new Exception("No such entity in this tile provider.");
            }
        }

        #endregion
        #region UpdateEntities

        public void UpdateEntities()
        {
            lock (Locker)
                foreach (IFake entity in _Entities.ToArray())
                    UpdateEntity(entity);
        }

        #endregion
        #region HideEntities

        public void HideEntities()
        {
            lock (Locker)
                foreach (IFake entity in _Entities.ToArray())
                    HideEntity(entity);
        }

        #endregion
        #region UpdateEntity

        private bool UpdateEntity(IFake Entity)
        {
            if (IsEntityTile(Entity.RelativeX, Entity.RelativeY, Entity.TileTypes)
                    && TileOnTop(Entity.RelativeX, Entity.RelativeY))
                return ApplyEntity(Entity);
            else
                HideEntity(Entity);
            return true;
        }

        #endregion
        #region ApplyEntity

        private bool ApplyEntity(IFake Entity)
        {
            if (Entity is FakeSign)
            {
                Entity.X = ProviderCollection.OffsetX + this.X + Entity.RelativeX;
                Entity.Y = ProviderCollection.OffsetY + this.Y + Entity.RelativeY;
                if (Entity.Index >= 0 && Main.sign[Entity.Index] == Entity)
                    return true;

                bool applied = false;
                for (int i = 0; i < 1000; i++)
                {
                    if (Main.sign[i] != null && Main.sign[i].x == Entity.X && Main.sign[i].y == Entity.Y)
                        Main.sign[i] = null;
                    if (!applied && Main.sign[i] == null)
                    {
                        applied = true;
                        Main.sign[i] = (FakeSign)Entity;
                        Entity.Index = i;
                    }
                }
                return applied;
            }
            else if (Entity is FakeChest)
            {
                Entity.X = ProviderCollection.OffsetX + this.X + Entity.RelativeX;
                Entity.Y = ProviderCollection.OffsetY + this.Y + Entity.RelativeY;
                if (Entity.Index >= 0 && Main.chest[Entity.Index] == Entity)
                    return true;

                bool applied = false;
                for (int i = 0; i < 1000; i++)
                {
                    if (Main.chest[i] != null && Main.chest[i].x == Entity.X && Main.chest[i].y == Entity.Y)
                        Main.chest[i] = null;
                    if (!applied && Main.chest[i] == null)
                    {
                        applied = true;
                        Main.chest[i] = (FakeChest)Entity;
                        Entity.Index = i;
                    }
                }
                return applied;
            }
            else if (Entity is TileEntity)
            {
                Point16 position = new Point16(Entity.X, Entity.Y);
                if (TileEntity.ByPosition.TryGetValue(position, out TileEntity entity)
                        && entity == Entity)
                    TileEntity.ByPosition.Remove(position);
                Entity.X = ProviderCollection.OffsetX + this.X + Entity.RelativeX;
                Entity.Y = ProviderCollection.OffsetY + this.Y + Entity.RelativeY;
                TileEntity.ByPosition[new Point16(Entity.X, Entity.Y)] = (TileEntity)Entity;
                if (Entity.Index < 0)
                    Entity.Index = TileEntity.AssignNewID();
                TileEntity.ByID[Entity.Index] = (TileEntity)Entity;
                return true;
            }
            else
                throw new ArgumentException($"Unknown entity type {Entity.GetType().Name}", nameof(Entity));
        }

        #endregion
        #region HideEntity

        private void HideEntity(IFake Entity)
        {
            if (Entity is Sign)
            {
                if (Entity.Index >= 0 && Main.sign[Entity.Index] == Entity)
                    Main.sign[Entity.Index] = null;
            }
            else if (Entity is Chest)
            {
                if (Entity.Index >= 0 && Main.chest[Entity.Index] == Entity)
                    Main.chest[Entity.Index] = null;
            }
            else if (Entity is TileEntity entity)
            {
                TileEntity.ByID.Remove(Entity.Index);
                if (Entity.Index >= 0
                        && TileEntity.ByPosition.TryGetValue(entity.Position, out TileEntity entity2)
                        && entity == entity2)
                    TileEntity.ByPosition.Remove(entity.Position);

                if (Entity is TETrainingDummy trainingDummy && trainingDummy.npc >= 0)
                {
                    NPC npc = Main.npc[trainingDummy.npc];
                    npc.type = 0;
                    npc.active = false;
                    NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, null, trainingDummy.npc);
                    trainingDummy.npc = -1;
                }
            }
            else
                throw new ArgumentException($"Unknown entity type {Entity.GetType().Name}", nameof(Entity));
        }

        #endregion
        #region GetEntityTileTypes

        private ushort[] GetEntityTileTypes(TileEntity Entity) =>
            Entity is TETrainingDummy
                ? FakeTrainingDummy._TileTypes
                : Entity is TEItemFrame
                    ? FakeItemFrame._TileTypes
                    : Entity is TELogicSensor
                        ? FakeLogicSensor._TileTypes
                        : throw new ArgumentException($"Unknown entity type {Entity.GetType().Name}", nameof(Entity));

        #endregion
        #region ScanEntities

        public void ScanEntities()
        {
            lock (Locker)
                foreach (IFake entity in _Entities.ToArray())
                    if (!IsEntityTile(entity.RelativeX, entity.RelativeY, entity.TileTypes))
                        RemoveEntity(entity);

            (int x, int y, int width, int height) = XYWH(ProviderCollection.OffsetX, ProviderCollection.OffsetY);
            for (int i = 0; i < 1000; i++)
            {
                Sign sign = Main.sign[i];
                if (sign == null)
                    continue;

                if (sign.GetType().Name == nameof(Sign) // <=> not FakeSign or some other inherited type
                    && Helper.Inside(sign.x, sign.y, x, y, width, height)
                    && TileOnTop(sign.x - this.X, sign.y - this.Y))
                {
                    if (IsEntityTile(sign.x - this.X, sign.y - this.Y, FakeSign._TileTypes))
                        AddEntity(sign);
                    else
                        Main.sign[i] = null;
                }
            }

            for (int i = 0; i < 1000; i++)
            {
                Chest chest = Main.chest[i];
                if (chest == null)
                    continue;

                if (chest.GetType().Name == nameof(Chest) // <=> not FakeChest or some other inherited type
                    && Helper.Inside(chest.x, chest.y, x, y, width, height)
                    && TileOnTop(chest.x - this.X, chest.y - this.Y))
                {
                    if (IsEntityTile(chest.x - this.X, chest.y - this.Y, FakeChest._TileTypes))
                        AddEntity(chest);
                    else
                        Main.chest[i] = null;
                }
            }

            foreach (TileEntity entity in TileEntity.ByID.Values.ToArray())
            {
                int entityX = entity.Position.X;
                int entityY = entity.Position.Y;

                if ((entity.GetType().Name == nameof(TETrainingDummy)      // <=> not FakeTrainingDummy or some other inherited type
                        || entity.GetType().Name == nameof(TEItemFrame)    // <=> not FakeItemFrame or some other inherited type
                        || entity.GetType().Name == nameof(TELogicSensor)) // <=> not FakeLogicSensor or some other inherited type
                    && Helper.Inside(entityX, entityY, x, y, width, height)
                    && TileOnTop(entityX - this.X, entityY - this.Y))
                {
                    if (IsEntityTile(entityX - this.X, entityY - this.Y, GetEntityTileTypes(entity)))
                        AddEntity(entity);
                    else
                    {
                        TileEntity.ByID.Remove(entity.ID);
                        TileEntity.ByPosition.Remove(entity.Position);
                    }
                }
            }
        }

        #endregion
        #region IsEntityTile

        private bool IsEntityTile(int X, int Y, ushort[] TileTypes)
        {
            ITile providerTile = GetTileSafe(X, Y);
            return providerTile.active() && TileTypes.Contains(providerTile.type);
        }

        #endregion
        #region TileOnTop

        private bool TileOnTop(int X, int Y) =>
            ProviderCollection.GetTileSafe(this.X + X, this.Y + Y).Provider == this;

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
