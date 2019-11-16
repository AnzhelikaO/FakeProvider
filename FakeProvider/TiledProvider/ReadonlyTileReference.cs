#region Using
using Microsoft.Xna.Framework;
using OTAPI.Tile;
using Terraria;
#endregion
namespace FakeProvider
{
    public sealed class ReadonlyTileReference<T> : IProviderTile
    {
        #region Constants

        public const int Type_Solid = 0;
        public const int Type_Halfbrick = 1;
        public const int Type_SlopeDownRight = 2;
        public const int Type_SlopeDownLeft = 3;
        public const int Type_SlopeUpRight = 4;
        public const int Type_SlopeUpLeft = 5;
        public const int Liquid_Water = 0;
        public const int Liquid_Lava = 1;
        public const int Liquid_Honey = 2;

        #endregion

        #region Data

        private StructTile[,] Data;
        private static INamedTileCollection _Provider;
        public INamedTileCollection Provider => _Provider;
        public int X { get; }
        public int Y { get; }

        #endregion
        #region Constructor

        public ReadonlyTileReference(StructTile[,] Data, int X, int Y)
        {
            this.Data = Data;
            this.X = X;
            this.Y = Y;
        }

        #endregion

        #region Initialise

        public void Initialise()
        {
            type = 0;
            wall = 0;
            liquid = 0;
            sTileHeader = 0;
            bTileHeader = 0;
            bTileHeader2 = 0;
            bTileHeader3 = 0;
            frameX = 0;
            frameY = 0;
        }

        #endregion

        #region type

        public ushort type
        {
            get => Data[X, Y].type;
            set { }
        }

        #endregion
        #region wall

        public byte wall
        {
            get => Data[X, Y].wall;
            set { }
        }

        #endregion
        #region liquid

        public byte liquid
        {
            get => Data[X, Y].liquid;
            set { }
        }

        #endregion
        #region frameX

        public short frameX
        {
            get => Data[X, Y].frameX;
            set { }
        }

        #endregion
        #region frameY

        public short frameY
        {
            get => Data[X, Y].frameY;
            set { }
        }

        #endregion

        #region sTileHeader

        public short sTileHeader
        {
            get => Data[X, Y].sTileHeader;
            set { }
        }

        #endregion
        #region bTileHeader

        public byte bTileHeader
        {
            get => Data[X, Y].bTileHeader;
            set { }
        }

        #endregion
        #region bTileHeader2

        public byte bTileHeader2
        {
            get => Data[X, Y].bTileHeader2;
            set { }
        }

        #endregion
        #region bTileHeader3

        public byte bTileHeader3
        {
            get => Data[X, Y].bTileHeader3;
            set { }
        }

        #endregion
        #region collisionType

        public int collisionType
        {
            get
            {
                if (!active())
                    return 0;
                if (halfBrick())
                    return 2;
                if (slope() > 0)
                    return (2 + slope());
                if (Main.tileSolid[type] && !Main.tileSolidTop[type])
                    return 1;
                return -1;
            }
        }

        #endregion

        #region ClearEverything

        public void ClearEverything() { }

        #endregion
        #region ClearTile

        public void ClearTile() { }

        #endregion
        #region ClearMetadata

        public void ClearMetadata() { }

        #endregion
        #region ResetToType

        public void ResetToType(ushort Type) { }

        #endregion

        #region CopyFrom

        public void CopyFrom(ITile From) { }

        #endregion
        #region ForceCopyFrom

        public void ForceCopyFrom(ITile From)
        {
            Data[X, Y].type = From.type;
            Data[X, Y].wall = From.wall;
            Data[X, Y].liquid = From.liquid;
            Data[X, Y].sTileHeader = From.sTileHeader;
            Data[X, Y].bTileHeader = From.bTileHeader;
            Data[X, Y].bTileHeader2 = From.bTileHeader2;
            Data[X, Y].bTileHeader3 = From.bTileHeader3;
            Data[X, Y].frameX = From.frameX;
            Data[X, Y].frameY = From.frameY;
        }

        #endregion
        #region isTheSameAs

        public bool isTheSameAs(ITile Tile)
        {
            if ((Tile == null) || (sTileHeader != Tile.sTileHeader))
                return false;
            if (active())
            {
                if (type != Tile.type)
                    return false;
                if (Main.tileFrameImportant[type]
                        && ((frameX != Tile.frameX)
                        || (frameY != Tile.frameY)))
                    return false;
            }
            if ((wall != Tile.wall) || (liquid != Tile.liquid))
                return false;
            if (Tile.liquid == 0)
            {
                if (wallColor() != Tile.wallColor())
                    return false;
                if (wire4() != Tile.wire4())
                    return false;
            }
            else if (bTileHeader != Tile.bTileHeader)
                return false;
            return true;
        }

        #endregion
        #region actColor

        const double ActNum = 0.4;
        public Color actColor(Color oldColor)
        {
            if (!inActive())
                return oldColor;

            return new Color
            (
                ((byte)(ActNum * oldColor.R)),
                ((byte)(ActNum * oldColor.G)),
                ((byte)(ActNum * oldColor.B)),
                oldColor.A
            );
        }

        #endregion
        
        #region lava

        public bool lava() => ((bTileHeader & 32) == 32);
        public void lava(bool Lava) { }

        #endregion
        #region honey

        public bool honey() => ((bTileHeader & 64) == 64);
        public void honey(bool Honey) { }

        #endregion
        #region liquidType

        public byte liquidType() => (byte)((bTileHeader & 96) >> 5);
        public void liquidType(int LiquidType) { }

        #endregion
        #region checkingLiquid

        public bool checkingLiquid() => ((bTileHeader3 & 8) == 8);
        public void checkingLiquid(bool CheckingLiquid) { }

        #endregion
        #region skipLiquid

        public bool skipLiquid() => ((bTileHeader3 & 16) == 16);
        public void skipLiquid(bool SkipLiquid) { }

        #endregion

        #region frame

        public byte frameNumber() => (byte)((bTileHeader2 & 48) >> 4);
        public void frameNumber(byte FrameNumber) { }

        public byte wallFrameNumber() => (byte)((bTileHeader2 & 192) >> 6);
        public void wallFrameNumber(byte WallFrameNumber) { }

        public int wallFrameX() => ((bTileHeader2 & 15) * 36);
        public void wallFrameX(int WallFrameX) { }

        public int wallFrameY() => ((bTileHeader3 & 7) * 36);
        public void wallFrameY(int WallFrameY) { }

        #endregion

        #region color

        public byte color() => (byte)(sTileHeader & 31);
        public void color(byte Color) { }

        #endregion
        #region wallColor

        public byte wallColor() => (byte)(bTileHeader & 31);
        public void wallColor(byte WallColor) { }

        #endregion

        #region active

        public bool active() => ((sTileHeader & 32) == 32);
        public void active(bool Active) { }

        #endregion
        #region inActive

        public bool inActive() => ((sTileHeader & 64) == 64);
        public void inActive(bool InActive) { }

        #endregion
        public bool nactive() => ((sTileHeader & 96) == 32);

        #region wire

        public bool wire() => ((sTileHeader & 128) == 128);
        public void wire(bool Wire) { }

        #endregion
        #region wire2

        public bool wire2() => ((sTileHeader & 256) == 256);
        public void wire2(bool Wire2) { }

        #endregion
        #region wire3

        public bool wire3() => ((sTileHeader & 512) == 512);
        public void wire3(bool Wire3) { }

        #endregion
        #region wire4

        public bool wire4() => ((bTileHeader & 128) == 128);

        public void wire4(bool Wire4) { }

        #endregion
        #region actuator

        public bool actuator() => ((sTileHeader & 2048) == 2048);
        public void actuator(bool Actuator) { }

        #endregion

        #region halfBrick

        public bool halfBrick() => ((sTileHeader & 1024) == 1024);
        public void halfBrick(bool HalfBrick) { }

        #endregion
        #region slope

        public byte slope() => (byte)((sTileHeader & 28672) >> 12);
        public void slope(byte Slope) { }

        #endregion
        #region topSlope

        public bool topSlope()
        {
            byte b = slope();
            return ((b == 1) || (b == 2));
        }

        #endregion
        #region bottomSlope

        public bool bottomSlope()
        {
            byte b = slope();
            return ((b == 3) || (b == 4));
        }

        #endregion
        #region leftSlope

        public bool leftSlope()
        {
            byte b = slope();
            return ((b == 2) || (b == 4));
        }

        #endregion
        #region rightSlope

        public bool rightSlope()
        {
            byte b = slope();
            return ((b == 1) || (b == 3));
        }

        #endregion
        public bool HasSameSlope(ITile Tile) =>
            ((sTileHeader & 29696) == (Tile.sTileHeader & 29696));
        #region blockType

        public int blockType()
        {
            if (halfBrick())
                return 1;
            int num = slope();
            if (num > 0)
                num++;
            return num;
        }

        #endregion

        public object Clone() => MemberwiseClone();
        #region ToString

        public new string ToString() =>
            $"Tile Type:{type} Active:{active()} " +
            $"Wall:{wall} Slope:{slope()} fX:{frameX} fY:{frameY}";

        #endregion
    }
}