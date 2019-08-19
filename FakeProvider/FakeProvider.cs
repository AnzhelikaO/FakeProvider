#region Using
using OTAPI.Tile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
#endregion
namespace FakeProvider
{
    public class FakeProvider : TerrariaPlugin
    {
        #region Data

        public override string Name => "FakeProvider";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public override string Author => "Anzhelika & ASgo";
        public override string Description => "TODO";

        public static TileProviderCollection Tile { get; }
        internal static int[] AllPlayers;

        #endregion

        #region Constructor

        public FakeProvider(Main game) : base(game)
        {
            Order = -1002;
            string[] args = Environment.GetCommandLineArgs();
            #region Offset

            int requiredOffsetX = 0, requiredOffsetY = 0;
            int argumentIndex = Array.FindIndex(args, (x => (x.ToLower() == "-offsetx")));
            if (argumentIndex > -1)
            {
                argumentIndex++;
                if ((argumentIndex >= args.Length)
                        || !int.TryParse(args[argumentIndex], out requiredOffsetX))
                    Console.WriteLine("Please provide a offsetX integer value.");
            }

            argumentIndex = Array.FindIndex(args, (x => (x.ToLower() == "-offsety")));
            if (argumentIndex > -1)
            {
                argumentIndex++;
                if ((argumentIndex >= args.Length)
                        || !int.TryParse(args[argumentIndex], out requiredOffsetY))
                    Console.WriteLine("Please provide a offsetY integer value.");
            }

            #endregion

        }

        #endregion
        #region Initialize

        public override void Initialize()
        {
            AllPlayers = new int[Main.maxPlayers];
            for (int i = 0; i < Main.maxPlayers; i++)
                AllPlayers[i] = i;

            ServerApi.Hooks.NetSendData.Register(this, OnSendData, 1000000);
            ServerApi.Hooks.GamePostInitialize.Register(this, OnGamePostInitialize, int.MaxValue);
        }

        #endregion
        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ServerApi.Hooks.NetSendData.Deregister(this, OnSendData);
            base.Dispose(disposing);
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
        #region OnGamePostInitialize

        private void OnGamePostInitialize(EventArgs args)
        {
            FakeTileProvider provider = new FakeTileProvider(Main.maxTilesX, Main.maxTilesY);
            if (Netplay.IsServerRunning && (Main.tile != null))
            {
                int x = 0, y = 0, w = provider.Width, h = provider.Height;
                try
                {
                    provider[0, 0].ClearTile();

                    for (x = 0; x < w; x++)
                        for (y = 0; y < h; y++)
                            provider[x, y] = Main.tile[x, y];
                }
                catch (Exception ex)
                {
                    ServerApi.LogWriter.PluginWriteLine(this,
                        $"Error @{x}x{y}\n{ex}", TraceLevel.Error);
                    Environment.Exit(0);
                }
            }

            IDisposable previous = Main.tile as IDisposable;
            Main.tile = provider;
            if (previous != null)
                previous.Dispose();
            GC.Collect();
        }

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
            return tiles;
        }

        #endregion
    }
}