#region Using
using OTAPI;
using OTAPI.Tile;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Terraria;
using Terraria.IO;
using Terraria.Social;
using Terraria.Utilities;
using TerrariaApi.Server;
using OTAPI.Callbacks.Terraria;
using System.Collections.Generic;
using TShockAPI;
#endregion
namespace FakeProvider
{
    [ApiVersion(2, 1)]
    public class FakeProviderPlugin : TerrariaPlugin
    {
        #region Data

        public override string Name => "FakeProvider";
        public override string Author => "ASgo and Anzhelika";
        public override string Description => "TODO";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        internal static int[] AllPlayers;
        internal static Func<RemoteClient, byte[], int, int, bool> NetSendBytes;

        public static int OffsetX { get; private set; }
        public static int OffsetY { get; private set; }
        public static int VisibleWidth { get; private set; }
        public static int VisibleHeight { get; private set; }
        public static bool ReadonlyWorld { get; private set; }

        internal static List<INamedTileCollection> ProvidersToAdd = new List<INamedTileCollection>();
        internal static bool ProvidersLoaded = false;
        public static Command[] CommandList = new Command[]
        {
            new Command("FakeProvider.Control", FakeCommand, "fake")
        };

        #endregion

        #region Constructor

        public FakeProviderPlugin(Main game) : base(game)
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
            #region VisibleWidth, VisibleHeight

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
            Helper.ModuleBuilder = assemblyBuilder.DefineDynamicModule("FakeProviderRuntimeModule");
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

            Hooks.World.IO.PreLoadWorld += OnPreLoadWorld;
            Hooks.World.IO.PostLoadWorld += OnPostLoadWorld;
            Hooks.World.IO.PreSaveWorld += OnPreSaveWorld;
            Hooks.World.IO.PostSaveWorld += OnPostSaveWorld;
            ServerApi.Hooks.NetSendData.Register(this, OnSendData, Int32.MaxValue);

            Commands.ChatCommands.AddRange(CommandList);
        }

        #endregion
        #region Dispose

        protected override void Dispose(bool Disposing)
        {
            if (Disposing)
            {
                Hooks.World.IO.PreLoadWorld -= OnPreLoadWorld;
                Hooks.World.IO.PostLoadWorld -= OnPostLoadWorld;
                Hooks.World.IO.PreSaveWorld -= OnPreSaveWorld;
                Hooks.World.IO.PostSaveWorld -= OnPostSaveWorld;
                ServerApi.Hooks.NetSendData.Deregister(this, OnSendData);
            }
            base.Dispose(Disposing);
        }

        #endregion

        #region OnPreLoadWorld

        private static HookResult OnPreLoadWorld(ref bool loadFromCloud)
        {
            LoadWorldDirect(loadFromCloud);
            Hooks.World.IO.PostLoadWorld?.Invoke(loadFromCloud);
            return HookResult.Cancel;
        }

        #endregion
        #region OnPostLoadWorld

        private static void OnPostLoadWorld(bool FromCloud)
        {
            FakeProviderAPI.Tile.OffsetX = OffsetX;
            FakeProviderAPI.Tile.OffsetY = OffsetY;

            Main.maxTilesX = VisibleWidth;
            Main.maxTilesY = VisibleHeight;
            Main.worldSurface += OffsetY;
            Main.rockLayer += OffsetY;
            Main.spawnTileX += OffsetX;
            Main.spawnTileY += OffsetY;
            WorldGen.setWorldSize();

            lock (ProvidersToAdd)
            {
                ProvidersLoaded = true;

                FakeProviderAPI.Tile.Add(FakeProviderAPI.Tile.Void);
                ProvidersToAdd.Remove(FakeProviderAPI.Tile.Void);

                FakeProviderAPI.Tile.Add(FakeProviderAPI.World);
                ProvidersToAdd.Remove(FakeProviderAPI.World);
                FakeProviderAPI.World.ScanEntities();

                foreach (INamedTileCollection provider in ProvidersToAdd)
                    FakeProviderAPI.Tile.Add(provider);
                ProvidersToAdd.Clear();
            }

            Main.tile = FakeProviderAPI.Tile;

            GC.Collect();
        }

        #endregion
        #region OnPreSaveWorld

        private static HookResult OnPreSaveWorld(ref bool Cloud, ref bool ResetTime)
        {
            Main.maxTilesX = FakeProviderAPI.World.Width;
            Main.maxTilesY = FakeProviderAPI.World.Height;
            Main.worldSurface -= OffsetY;
            Main.rockLayer -= OffsetY;
            Main.tile = FakeProviderAPI.World;
            FakeProviderAPI.Tile.HideEntities();
            return HookResult.Continue;
        }

        #endregion
        #region OnPostSaveWorld

        private static void OnPostSaveWorld(bool Cloud, bool ResetTime)
        {
            Main.maxTilesX = VisibleWidth;
            Main.maxTilesY = VisibleHeight;
            Main.worldSurface += OffsetY;
            Main.rockLayer += OffsetY;
            Main.tile = FakeProviderAPI.Tile;
            FakeProviderAPI.Tile.UpdateEntities();
        }

        #endregion
        #region OnSendData

        private static void OnSendData(SendDataEventArgs args)
        {
            if (args.Handled)
                return;

            switch (args.MsgId)
            {
                case PacketTypes.TileSendSection:
                    args.Handled = true;
                    // We allow sending packet to custom list of players by specifying it in text parameter
                    if (args.text?._text?.Length > 0)
                        SendSectionPacket.Send(args.text._text.Select(c => (int)c), args.ignoreClient,
                            args.number, (int)args.number2, (short)args.number3, (short)args.number4);
                    else
                        SendSectionPacket.Send(args.remoteClient, args.ignoreClient,
                            args.number, (int)args.number2, (short)args.number3, (short)args.number4);
                    break;
                case PacketTypes.TileFrameSection:
#warning NotImplemented
                    // TODO: sending to custom list of players with args.text
                    break;
                case PacketTypes.TileSendSquare:
                    args.Handled = true;
                    if (args.text?._text?.Length > 0)
                        SendTileSquarePacket.Send(args.text._text.Select(c => (int)c), args.ignoreClient,
                            (int)args.number3, (int)args.number4, (int)args.number, (int)args.number2, args.number5);
                    else
                        SendTileSquarePacket.Send(args.remoteClient, args.ignoreClient,
                            (int)args.number3, (int)args.number4, (int)args.number, (int)args.number2, args.number5);
                    break;
            }
        }

        #endregion

        #region FindProvider

        public static bool FindProvider(string name, TSPlayer player, out INamedTileCollection found)
        {
            found = null;
            List<INamedTileCollection> foundProviders = new List<INamedTileCollection>();
            string lowerName = name.ToLower();
            foreach (INamedTileCollection provider in FakeProviderAPI.Tile.Providers)
            {
                if (provider == null)
                    continue;
                if (provider.Name == name)
                {
                    found = provider;
                    return true;
                }
                else if (provider.Name.ToLower().StartsWith(lowerName))
                    foundProviders.Add(provider);
            }
            if (foundProviders.Count == 0)
            {
                player?.SendErrorMessage($"Invalid provider '{name}'.");
                return false;
            }
            else if (foundProviders.Count > 1)
            {
                if (player != null)
                    player.SendMultipleMatchError(foundProviders);
                return false;
            }
            else
            {
                found = foundProviders[0];
                return true;
            }
        }

        #endregion
        #region FakeCommand

        public static void FakeCommand(CommandArgs args)
        {
            string arg0 = args.Parameters.ElementAtOrDefault(0);
            switch (arg0?.ToLower())
            {
                case "l":
                case "list":
                {
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int page))
                        return;
                    List<string> lines = PaginationTools.BuildLinesFromTerms(FakeProviderAPI.Tile.Providers);
                    PaginationTools.SendPage(args.Player, page, lines, new PaginationTools.Settings()
                    {
                        HeaderFormat = "Fake providers ({0}/{1}):",
                        FooterFormat = "Type '/fake list {0}' for more.",
                        NothingToDisplayString = "There are no fake providers yet."
                    });
                    break;
                }
                case "tp":
                case "teleport":
                {
                    if (args.Parameters.Count != 2)
                    {
                        args.Player.SendErrorMessage("/fake tp \"provider name\"");
                        return;
                    }
                    if (!FindProvider(args.Parameters[1], args.Player, out INamedTileCollection provider))
                        return;

                    args.Player.Teleport((provider.X + provider.Width / 2) * 16,
                        (provider.Y + provider.Height / 2) * 16);
                    args.Player.SendSuccessMessage($"Teleported to fake provider '{provider.Name}'.");
                    break;
                }
                case "m":
                case "move":
                {
                    if (args.Parameters.Count != 4)
                    {
                        args.Player.SendErrorMessage("/fake move \"provider name\" <relative x> <relative y>");
                        return;
                    }
                    if (!FindProvider(args.Parameters[1], args.Player, out INamedTileCollection provider))
                        return;

                    if (!Int32.TryParse(args.Parameters[2], out int x)
                        || !Int32.TryParse(args.Parameters[3], out int y))
                    {
                        args.Player.SendErrorMessage("Invalid coordinates.");
                        return;
                    }

                    provider.Move(x, y, true);
                    args.Player.SendSuccessMessage($"Fake provider '{provider.Name}' moved to ({x}, {y}).");
                    break;
                }
                case "la":
                case "layer":
                {
                    if (args.Parameters.Count != 3)
                    {
                        args.Player.SendErrorMessage("/fake layer \"provider name\" <layer>");
                        return;
                    }
                    if (!FindProvider(args.Parameters[1], args.Player, out INamedTileCollection provider))
                        return;

                    if (!Int32.TryParse(args.Parameters[2], out int layer))
                    {
                        args.Player.SendErrorMessage("Invalid layer.");
                        return;
                    }

                    provider.SetLayer(layer, true);
                    args.Player.SendSuccessMessage($"Fake provider '{provider.Name}' layer set to {layer}.");
                    break;
                }
                case "i":
                case "info":
                {
                    if (args.Parameters.Count != 2)
                    {
                        args.Player.SendErrorMessage("/fake info \"provider name\"");
                        return;
                    }
                    if (!FindProvider(args.Parameters[1], args.Player, out INamedTileCollection provider))
                        return;

                    args.Player.SendInfoMessage(
$@"Fake provider '{provider.Name}' ({provider.GetType().Name})
Position and size: {provider.XYWH()}
Enabled: {provider.Enabled}
Entities: {provider.Entities.Count}");
                    break;
                }
                default:
                {
                    args.Player.SendSuccessMessage("/fake subcommands:");
                    args.Player.SendInfoMessage(
@"/fake info ""provider name""
/fake tp ""provider name""
/fake move ""provider name"" <relative x> <relative y>
/fake layer ""provider name"" <layer>
/fake list [page]");
                    break;
                }
            }
        }

        #endregion

        #region CreateCustomTileProvider

        private static void CreateCustomTileProvider()
        {
            int maxTilesX = Main.maxTilesX;
            int maxTilesY = Main.maxTilesY;
            if (VisibleWidth < 0)
                VisibleWidth = (OffsetX + maxTilesX);
            else
                VisibleWidth++;
            if (VisibleHeight < 0)
                VisibleHeight = (OffsetY + maxTilesY);
            else
                VisibleHeight++;
            FakeProviderAPI.Tile = new TileProviderCollection();
            FakeProviderAPI.Tile.Initialize(VisibleWidth, VisibleHeight, 0, 0);

            if (ReadonlyWorld)
                FakeProviderAPI.World = FakeProviderAPI.CreateReadonlyTileProvider(FakeProviderAPI.WorldProviderName, 0, 0,
                    maxTilesX, maxTilesY, Int32.MinValue + 1);
            else
                FakeProviderAPI.World = FakeProviderAPI.CreateTileProvider(FakeProviderAPI.WorldProviderName, 0, 0,
                    maxTilesX, maxTilesY, Int32.MinValue + 1);

            using (IDisposable previous = Main.tile as IDisposable)
                Main.tile = FakeProviderAPI.World;
        }

        #endregion
        #region LoadWorldDirect

        private static void LoadWorldDirect(bool loadFromCloud)
        {
            Main.lockMenuBGChange = true;
            WorldFile._isWorldOnCloud = loadFromCloud;
            Main.checkXMas();
            Main.checkHalloween();
            bool flag = loadFromCloud && SocialAPI.Cloud != null;
            if (!FileUtilities.Exists(Main.worldPathName, flag) && Main.autoGen)
            {
                if (!flag)
                {
                    for (int i = Main.worldPathName.Length - 1; i >= 0; i--)
                    {
                        if (Main.worldPathName.Substring(i, 1) == (Path.DirectorySeparatorChar.ToString() ?? ""))
                        {
                            Terraria.Utils.TryCreatingDirectory(Main.worldPathName.Substring(0, i));
                            break;
                        }
                    }
                }
                WorldGen.clearWorld();
                Main.ActiveWorldFileData = WorldFile.CreateMetadata((Main.worldName == "") ? "World" : Main.worldName, flag, Main.GameMode);
                string text = (Main.AutogenSeedName ?? "").Trim();
                if (text.Length == 0)
                {
                    Main.ActiveWorldFileData.SetSeedToRandom();
                }
                else
                {
                    Main.ActiveWorldFileData.SetSeed(text);
                }
                WorldGen.GenerateWorld(Main.ActiveWorldFileData.Seed, Main.AutogenProgress);
                WorldFile.SaveWorld();
            }
            using (MemoryStream memoryStream = new MemoryStream(FileUtilities.ReadAllBytes(Main.worldPathName, flag)))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream))
                {
                    try
                    {
                        WorldGen.loadFailed = false;
                        WorldGen.loadSuccess = false;
                        int num = WorldFile._versionNumber = binaryReader.ReadInt32();
                        int num2;
                        if (num <= 87)
                        {
                            num2 = WorldFile.LoadWorld_Version1_Old_BeforeRelease88(binaryReader);
                        }
                        else
                        {
                            num2 = LoadWorld_Version2(binaryReader);
                        }
                        if (num < 141)
                        {
                            if (!loadFromCloud)
                            {
                                Main.ActiveWorldFileData.CreationTime = File.GetCreationTime(Main.worldPathName);
                            }
                            else
                            {
                                Main.ActiveWorldFileData.CreationTime = DateTime.Now;
                            }
                        }
                        WorldFile.CheckSavedOreTiers();
                        binaryReader.Close();
                        memoryStream.Close();
                        if (num2 != 0)
                        {
                            WorldGen.loadFailed = true;
                        }
                        else
                        {
                            WorldGen.loadSuccess = true;
                        }
                        if (WorldGen.loadFailed || !WorldGen.loadSuccess)
                        {
                            return;
                        }
                        WorldFile.ConvertOldTileEntities();
                        WorldFile.ClearTempTiles();
                        WorldGen.gen = true;
                        WorldGen.waterLine = Main.maxTilesY;
                        Liquid.QuickWater(2, -1, -1);
                        WorldGen.WaterCheck();
                        int num3 = 0;
                        Liquid.quickSettle = true;
                        int num4 = Liquid.numLiquid + LiquidBuffer.numLiquidBuffer;
                        float num5 = 0f;
                        while (Liquid.numLiquid > 0 && num3 < 100000)
                        {
                            num3++;
                            float num6 = (float)(num4 - (Liquid.numLiquid + LiquidBuffer.numLiquidBuffer)) / (float)num4;
                            if (Liquid.numLiquid + LiquidBuffer.numLiquidBuffer > num4)
                            {
                                num4 = Liquid.numLiquid + LiquidBuffer.numLiquidBuffer;
                            }
                            if (num6 > num5)
                            {
                                num5 = num6;
                            }
                            else
                            {
                                num6 = num5;
                            }
                            SetStatusText(string.Concat(new object[]
                            {
                                Lang.gen[27].Value,
                                " ",
                                (int)(num6 * 100f / 2f + 50f),
                                "%"
                            }));
                            Liquid.UpdateLiquid();
                        }
                        Liquid.quickSettle = false;
                        Main.weatherCounter = WorldGen.genRand.Next(3600, 18000);
                        Cloud.resetClouds();
                        WorldGen.WaterCheck();
                        WorldGen.gen = false;
                        NPC.setFireFlyChance();
                        if (Main.slimeRainTime > 0.0)
                        {
                            Main.StartSlimeRain(false);
                        }
                        NPC.SetWorldSpecificMonstersByWorldID();
                    }
                    catch (Exception value)
                    {
                        WorldGen.loadFailed = true;
                        WorldGen.loadSuccess = false;
                        System.Console.WriteLine(value);
                        try
                        {
                            binaryReader.Close();
                            memoryStream.Close();
                        }
                        catch
                        {
                        }
                        return;
                    }
                }
            }

            EventInfo eventOnWorldLoad = typeof(WorldFile).GetEvent("OnWorldLoad", BindingFlags.Public | BindingFlags.Static);
                eventOnWorldLoad.GetRaiseMethod()?.Invoke(null, new object[] { });
            //if (WorldFile.OnWorldLoad != null)
                //WorldFile.OnWorldLoad();
        }

        #endregion
        #region SetStatusText

        private static void SetStatusText(string text)
        {
            Hooks.Game.StatusTextHandler statusTextWrite = Hooks.Game.StatusTextWrite;
            HookResult? hookResult = (statusTextWrite != null) ? new HookResult?(statusTextWrite(ref text)) : null;
            bool flag = hookResult != null && hookResult.Value == HookResult.Cancel;
            if (!flag)
            {
                typeof(Main).GetField("statusText", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, text);
            }
        }

        #endregion
        #region LoadWorld_Version2

        private static int LoadWorld_Version2(BinaryReader reader)
        {
            reader.BaseStream.Position = 0L;
            bool[] importance;
            int[] array;
            if (!WorldFile.LoadFileFormatHeader(reader, out importance, out array))
            {
                return 5;
            }
            if (reader.BaseStream.Position != (long)array[0])
            {
                return 5;
            }
            WorldFile.LoadHeader(reader);
            if (reader.BaseStream.Position != (long)array[1])
            {
                return 5;
            }

            // ======================
            CreateCustomTileProvider();
            // ======================

            WorldFile.LoadWorldTiles(reader, importance);
            if (reader.BaseStream.Position != (long)array[2])
            {
                return 5;
            }
            WorldFile.LoadChests(reader);
            if (reader.BaseStream.Position != (long)array[3])
            {
                return 5;
            }
            WorldFile.LoadSigns(reader);
            if (reader.BaseStream.Position != (long)array[4])
            {
                return 5;
            }
            WorldFile.LoadNPCs(reader);
            if (reader.BaseStream.Position != (long)array[5])
            {
                return 5;
            }
            if (WorldFile._versionNumber >= 116)
            {
                if (WorldFile._versionNumber < 122)
                {
                    WorldFile.LoadDummies(reader);
                    if (reader.BaseStream.Position != (long)array[6])
                    {
                        return 5;
                    }
                }
                else
                {
                    WorldFile.LoadTileEntities(reader);
                    if (reader.BaseStream.Position != (long)array[6])
                    {
                        return 5;
                    }
                }
            }
            if (WorldFile._versionNumber >= 170)
            {
                WorldFile.LoadWeightedPressurePlates(reader);
                if (reader.BaseStream.Position != (long)array[7])
                {
                    return 5;
                }
            }
            if (WorldFile._versionNumber >= 189)
            {
                WorldFile.LoadTownManager(reader);
                if (reader.BaseStream.Position != (long)array[8])
                {
                    return 5;
                }
            }
            if (WorldFile._versionNumber >= 210)
            {
                WorldFile.LoadBestiary(reader, WorldFile._versionNumber);
                if (reader.BaseStream.Position != (long)array[9])
                {
                    return 5;
                }
            }
            else
            {
                WorldFile.LoadBestiaryForVersionsBefore210();
            }
            if (WorldFile._versionNumber >= 220)
            {
                WorldFile.LoadCreativePowers(reader, WorldFile._versionNumber);
                if (reader.BaseStream.Position != (long)array[10])
                {
                    return 5;
                }
            }
            return WorldFile.LoadFooter(reader);
        }

        #endregion
    }
}
