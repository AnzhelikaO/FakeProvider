#region Using
using OTAPI.Tile;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public int OffsetX { get; private set; }
        public int OffsetY { get; private set; }
        public int VisibleWidth { get; private set; }
        public int VisibleHeight { get; private set; }
        public bool ReadonlyWorld { get; private set; }

        #endregion

        #region Constructor

        public FakeProvider(Main game) : base(game)
        {
            Order = -1002;
            string[] args = Environment.GetCommandLineArgs();
            #region Offset

            int offsetX = 0;
            int argumentIndex = Array.FindIndex(args, (x => (x.ToLower() == "-offsetx")));
            if (argumentIndex > -1)
            {
                argumentIndex++;
                if ((argumentIndex >= args.Length)
                        || !int.TryParse(args[argumentIndex], out offsetX))
                    Console.WriteLine("Please provide a not negative offsetX integer value.");
            }
            OffsetX = offsetX;

            int offsetY = 0;
            argumentIndex = Array.FindIndex(args, (x => (x.ToLower() == "-offsety")));
            if (argumentIndex > -1)
            {
                argumentIndex++;
                if ((argumentIndex >= args.Length)
                        || !int.TryParse(args[argumentIndex], out offsetY))
                    Console.WriteLine("Please provide a not negative offsetY integer value.");
            }
            OffsetY = offsetY;

            #endregion
            #region Width, Height

            int visibleWidth = -1;
            argumentIndex = Array.FindIndex(args, (x => (x.ToLower() == "-visiblewidth")));
            if (argumentIndex > -1)
            {
                argumentIndex++;
                if ((argumentIndex >= args.Length)
                        || !int.TryParse(args[argumentIndex], out visibleWidth))
                {
                    Console.WriteLine("Please provide a not negative visibleWidth integer value.");
                    visibleWidth = -1;
                }
            }
            VisibleWidth = visibleWidth;

            int visibleHeight = -1;
            argumentIndex = Array.FindIndex(args, (x => (x.ToLower() == "-visibleheight")));
            if (argumentIndex > -1)
            {
                argumentIndex++;
                if ((argumentIndex >= args.Length)
                        || !int.TryParse(args[argumentIndex], out visibleHeight))
                {
                    Console.WriteLine("Please provide a not negative visibleHeight integer value.");
                    visibleHeight = -1;
                }
            }
            VisibleHeight = visibleHeight;

            #endregion

            ReadonlyWorld = args.Any(x => (x.ToLower() == "-readonlyworld"));
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
                    // We allow sending packet to custom list of players by specifying it in text parameter
                    if (args.text?._text == null)
                        SendSectionPacket.Send(args.remoteClient, args.ignoreClient,
                            args.number, (int)args.number2, (short)args.number3, (short)args.number4);
                    else
                        SendSectionPacket.Send(args.text._text.Select(c => (int)c), args.ignoreClient,
                            args.number, (int)args.number2, (short)args.number3, (short)args.number4);
                    break;
                case PacketTypes.TileFrameSection:
#warning NotImplemented
                    // TODO: sending to custom list of players with args.text
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
            if (VisibleWidth < 0)
                VisibleWidth = (OffsetX + Main.maxTilesX);
            if (VisibleHeight < 0)
                VisibleHeight = (OffsetY + Main.maxTilesY);
            TileProviderCollection provider = new TileProviderCollection(VisibleWidth, VisibleHeight,
                OffsetX, OffsetY);

            if (Netplay.IsServerRunning && (Main.tile != null))
            {
                INamedTileCollection world;
                if (ReadonlyWorld)
                    world = new ReadonlyTileProvider();
                else
                    world = new TileProvider("__world__", OffsetX, OffsetY,
                        Main.maxTilesX, Main.maxTilesY, Main.tile);
                provider.Add(world);
            }

            IDisposable previous = Main.tile as IDisposable;
            Main.tile = provider;
            Main.maxTilesX = VisibleWidth;
            Main.maxTilesY = VisibleHeight;
            previous?.Dispose();
            GC.Collect();
        }

        #endregion
    }
}