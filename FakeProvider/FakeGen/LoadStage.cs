using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeProvider.FakeGen
{
    [Flags]
    public enum LoadStage
    {
        None = 0,

        /// <summary>
        /// Allows <see cref="Terraria.Main.checkXMas"/>
        /// </summary>
        CheckXMas = 1 << 0,
        /// <summary>
        /// Allows <see cref="Terraria.Main.checkHalloween"/>
        /// </summary>
        CheckHalloween = 1 << 1,

        /// <summary>
        /// Allows <see cref="Terraria.WorldGen.clearWorld"/>
        /// </summary>
        ClearWorld = 1 << 2,
        
        /// <summary>
        /// Allows <see cref="Terraria.IO.WorldFile.CheckSavedOreTiers"/>
        /// </summary>
        CheckSavedOreTiers = 1 << 3,

        /// <summary>
        /// Allows <see cref="Terraria.IO.WorldFile.ConvertOldTileEntities"/>
        /// </summary>
        ConvertOldTileEntities = 1 << 4,

        /// <summary>
        /// Allows <see cref="Terraria.IO.WorldFile.ClearTempTiles"/>
        /// </summary>
        ClearTempTiles = 1 << 5,

        /// <summary>
        /// Allows <see cref="Terraria.Liquid.QuickWater(int, int, int)"/>
        /// </summary>
        QuickWater = 1 << 6,

        /// <summary>
        /// Allows <see cref="Terraria.WorldGen.WaterCheck"/>
        /// </summary>
        WaterCheck = 1 << 7,

        /// <summary>
        /// Allows to regulate water in the world.
        /// </summary>
        QuickSettle = 1 << 8,

        /// <summary>
        /// Allows <see cref="Terraria.Cloud.resetClouds"/>
        /// </summary>
        ResetClouds = 1 << 9,

        /// <summary>
        /// Allows <see cref="Terraria.NPC.setFireFlyChance"/>
        /// </summary>
        SetFireFlyChance = 1 << 10,

        /// <summary>
        /// Allows <see cref="Terraria.Main.StartSlimeRain"/>
        /// </summary>
        SlimeRain = 1 << 11
    }
}
