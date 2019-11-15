#region Using
using OTAPI;
using OTAPI.Tile;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Terraria;
using TerrariaApi.Server;
#endregion
namespace FakeProvider
{
    [ApiVersion(2, 1)]
    public class FakeProvider : TerrariaPlugin
    {
        #region Data

        public override string Name => "FakeProvider";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public override string Author => "Anzhelika & ASgo";
        public override string Description => "TODO";

        public static TileProviderCollection Tile { get; private set; }
        public static INamedTileCollection World { get; private set; }
        internal static TypeBuilder TypeBuilder { get; private set; }
        internal static int[] AllPlayers;

        public static int OffsetX { get; private set; }
        public static int OffsetY { get; private set; }
        public static int VisibleWidth { get; private set; }
        public static int VisibleHeight { get; private set; }
        public static bool ReadonlyWorld { get; private set; }

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

#warning TODO: rockLevel, surfaceLevel, cavernLevel or whatever

            ReadonlyWorld = args.Any(x => (x.ToLower() == "-readonlyworld"));

            AssemblyName assemblyName = new AssemblyName("TileProvider");
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            TypeBuilder = moduleBuilder.DefineType(assemblyName.FullName
                                , TypeAttributes.Public |
                                TypeAttributes.Class |
                                TypeAttributes.AutoClass |
                                TypeAttributes.AnsiClass |
                                TypeAttributes.BeforeFieldInit |
                                TypeAttributes.AutoLayout);
        }

        #endregion
        #region Initialize

        public override void Initialize()
        {
            AllPlayers = new int[Main.maxPlayers];
            for (int i = 0; i < Main.maxPlayers; i++)
                AllPlayers[i] = i;

            ServerApi.Hooks.NetSendData.Register(this, OnSendData, 1000000);
            OTAPI.Hooks.World.IO.PostLoadWorld += OnPostLoadWorld;
            OTAPI.Hooks.World.IO.PreSaveWorld += OnPreSaveWorld;
            OTAPI.Hooks.World.IO.PostSaveWorld += OnPostSaveWorld;
        }

        #endregion
        #region Dispose

        protected override void Dispose(bool Disposing)
        {
            if (Disposing)
            {
                ServerApi.Hooks.NetSendData.Deregister(this, OnSendData);
                OTAPI.Hooks.World.IO.PostLoadWorld += OnPostLoadWorld;
                OTAPI.Hooks.World.IO.PreSaveWorld += OnPreSaveWorld;
                OTAPI.Hooks.World.IO.PostSaveWorld += OnPostSaveWorld;
            }
            base.Dispose(Disposing);
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
        #region OnPostLoadWorld

        private void OnPostLoadWorld(bool FromCloud)
        {
            Console.WriteLine($"REAL maxTilesX, maxTilesY: {Main.maxTilesX}, {Main.maxTilesY}");

            if (VisibleWidth < 0)
                VisibleWidth = (OffsetX + Main.maxTilesX);
            if (VisibleHeight < 0)
                VisibleHeight = (OffsetY + Main.maxTilesY);
            Tile = new TileProviderCollection(VisibleWidth, VisibleHeight,
                OffsetX, OffsetY);

            if (ReadonlyWorld)
                World = CreateReadonlyTileProvider("__world__", 0, 0,
                    Main.maxTilesX, Main.maxTilesY, Main.tile);
            else
                World = CreateTileProvider("__world__", 0, 0,
                    Main.maxTilesX, Main.maxTilesY, Main.tile);
            Tile.Add(World);

            using (IDisposable previous = Main.tile as IDisposable)
            {
                Main.maxTilesX = VisibleWidth;
                Main.maxTilesY = VisibleHeight;
                Main.worldSurface += OffsetY;
                Main.rockLayer += OffsetY;
                Main.tile = Tile;
            }
            GC.Collect();

            WorldGen.setWorldSize();
            Console.WriteLine($"NEW maxTilesX, maxTilesY: {Main.maxTilesX}, {Main.maxTilesY}");
            Console.ReadKey();
        }

        #endregion
        #region OnPreSaveWorld

        private HookResult OnPreSaveWorld(ref bool Cloud, ref bool ResetTime)
        {
            Main.maxTilesX = World.Width;
            Main.maxTilesY = World.Height;
            Main.worldSurface -= OffsetY;
            Main.rockLayer -= OffsetY;
            Main.tile = World;
            return HookResult.Continue;
        }

        #endregion
        #region OnPostSaveWorld

        private void OnPostSaveWorld(bool Cloud, bool ResetTime)
        {
            Main.maxTilesX = VisibleWidth;
            Main.maxTilesY = VisibleHeight;
            Main.worldSurface += OffsetY;
            Main.rockLayer += OffsetY;
            Main.tile = Tile;
        }

        #endregion

        #region CreateTileProvider

        public static INamedTileCollection CreateTileProvider(string Name, int X, int Y, int Width, int Height, int Layer = 0)
        {
            Type newType = TypeBuilder.CreateType();
            Type tileProviderType = typeof(TileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(
                tileProviderType, Name, X, Y, Width, Height, Layer);
            typeof(TileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            return result;
        }

        public static INamedTileCollection CreateTileProvider(string Name, int X, int Y, int Width, int Height, ITileCollection CopyFrom, int Layer = 0)
        {
            Type newType = TypeBuilder.CreateType();
            Type tileProviderType = typeof(TileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(
                tileProviderType, Name, X, Y, Width, Height, CopyFrom, Layer);
            typeof(TileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            return result;
        }

        public static INamedTileCollection CreateTileProvider(string Name, int X, int Y, int Width, int Height, ITile[,] CopyFrom, int Layer = 0)
        {
            Type newType = TypeBuilder.CreateType();
            Type tileProviderType = typeof(TileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(
                tileProviderType, Name, X, Y, Width, Height, CopyFrom, Layer);
            typeof(TileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            return result;
        }

        #endregion
        #region CreateReadonlyTileProvider

        public static INamedTileCollection CreateReadonlyTileProvider(string Name, int X, int Y, int Width, int Height, int Layer = 0)
        {
            Type newType = TypeBuilder.CreateType();
            Type tileProviderType = typeof(ReadonlyTileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(
                tileProviderType, Name, X, Y, Width, Height, Layer);
            typeof(ReadonlyTileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            return result;
        }

        public static INamedTileCollection CreateReadonlyTileProvider(string Name, int X, int Y, int Width, int Height, ITileCollection CopyFrom, int Layer = 0)
        {
            Type newType = TypeBuilder.CreateType();
            Type tileProviderType = typeof(ReadonlyTileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(
                tileProviderType, Name, X, Y, Width, Height, CopyFrom, Layer);
            typeof(ReadonlyTileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            return result;
        }

        public static INamedTileCollection CreateReadonlyTileProvider(string Name, int X, int Y, int Width, int Height, ITile[,] CopyFrom, int Layer = 0)
        {
            Type newType = TypeBuilder.CreateType();
            Type tileProviderType = typeof(ReadonlyTileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(
                tileProviderType, Name, X, Y, Width, Height, CopyFrom, Layer);
            typeof(ReadonlyTileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            return result;
        }

        #endregion
    }
}