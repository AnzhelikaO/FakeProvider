#region Using

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

using Terraria;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.Social;
using Terraria.Utilities;
using Terraria.WorldBuilding;

using TerrariaApi.Server;

using static Terraria.IO.WorldFile;

#endregion

namespace FakeProvider.FakeGen
{
    public static class FakeGenManager
    {
        #region Data

        public static LoadStage Stages = LoadStage.CheckXMas | LoadStage.CheckHalloween;

        #endregion

        #region CustomLoadWorld

        /// <summary>
        /// <see cref="Terraria.IO.WorldFile.LoadWorld(bool)"/>
        /// </summary>
        public static void CustomLoadWorld(bool loadFromCloud, TerrariaPlugin plugin = null)
        {
            Main.lockMenuBGChange = true;
            _isWorldOnCloud = loadFromCloud;
            if (Stages.HasFlag(LoadStage.CheckXMas))
                Main.checkXMas();
            if (Stages.HasFlag(LoadStage.CheckHalloween))
                Main.checkHalloween();

            bool needSteam = loadFromCloud && SocialAPI.Cloud != null;

            #region AutoGen
            if (!FileUtilities.Exists(Main.worldPathName, needSteam) && Main.autoGen)
            {
                if (!needSteam)
                {
                    for (int i = Main.worldPathName.Length - 1; i >= 0; i--)
                    {
                        if (Main.worldPathName.Substring(i, 1) == (Path.DirectorySeparatorChar.ToString() ?? ""))
                        {
                            Utils.TryCreatingDirectory(Main.worldPathName.Substring(0, i));
                            break;
                        }
                    }
                }

                WorldGen.clearWorld();

                Main.ActiveWorldFileData = CreateMetadata((Main.worldName == "") ? 
                    "World" : Main.worldName, needSteam, Main.GameMode);

                string seed = (Main.AutogenSeedName ?? "").Trim();
                if (seed.Length == 0)
                    Main.ActiveWorldFileData.SetSeedToRandom();
                else
                    Main.ActiveWorldFileData.SetSeed(seed);

                UIWorldCreation.ProcessSpecialWorldSeeds(seed);
                WorldGen.GenerateWorld(Main.ActiveWorldFileData.Seed, Main.AutogenProgress);

                SaveWorld();
            }

            #endregion

            try
            {
                using UnsafeReadOnlyMemoryStream stream
                    = new UnsafeReadOnlyMemoryStream(FileUtilities.ReadAllBytes(Main.worldPathName, needSteam));
                using UnsafeBinaryReader reader = new UnsafeBinaryReader(stream);

                try
                {
                    WorldGen.loadFailed = false;
                    WorldGen.loadSuccess = false;

                    #region Validating

                    _versionNumber = reader.ReadInt32();
                    if (_versionNumber <= 0 || _versionNumber > 279)
                    {
                        if (plugin != null)
                            ServerApi.LogWriter.PluginWriteLine(plugin, "The world version is irrelevant for this version of the plugin. Disable custom world loading.",
                                TraceLevel.Error);
                        else
                            Console.WriteLine("The world version is irrelevant for this version of the plugin. Disable custom world loading.");
                        WorldGen.loadFailed = true;
                        return;
                    }

                    #endregion

                    #region PreLoadWorld

                    On.Terraria.IO.WorldFile.LoadWorldTiles += OnLoadWorldTiles;
                    On.Terraria.WorldGen.clearWorld += OnClearWorld;

                    #endregion
                    #region LoadWorld

                    int result;
                    if (_versionNumber > 87)
                        result = LoadWorld_Version2(reader);
                    else
                        result = LoadWorld_Version1_Old_BeforeRelease88(reader);

                    #endregion
                    #region PostLoadWorld

                    On.Terraria.IO.WorldFile.LoadWorldTiles -= OnLoadWorldTiles;
                    On.Terraria.WorldGen.clearWorld -= OnClearWorld;

                    #endregion

                    #region CreationTime

                    if (_versionNumber < 141)
                    {
                        if (!loadFromCloud)
                            Main.ActiveWorldFileData.CreationTime = File.GetCreationTime(Main.worldPathName);
                        else
                            Main.ActiveWorldFileData.CreationTime = DateTime.Now;
                    }

                    #endregion

                    if (Stages.HasFlag(LoadStage.CheckSavedOreTiers))
                        CheckSavedOreTiers();

                    reader.Close();
                    stream.Close();

                    WorldGen.loadSuccess = !(WorldGen.loadFailed = result != 0);
                    if (WorldGen.loadFailed || !WorldGen.loadSuccess)
                        return;

                    if (Stages.HasFlag(LoadStage.ConvertOldTileEntities))
                        ConvertOldTileEntities();
                    if (Stages.HasFlag(LoadStage.ClearTempTiles))
                        ConvertOldTileEntities();

                    WorldGen.gen = true;
                    GenVars.waterLine = Main.maxTilesY;

                    if (Stages.HasFlag(LoadStage.QuickWater))
                        Liquid.QuickWater(2);
                    if (Stages.HasFlag(LoadStage.WaterCheck))
                        WorldGen.WaterCheck();

                    if (Stages.HasFlag(LoadStage.QuickSettle))
                    {
                        int liquidChecks = 0;
                        Liquid.quickSettle = true;
                        int totalLiquids = Liquid.numLiquid + LiquidBuffer.numLiquidBuffer;
                        float progressAuxiliary = 0f;
                        while (Liquid.numLiquid > 0 && liquidChecks < 100000)
                        {
                            liquidChecks++;
                            float liquidProgress = (float)(totalLiquids - (Liquid.numLiquid + LiquidBuffer.numLiquidBuffer)) / (float)totalLiquids;
                            if (Liquid.numLiquid + LiquidBuffer.numLiquidBuffer > totalLiquids)
                                totalLiquids = Liquid.numLiquid + LiquidBuffer.numLiquidBuffer;

                            if (liquidProgress > progressAuxiliary)
                                progressAuxiliary = liquidProgress;
                            else
                                liquidProgress = progressAuxiliary;

                            Main.statusText = Lang.gen[27].Value + " " + (int)(liquidProgress * 100f / 2f + 50f) + "%";
                            Liquid.UpdateLiquid();
                        }
                        Liquid.quickSettle = false;
                    }

                    Main.weatherCounter = WorldGen.genRand.Next(3600, 18000);
                    if (Stages.HasFlag(LoadStage.ResetClouds))
                        Cloud.resetClouds();

                    if (Stages.HasFlag(LoadStage.WaterCheck))
                        WorldGen.WaterCheck();
                    WorldGen.gen = false;
                    if (Stages.HasFlag(LoadStage.SetFireFlyChance))
                        NPC.setFireFlyChance();

                    if (Stages.HasFlag(LoadStage.SlimeRain) && Main.slimeRainTime > 0.0)
                        Main.StartSlimeRain(announce: false);

                    NPC.SetWorldSpecificMonstersByWorldID();
                }
                catch (Exception ex)
                {
                    LastThrownLoadException = ex;
                    WorldGen.loadFailed = true;
                    WorldGen.loadSuccess = false;

                    try
                    {
                        reader.Close();
                        stream.Close();
                    }
                    catch
                    {
                    }

                    if (plugin != null)
                        ServerApi.LogWriter.PluginWriteLine(plugin, "Error in reading the world: " + ex,
                            TraceLevel.Error);
                    else
                        Console.WriteLine("Error in reading the world: " + ex);
                }
            }
            catch (Exception ex)
            {
                LastThrownLoadException = ex;
                WorldGen.loadFailed = true;
                WorldGen.loadSuccess = false;
                if (plugin != null)
                    ServerApi.LogWriter.PluginWriteLine(plugin, "Error when creating unsafe objects: " + ex, 
                        TraceLevel.Error);
                else
                    Console.WriteLine("Error when creating unsafe objects: "+ex);
            }
        }

        #endregion

        #region OnClearWorld

        private static void OnClearWorld(On.Terraria.WorldGen.orig_clearWorld orig)
        {
            if (Stages.HasFlag(LoadStage.ClearWorld))
                orig();
            else
            {
                WorldGen.lastMaxTilesX = Main.maxTilesX;
                WorldGen.lastMaxTilesY = Main.maxTilesY;
            }
        }

        #endregion

        #region Index

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Index(int x, int y) => x * Main.tile.Height + y;

        #endregion
        #region OnLoadWorldTiles

        private static unsafe void OnLoadWorldTiles(On.Terraria.IO.WorldFile.orig_LoadWorldTiles orig,
            BinaryReader reader, bool[] importance)
        {
            StructTile[,] providerData = FakeProviderAPI.World.Data;
            //TODO: make .Data a StructTile[] to not have to do this ugly conversion
            var length = providerData.GetLength(0) * providerData.GetLength(1);
            Span<StructTile> tiles = new Span<StructTile>();
            fixed (StructTile* p = providerData)
            {
                tiles = new Span<StructTile>(p, length);
            }

            for (int i = 0; i < Main.maxTilesX; i++)
            {
                for (int j = 0; j < Main.maxTilesY; j++)
                {
                    #region LoadWorldTile

                    int num2 = -1;
                    byte b2;
                    byte b;
                    byte b3 = (b2 = (b = 0));
                    byte b4 = reader.ReadByte();
                    bool flag = false;
                    if ((b4 & 1) == 1)
                    {
                        flag = true;
                        b3 = reader.ReadByte();
                    }

                    bool flag2 = false;
                    if (flag && (b3 & 1) == 1)
                    {
                        flag2 = true;
                        b2 = reader.ReadByte();
                    }

                    if (flag2 && (b2 & 1) == 1)
                    {
                        b = reader.ReadByte();
                    }

                    byte b5;
                    if ((b4 & 2) == 2)
                    {
                        tiles[Index(i, j)].active(active: true);
                        if ((b4 & 0x20) == 32)
                        {
                            b5 = reader.ReadByte();
                            num2 = reader.ReadByte();
                            num2 = (num2 << 8) | b5;
                        }
                        else
                        {
                            num2 = reader.ReadByte();
                        }

                        tiles[Index(i, j)].type = (ushort)num2;
                        if (importance[num2])
                        {
                            tiles[Index(i, j)].frameX = reader.ReadInt16();
                            tiles[Index(i, j)].frameY = reader.ReadInt16();
                            if (tiles[Index(i, j)].type == 144)
                            {
                                tiles[Index(i, j)].frameY = 0;
                            }
                        }
                        else
                        {
                            tiles[Index(i, j)].frameX = -1;
                            tiles[Index(i, j)].frameY = -1;
                        }

                        if ((b2 & 8) == 8)
                        {
                            tiles[Index(i, j)].color(reader.ReadByte());
                        }
                    }

                    if ((b4 & 4) == 4)
                    {
                        tiles[Index(i, j)].wall = reader.ReadByte();
                        if (tiles[Index(i, j)].wall >= 347)
                        {
                            tiles[Index(i, j)].wall = 0;
                        }

                        if ((b2 & 0x10) == 16)
                        {
                            tiles[Index(i, j)].wallColor(reader.ReadByte());
                        }
                    }

                    b5 = (byte)((b4 & 0x18) >> 3);
                    if (b5 != 0)
                    {
                        tiles[Index(i, j)].liquid = reader.ReadByte();
                        if ((b2 & 0x80) == 128)
                        {
                            tiles[Index(i, j)].shimmer(shimmer: true);
                        }
                        else if (b5 > 1)
                        {
                            if (b5 == 2)
                            {
                                tiles[Index(i, j)].lava(lava: true);
                            }
                            else
                            {
                                tiles[Index(i, j)].honey(honey: true);
                            }
                        }
                    }

                    if (b3 > 1)
                    {
                        if ((b3 & 2) == 2)
                        {
                            tiles[Index(i, j)].wire(wire: true);
                        }

                        if ((b3 & 4) == 4)
                        {
                            tiles[Index(i, j)].wire2(wire2: true);
                        }

                        if ((b3 & 8) == 8)
                        {
                            tiles[Index(i, j)].wire3(wire3: true);
                        }

                        b5 = (byte)((b3 & 0x70) >> 4);
                        if (b5 != 0 && (Main.tileSolid[tiles[Index(i, j)].type] || TileID.Sets.NonSolidSaveSlopes[tiles[Index(i, j)].type]))
                        {
                            if (b5 == 1)
                            {
                                tiles[Index(i, j)].halfBrick(halfBrick: true);
                            }
                            else
                            {
                                tiles[Index(i, j)].slope((byte)(b5 - 1));
                            }
                        }
                    }

                    if (b2 > 1)
                    {
                        if ((b2 & 2) == 2)
                        {
                            tiles[Index(i, j)].actuator(actuator: true);
                        }

                        if ((b2 & 4) == 4)
                        {
                            tiles[Index(i, j)].inActive(inActive: true);
                        }

                        if ((b2 & 0x20) == 32)
                        {
                            tiles[Index(i, j)].wire4(wire4: true);
                        }

                        if ((b2 & 0x40) == 64)
                        {
                            b5 = reader.ReadByte();
                            tiles[Index(i, j)].wall = (ushort)((b5 << 8) | tiles[Index(i, j)].wall);
                            if (tiles[Index(i, j)].wall >= 347)
                            {
                                tiles[Index(i, j)].wall = 0;
                            }
                        }
                    }

                    if (b > 1)
                    {
                        if ((b & 2) == 2)
                        {
                            tiles[Index(i, j)].invisibleBlock(invisibleBlock: true);
                        }

                        if ((b & 4) == 4)
                        {
                            tiles[Index(i, j)].invisibleWall(invisibleWall: true);
                        }

                        if ((b & 8) == 8)
                        {
                            tiles[Index(i, j)].fullbrightBlock(fullbrightBlock: true);
                        }

                        if ((b & 0x10) == 16)
                        {
                            tiles[Index(i, j)].fullbrightWall(fullbrightWall: true);
                        }
                    }

                    // Terraria.IO.WorldFile.ClearTempTiles();
                    //if (betterClearTempTiles && num2 == 127 || num2 == 504)
                    //    tiles[Index(i, j)].Clear(TileDataType.Tile | TileDataType.TilePaint);

                    int num3 = (byte)((b4 & 0xC0) >> 6) switch
                    {
                        0 => 0,
                        1 => reader.ReadByte(),
                        _ => reader.ReadInt16(),
                    };
                    if (num2 != -1)
                    {
                        if ((double)j <= Main.worldSurface)
                        {
                            if ((double)(j + num3) <= Main.worldSurface)
                            {
                                WorldGen.tileCounts[num2] += (num3 + 1) * 5;
                            }
                            else
                            {
                                int num4 = (int)(Main.worldSurface - (double)j + 1.0);
                                int num5 = num3 + 1 - num4;
                                WorldGen.tileCounts[num2] += num4 * 5 + num5;
                            }
                        }
                        else
                        {
                            WorldGen.tileCounts[num2] += num3 + 1;
                        }
                    }

                    StructTile tile = tiles[Index(i, j)];
                    while (num3 > 0)
                    {
                        j++;
                        tiles[Index(i, j)] = tile;
                        num3--;
                    }

                    #endregion
                }
            }


            WorldGen.AddUpAlignmentCounts(clearCounts: true);
            if (_versionNumber < 105)
                WorldGen.FixHearts();
        }

        #endregion
    }
}