﻿#region Using
using OTAPI;
using OTAPI.Tile;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
        public override string Author => "ASgo and Anzhelika";
        public override string Description => "TODO";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        public const string WorldProviderName = "__world__";
        public static TileProviderCollection Tile { get; private set; }
        public static TileProviderCollection2 Tile2 { get; private set; }
        public static INamedTileCollection Void { get; private set; }
        public static IProviderTile VoidTile { get; private set; }
        public static INamedTileCollection World { get; private set; }
        internal static ModuleBuilder ModuleBuilder { get; private set; }
        private static int TypeCounter = 0;
        internal static int[] AllPlayers;
        internal static Func<RemoteClient, byte[], int, int, bool> NetSendBytes;

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

            // WARNING: stack overflow exception sometimes
            ReadonlyWorld = args.Any(x => (x.ToLower() == "-readonlyworld"));

            AssemblyName assemblyName = new AssemblyName("FakeProviderRuntimeAssembly");
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain
                .DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder = assemblyBuilder.DefineDynamicModule("FakeProviderRuntimeModule");
        }

        #endregion
        #region Initialize

        public override void Initialize()
        {
            NetSendBytes = (Func<RemoteClient, byte[], int, int, bool>)Delegate.CreateDelegate(
                typeof(Func<RemoteClient, byte[], int, int, bool>),
                ServerApi.Hooks,
                ServerApi.Hooks.GetType().GetMethod("InvokeNetSendBytes", BindingFlags.NonPublic | BindingFlags.Instance));

            AllPlayers = new int[Main.maxPlayers];
            for (int i = 0; i < Main.maxPlayers; i++)
                AllPlayers[i] = i;

            ServerApi.Hooks.NetSendData.Register(this, OnSendData, Int32.MaxValue);
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
            Tile2 = new TileProviderCollection2(VisibleWidth, VisibleHeight,
                OffsetX, OffsetY);

            ITile[,] voidTiles = new ITile[1, 1];
            voidTiles[0, 0] = new Tile();
            //Void = CreateReadonlyTileProvider("FakeProviderVoid", 0, 0, 1, 1, voidTiles, Int32.MinValue);
            Void = CreateTileProvider2("FakeProviderVoid", 0, 0, 1, 1, voidTiles, Int32.MinValue);
            VoidTile = Void[0, 0];

            //if (ReadonlyWorld)
                //World = CreateReadonlyTileProvider(WorldProviderName, 0, 0,
                    //Main.maxTilesX, Main.maxTilesY, Main.tile, Int32.MinValue + 1);
            //else
                World = CreateTileProvider2(WorldProviderName, 0, 0,
                    Main.maxTilesX, Main.maxTilesY, Main.tile, Int32.MinValue + 1);

            using (IDisposable previous = Main.tile as IDisposable)
            {
                Main.maxTilesX = VisibleWidth;
                Main.maxTilesY = VisibleHeight;
                Main.worldSurface += OffsetY;
                Main.rockLayer += OffsetY;
                Main.tile = Tile2;
            }

            WorldGen.setWorldSize();
            GC.Collect();

            // What's with loaded signs, chests and entities? Add to World TileProvider? Nah...
            World.ScanEntities();
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
            Tile.HideEntities();
            Tile2.HideEntities();
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
            Main.tile = Tile2;
            Tile.UpdateEntities();
            Tile2.UpdateEntities();
        }

        #endregion

        #region CreateTileProvider

        public static INamedTileCollection CreateTileProvider(string Name, int X, int Y, int Width, int Height, int Layer = 0)
        {
            Type newType = CreateType();
            Type tileProviderType = typeof(TileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(tileProviderType, true);
            result.Initialize(Tile, Name, X, Y, Width, Height, Layer);
            typeof(Tile<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            Tile.Add(result);
            return result;
        }

        public static INamedTileCollection CreateTileProvider(string Name, int X, int Y, int Width, int Height, ITileCollection CopyFrom, int Layer = 0)
        {
            Type newType = CreateType();
            Type tileProviderType = typeof(TileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(tileProviderType, true);
            result.Initialize(Tile, Name, X, Y, Width, Height, CopyFrom, Layer);
            typeof(Tile<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            Tile.Add(result);
            return result;
        }

        public static INamedTileCollection CreateTileProvider(string Name, int X, int Y, int Width, int Height, ITile[,] CopyFrom, int Layer = 0)
        {
            Type newType = CreateType();
            Type tileProviderType = typeof(TileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(tileProviderType, true);
            result.Initialize(Tile, Name, X, Y, Width, Height, CopyFrom, Layer);
            typeof(Tile<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            Tile.Add(result);
            return result;
        }

        #endregion
        #region CreateTileProvider

        public static INamedTileCollection CreateTileProvider2(string Name, int X, int Y, int Width, int Height, int Layer = 0)
        {
            Type newType = CreateType();
            Type tileProviderType = typeof(TileProvider2<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(tileProviderType, true);
            result.Initialize(Tile2, Name, X, Y, Width, Height, Layer);
            typeof(TileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            Tile2.Add(result);
            return result;
        }

        public static INamedTileCollection CreateTileProvider2(string Name, int X, int Y, int Width, int Height, ITileCollection CopyFrom, int Layer = 0)
        {
            Type newType = CreateType();
            Type tileProviderType = typeof(TileProvider2<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(tileProviderType, true);
            result.Initialize(Tile2, Name, X, Y, Width, Height, CopyFrom, Layer);
            typeof(TileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            Tile2.Add(result);
            return result;
        }

        public static INamedTileCollection CreateTileProvider2(string Name, int X, int Y, int Width, int Height, ITile[,] CopyFrom, int Layer = 0)
        {
            Type newType = CreateType();
            Type tileProviderType = typeof(TileProvider2<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(tileProviderType, true);
            result.Initialize(Tile2, Name, X, Y, Width, Height, CopyFrom, Layer);
            typeof(TileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            Tile2.Add(result);
            return result;
        }

        #endregion
        #region CreateReadonlyTileProvider

        public static INamedTileCollection CreateReadonlyTileProvider(string Name, int X, int Y, int Width, int Height, int Layer = 0)
        {
            Type newType = CreateType();
            Type tileProviderType = typeof(ReadonlyTileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(tileProviderType, true);
            result.Initialize(Tile, Name, X, Y, Width, Height, Layer);
            typeof(ReadonlyTile<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            Tile.Add(result);
            return result;
        }

        public static INamedTileCollection CreateReadonlyTileProvider(string Name, int X, int Y, int Width, int Height, ITileCollection CopyFrom, int Layer = 0)
        {
            Type newType = CreateType();
            Type tileProviderType = typeof(ReadonlyTileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(tileProviderType, true);
            result.Initialize(Tile, Name, X, Y, Width, Height, CopyFrom, Layer);
            typeof(ReadonlyTile<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            Tile.Add(result);
            return result;
        }

        public static INamedTileCollection CreateReadonlyTileProvider(string Name, int X, int Y, int Width, int Height, ITile[,] CopyFrom, int Layer = 0)
        {
            Type newType = CreateType();
            Type tileProviderType = typeof(ReadonlyTileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(tileProviderType, true);
            result.Initialize(Tile, Name, X, Y, Width, Height, CopyFrom, Layer);
            typeof(ReadonlyTile<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            Tile.Add(result);
            return result;
        }

        #endregion

        #region CreateType

        public static Type CreateType() =>
            ModuleBuilder.DefineType($"FakeType{TypeCounter++}",
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.AutoLayout).CreateType();

        #endregion
    }
}
