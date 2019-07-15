#region Using
using OTAPI.Tile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
#endregion
namespace FakeManager
{
    public class FakeProvider : TerrariaPlugin
    {
        #region Data

        public override string Name => "FakeProvider";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public override string Author => "Anzhelika & ASgo";
        public override string Description => "TODO";

        public static FakeCollection Common { get; } = new FakeCollection();
        //public static FakeCollection[] Personal = new FakeCollection[Main.maxPlayers];
        internal static int[] AllPlayers;

        #endregion

        #region Constructor

        public FakeProvider(Main game) : base(game) =>
            Order = -1002;

        #endregion
        #region Initialize

        public override void Initialize()
        {
            AllPlayers = new int[Main.maxPlayers];
            for (int i = 0; i < Main.maxPlayers; i++)
                AllPlayers[i] = i;

            ServerApi.Hooks.NetGetData.Register(this, OnGetData, 1000000);
            ServerApi.Hooks.NetSendData.Register(this, OnSendData, 1000000);
            /*
            ServerApi.Hooks.ServerJoin.Register(this, OnServerJoin);
            ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
            */
        }

        #endregion
        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
                ServerApi.Hooks.NetSendData.Deregister(this, OnSendData);
                /*
                ServerApi.Hooks.ServerJoin.Deregister(this, OnServerJoin);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnServerLeave);
                */
            }
            base.Dispose(disposing);
        }

        #endregion

        #region OnGetData

        private void OnGetData(GetDataEventArgs args)
        {
            if (args.Handled || args.MsgID != PacketTypes.ChestGetContents)
                return;
            int x = args.Msg.readBuffer[args.Index] + args.Msg.readBuffer[args.Index + 1] * 256;
            int y = args.Msg.readBuffer[args.Index + 2] + args.Msg.readBuffer[args.Index + 3] * 256;
            Chest chest = GetAppliedChest(x, y);
            if (chest != null)
            {
                SendChestItemPacket.SendMany(args.Msg.whoAmI, 999, chest.item);
                SendChestOpenPacket.Send(args.Msg.whoAmI, 999, x, y);
            }
        }

        #endregion
        #region OnSendData

        private void OnSendData(SendDataEventArgs args)
        {
            if (args.Handled)
                return;

            switch (args.MsgId)
            {
                case PacketTypes.TileSendSection:
                    args.Handled = true;
                    if (args.text?._text == null)
                        SendSectionPacket.Send(args.remoteClient, args.ignoreClient,
                            args.number, (int)args.number2, (short)args.number3, (short)args.number4);
                    else
                        SendSectionPacket.Send(args.text._text.Select(c => (int)c), args.ignoreClient,
                            args.number, (int)args.number2, (short)args.number3, (short)args.number4);
                    break;
                case PacketTypes.TileSendSquare:
                    args.Handled = true;
                    if (args.text?._text == null)
                        SendTileSquarePacket.Send(args.remoteClient, args.ignoreClient,
                            args.number, (int)args.number2, (int)args.number3, args.number5);
                    else
                        SendTileSquarePacket.Send(args.text._text.Select(c => (int)c), args.ignoreClient,
                            args.number, (int)args.number2, (int)args.number3, args.number5);
                    break;
            }
        }

        #endregion
        #region OnServerJoin, OnServerLeave
        /*
        private void OnServerJoin(JoinEventArgs args) =>
            Personal[args.Who] = new FakeCollection(true);

        private void OnServerLeave(LeaveEventArgs args)
        {
            FakeCollection collection = Personal[args.Who];
            if (collection == null)
                return;
            collection.Clear();
            Personal[args.Who] = null;
        }
        */
        #endregion

        #region GetAppliedTiles

        public static ITile[,] GetAppliedTiles(int X, int Y, int Width, int Height)
        {
            ITile[,] tiles = new ITile[Width, Height];
            int X2 = (X + Width), Y2 = (Y + Height);
            for (int i = X; i < X2; i++)
                for (int j = Y; j < Y2; j++)
                    tiles[i - X, j - Y] = Main.tile[i, j];

            for (int i = 0; i < Common.Order.Count; i++)
            {
                FakeTileRectangle fake = Common.Data[Common.Order[i]];
                if (fake.Enabled && fake.IsIntersecting(X, Y, Width, Height))
                    fake.ApplyTiles(tiles, X, Y);
            }
            /*
            for (int i = 0; i < Personal[Who].Order.Count; i++)
            {
                FakeTileRectangle fake = Personal[Who].Data[Personal[Who].Order[i]];
                if (fake.Enabled && fake.IsIntersecting(X, Y, Width, Height))
                    fake.ApplyTiles(tiles, X, Y);
            }
            */
            return tiles;
        }

        #endregion
        #region GetAppliedSigns

        public static Dictionary<int, Sign> GetAppliedSigns(int X, int Y, int Width, int Height)
        {
            Dictionary<int, Sign> signs = new Dictionary<int, Sign>();
            int X2 = (X + Width), Y2 = (Y + Height);
            for (int i = 0; i < Main.sign.Length; i++)
            {
                Sign sign = Main.sign[i];
                if ((sign != null) && (sign.x >= X) && (sign.x < X2)
                        && (sign.y >= Y) && (sign.y < Y2))
                    signs.Add(i, sign);
            }

            for (int i = 0; i < Common.Order.Count; i++)
            {
                FakeTileRectangle fake = Common.Data[Common.Order[i]];
                if (fake.Enabled && fake.IsIntersecting(X, Y, Width, Height))
                    fake.ApplySigns(signs, X, Y, Width, Height);
            }
            /*
            for (int i = 0; i < Personal[Who].Order.Count; i++)
            {
                FakeTileRectangle fake = Personal[Who].Data[Personal[Who].Order[i]];
                if (fake.Enabled && fake.IsIntersecting(X, Y, Width, Height))
                    fake.ApplySigns(signs, X, Y, Width, Height);
            }
            */
            return signs;
        }

        #endregion
        #region GetAppliedChest

        public static Chest GetAppliedChest(int X, int Y)
        {
            Chest chest = null;
            for (int i = 0; i < Main.chest.Length; i++)
            {
                Chest ch = Main.chest[i];
                if ((ch != null) && (ch.x == X) && (ch.x == Y))
                {
                    chest = ch;
                    break;
                }
            }

            for (int i = 0; i < Common.Order.Count; i++)
            {
                FakeTileRectangle fake = Common.Data[Common.Order[i]];
                if (fake.Enabled && fake.IsIntersecting(X, Y, 1, 1))
                    fake.ApplyChest(ref chest, X, Y);
            }
            /*
            for (int i = 0; i < Personal[PlayerIndex].Order.Count; i++)
            {
                FakeTileRectangle fake = Personal[PlayerIndex].Data[Personal[PlayerIndex].Order[i]];
                if (fake.Enabled && fake.IsIntersecting(X, Y, Width, Height))
                    fake.ApplyChests(chests, X, Y, Width, Height);
            }
            */
            return chest;
        }

        #endregion
    }
}