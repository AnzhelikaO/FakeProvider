#region Using
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
#endregion
namespace FakeProvider
{
	public sealed class TileReference : ITile
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
		public int X { get; }
		public int Y { get; }

		#endregion
		#region Constructor

		public TileReference(StructTile[,] Data, int X, int Y)
		{
			this.Data = Data;
			this.X = X;
			this.Y = Y;
		}

		#endregion

		#region bTileHeader

		public byte bTileHeader
		{
			get => Data[X, Y].bTileHeader;
			set => Data[X, Y].bTileHeader = value;
		}

		#endregion
		#region bTileHeader2

		public byte bTileHeader2
		{
			get => Data[X, Y].bTileHeader2;
			set => Data[X, Y].bTileHeader2 = value;
		}

		#endregion
		#region bTileHeader3

		public byte bTileHeader3
		{
			get => Data[X, Y].bTileHeader3;
			set => Data[X, Y].bTileHeader3 = value;
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

		ushort ITile.sTileHeader { get => Data[X, Y].sTileHeader; set { } }

		#endregion
		#region frameX

		public short frameX
		{
			get => Data[X, Y].frameX;
			set => Data[X, Y].frameX = value;
		}

		#endregion
		#region frameY

		public short frameY
		{
			get => Data[X, Y].frameY;
			set => Data[X, Y].frameY = value;
		}

		#endregion
		#region liquid

		public byte liquid
		{
			get => Data[X, Y].liquid;
			set => Data[X, Y].liquid = value;
		}

		#endregion
		#region sTileHeader

		public ushort sTileHeader
		{
			get => Data[X, Y].sTileHeader;
			set => Data[X, Y].sTileHeader = value;
		}

		#endregion
		#region type

		public ushort type
		{
			get => Data[X, Y].type;
			set => Data[X, Y].type = value;
		}

		#endregion
		#region wall

		public ushort wall
		{
			get => Data[X, Y].wall;
			set => Data[X, Y].wall = value;
		}

        #endregion

        #region actColor

        public Color actColor(Color oldColor)
        {
            if (!this.inActive())
            {
                return oldColor;
            }
            double num = 0.4;
            return new Color((int)((byte)(num * (double)oldColor.R)), (int)((byte)(num * (double)oldColor.G)), (int)((byte)(num * (double)oldColor.B)), (int)oldColor.A);
        }

        public void actColor(ref Vector3 oldColor)
        {
            if (!this.inActive())
            {
                return;
            }
            oldColor *= 0.4f;
        }

        #endregion
		#region active

		public bool active()
        {
            return (this.sTileHeader & 32) == 32;
        }
        public void active(bool active)
        {
            if (active)
            {
                this.sTileHeader |= 32;
                return;
            }
            this.sTileHeader &= 65503;
        }

		#endregion
		#region actuator

		public bool actuator()
        {
            return (this.sTileHeader & 2048) == 2048;
        }
        public void actuator(bool actuator)
        {
            if (actuator)
            {
                this.sTileHeader |= 2048;
                return;
            }
            this.sTileHeader &= 63487;
        }

        #endregion
        #region BlockColorAndCoating

        public TileColorCache BlockColorAndCoating()
		{
            return new TileColorCache
            {
                Color = this.color(),
                FullBright = this.fullbrightBlock(),
                Invisible = this.invisibleBlock()
            };
        }

        #endregion
        #region blockType

        public int blockType()
		{
            if (this.halfBrick())
            {
                return 1;
            }
            int num = (int)this.slope();
            if (num > 0)
            {
                num++;
            }
            return num;
        }

		#endregion
		#region bottomSlope

		public bool bottomSlope()
        {
            byte b = this.slope();
            return b == 3 || b == 4;
        }

		#endregion
		#region checkingLiquid

		public bool checkingLiquid()
        {
            return (this.bTileHeader3 & 8) == 8;
        }

        public void checkingLiquid(bool checkingLiquid)
        {
            if (checkingLiquid)
            {
                this.bTileHeader3 |= 8;
                return;
            }
            this.bTileHeader3 &= 247;
        }

        #endregion
        #region Clear

        public void Clear(TileDataType types)
		{
            if ((types & TileDataType.Tile) != (TileDataType)0)
            {
                this.type = 0;
                this.active(false);
                this.frameX = 0;
                this.frameY = 0;
            }
            if ((types & TileDataType.Wall) != (TileDataType)0)
            {
                this.wall = 0;
                this.wallFrameX(0);
                this.wallFrameY(0);
            }
            if ((types & TileDataType.TilePaint) != (TileDataType)0)
            {
                this.ClearBlockPaintAndCoating();
            }
            if ((types & TileDataType.WallPaint) != (TileDataType)0)
            {
                this.ClearWallPaintAndCoating();
            }
            if ((types & TileDataType.Liquid) != (TileDataType)0)
            {
                this.liquid = 0;
                this.liquidType(0);
                this.checkingLiquid(false);
            }
            if ((types & TileDataType.Slope) != (TileDataType)0)
            {
                this.slope(0);
                this.halfBrick(false);
            }
            if ((types & TileDataType.Wiring) != (TileDataType)0)
            {
                this.wire(false);
                this.wire2(false);
                this.wire3(false);
                this.wire4(false);
            }
            if ((types & TileDataType.Actuator) != (TileDataType)0)
            {
                this.actuator(false);
                this.inActive(false);
            }
        }

		#endregion
        #region ClearBlockPaintAndCoating

        public void ClearBlockPaintAndCoating()
        {
            this.color(0);
            this.fullbrightBlock(false);
            this.invisibleBlock(false);
        }

        #endregion
		#region ClearEverything

		public void ClearEverything()
        {
            this.type = 0;
            this.wall = 0;
            this.liquid = 0;
            this.sTileHeader = 0;
            this.bTileHeader = 0;
            this.bTileHeader2 = 0;
            this.bTileHeader3 = 0;
            this.frameX = 0;
            this.frameY = 0;
        }

		#endregion
		#region ClearMetadata

		public void ClearMetadata()
        {
            this.liquid = 0;
            this.sTileHeader = 0;
            this.bTileHeader = 0;
            this.bTileHeader2 = 0;
            this.bTileHeader3 = 0;
            this.frameX = 0;
            this.frameY = 0;
        }

		#endregion
		#region ClearTile

		public void ClearTile()
        {
            this.slope(0);
            this.halfBrick(false);
            this.active(false);
            this.inActive(false);
        }

		#endregion
        #region ClearWallPaintAndCoating

        public void ClearWallPaintAndCoating()
        {
            this.wallColor(0);
            this.fullbrightWall(false);
            this.invisibleWall(false);
        }

        #endregion
		#region Clone

		public object Clone()
        {
            return base.MemberwiseClone();
        }

        #endregion
        #region color

        public byte color()
        {
            return (byte)(this.sTileHeader & 31);
        }

        public void color(byte color)
		{
            this.sTileHeader = (ushort)((this.sTileHeader & 65504) | (ushort)color);
        }

		#endregion
		#region CopyFrom

		public void CopyFrom(ITile from)
        {
            this.type = from.type;
            this.wall = from.wall;
            this.liquid = from.liquid;
            this.sTileHeader = from.sTileHeader;
            this.bTileHeader = from.bTileHeader;
            this.bTileHeader2 = from.bTileHeader2;
            this.bTileHeader3 = from.bTileHeader3;
            this.frameX = from.frameX;
            this.frameY = from.frameY;
        }

		#endregion
        #region CopyPaintAndCoating

        public void CopyPaintAndCoating(ITile other)
        {
            this.color(other.color());
            this.invisibleBlock(other.invisibleBlock());
            this.fullbrightBlock(other.fullbrightBlock());
        }

        #endregion
		#region frameNumber

		public byte frameNumber()
        {
            return (byte)((this.bTileHeader2 & 48) >> 4);
        }

        public void frameNumber(byte frameNumber)
        {
            this.bTileHeader2 = (byte)((int)(this.bTileHeader2 & 207) | ((int)(frameNumber & 3) << 4));
        }

        #endregion
        #region fullbrightBlock

        public bool fullbrightBlock()
		{
            return (this.bTileHeader3 & 128) == 128;
        }

		public void fullbrightBlock(bool fullbrightBlock)
		{
            if (fullbrightBlock)
            {
                this.bTileHeader3 |= 128;
                return;
            }
            this.bTileHeader3 = (byte)((int)this.bTileHeader3 & -129);
        }

        #endregion
        #region fullbrightWall

        public bool fullbrightWall()
		{
            return (this.sTileHeader & 32768) == 32768;
        }

		public void fullbrightWall(bool fullbrightWall)
		{
            if (fullbrightWall)
            {
                this.sTileHeader |= 32768;
                return;
            }
            this.sTileHeader = (ushort)((int)this.sTileHeader & -32769);
        }

        #endregion
		#region halfBrick

		public bool halfBrick()
        {
            return (this.sTileHeader & 1024) == 1024;
        }

        public void halfBrick(bool halfBrick)
		{
            if (halfBrick)
            {
                this.sTileHeader |= 1024;
                return;
            }
            this.sTileHeader &= 64511;
        }

		#endregion
		#region HasSameSlope

		public bool HasSameSlope(ITile tile)
        {
            return (this.sTileHeader & 29696) == (tile.sTileHeader & 29696);
        }

        #endregion
        #region honey

        public bool honey()
        {
            return (this.bTileHeader & 96) == 64;
        }

        public void honey(bool honey)
        {
            if (honey)
            {
                this.bTileHeader = (byte)((this.bTileHeader & 159) | 64);
                return;
            }
            this.bTileHeader &= 191;
        }

        #endregion
        #region inActive

        public bool inActive()
        {
            return (this.sTileHeader & 64) == 64;
        }

        public void inActive(bool inActive)
        {
            if (inActive)
            {
                this.sTileHeader |= 64;
                return;
            }
            this.sTileHeader &= 65471;
        }

        #endregion
        #region invisibleBlock

        public bool invisibleBlock()
		{
            return (this.bTileHeader3 & 32) == 32;
        }

        public void invisibleBlock(bool invisibleBlock)
		{
            if (invisibleBlock)
            {
                this.bTileHeader3 |= 32;
                return;
            }
            this.bTileHeader3 = (byte)((int)this.bTileHeader3 & -33);
        }

        #endregion
        #region invisibleWall

        public bool invisibleWall()
		{
            return (this.bTileHeader3 & 64) == 64;
        }

		public void invisibleWall(bool invisibleWall)
		{
            if (invisibleWall)
            {
                this.bTileHeader3 |= 64;
                return;
            }
            this.bTileHeader3 = (byte)((int)this.bTileHeader3 & -65);
        }

        #endregion
		#region isTheSameAs

		public bool isTheSameAs(ITile compTile)
		{
            if (compTile == null)
            {
                return false;
            }
            if (this.sTileHeader != compTile.sTileHeader)
            {
                return false;
            }
            if (this.active())
            {
                if (this.type != compTile.type)
                {
                    return false;
                }
                if (Main.tileFrameImportant[(int)this.type] && (this.frameX != compTile.frameX || this.frameY != compTile.frameY))
                {
                    return false;
                }
            }
            if (this.wall != compTile.wall || this.liquid != compTile.liquid)
            {
                return false;
            }
            if (compTile.liquid == 0)
            {
                if (this.wallColor() != compTile.wallColor())
                {
                    return false;
                }
                if (this.wire4() != compTile.wire4())
                {
                    return false;
                }
            }
            else if (this.bTileHeader != compTile.bTileHeader)
            {
                return false;
            }
            return this.invisibleBlock() == compTile.invisibleBlock() && this.invisibleWall() == compTile.invisibleWall() && this.fullbrightBlock() == compTile.fullbrightBlock() && this.fullbrightWall() == compTile.fullbrightWall();
        }

		#endregion
		#region lava

		public bool lava()
        {
            return (this.bTileHeader & 96) == 32;
        }

        public void lava(bool lava)
        {
            if (lava)
            {
                this.bTileHeader = (byte)((this.bTileHeader & 159) | 32);
                return;
            }
            this.bTileHeader &= 223;
        }

        #endregion
        #region leftSlope

        public bool leftSlope()
        {
            byte b = this.slope();
            return b == 2 || b == 4;
        }

        #endregion
        #region liquidType

        public byte liquidType()
        {
            return (byte)((this.bTileHeader & 96) >> 5);
        }

        public void liquidType(int liquidType)
        {
            if (liquidType == 0)
            {
                this.bTileHeader &= 159;
                return;
            }
            if (liquidType == 1)
            {
                this.lava(true);
                return;
            }
            if (liquidType == 2)
            {
                this.honey(true);
                return;
            }
            if (liquidType == 3)
            {
                this.shimmer(true);
            }
        }

        #endregion
        #region nactive

        public bool nactive()
        {
            return (this.sTileHeader & 96) == 32;
        }

        #endregion
        #region ResetToType

        public void ResetToType(ushort type)
        {
            this.liquid = 0;
            this.sTileHeader = 32;
            this.bTileHeader = 0;
            this.bTileHeader2 = 0;
            this.bTileHeader3 = 0;
            this.frameX = 0;
            this.frameY = 0;
            this.type = type;
        }

        #endregion
        #region rightSlope

        public bool rightSlope()
        {
            byte b = this.slope();
            return b == 1 || b == 3;
        }

        #endregion
        #region shimmer

        public bool shimmer()
        {
            return (this.bTileHeader & 96) == 96;
        }

        public void shimmer(bool shimmer)
        {
            if (shimmer)
            {
                this.bTileHeader = (byte)((this.bTileHeader & 159) | 96);
                return;
            }
            this.bTileHeader &= 159;
        }

        #endregion
        #region skipLiquid

        public bool skipLiquid()
        {
            return (this.bTileHeader3 & 16) == 16;
        }

        public void skipLiquid(bool skipLiquid)
        {
            if (skipLiquid)
            {
                this.bTileHeader3 |= 16;
                return;
            }
            this.bTileHeader3 &= 239;
        }

        #endregion
        #region slope

        public byte slope()
        {
            return (byte)((this.sTileHeader & 28672) >> 12);
        }

        public void slope(byte slope)
        {
            this.sTileHeader = (ushort)((int)(this.sTileHeader & 36863) | ((int)(slope & 7) << 12));
        }

        #endregion
        #region topSlope

        public bool topSlope()
        {
            byte b = this.slope();
            return b == 1 || b == 2;
        }

        #endregion
        #region UseBlockColors

        public void UseBlockColors(TileColorCache cache)
        {
            cache.ApplyToBlock(this);
        }

        #endregion
        #region UseWallColors

        public void UseWallColors(TileColorCache cache)
        {
            cache.ApplyToWall(this);
        }

        #endregion
        #region wallColor

        public byte wallColor()
        {
            return (byte)(this.bTileHeader & 31);
        }

        public void wallColor(byte wallColor)
        {
            this.bTileHeader = (byte)((this.bTileHeader & 224) | wallColor);
        }

        #endregion
        #region WallColorAndCoating

        public TileColorCache WallColorAndCoating()
        {
            return new TileColorCache
            {
                Color = this.wallColor(),
                FullBright = this.fullbrightWall(),
                Invisible = this.invisibleWall()
            };
        }

        #endregion
        #region wallFrameNumber

        public byte wallFrameNumber()
        {
            return (byte)((this.bTileHeader2 & 192) >> 6);
        }

        public void wallFrameNumber(byte wallFrameNumber)
        {
            this.bTileHeader2 = (byte)((int)(this.bTileHeader2 & 63) | ((int)(wallFrameNumber & 3) << 6));
        }

        #endregion
        #region wallFrameX

        public int wallFrameX()
        {
            return (int)((this.bTileHeader2 & 15) * 36);
        }

        public void wallFrameX(int wallFrameX)
        {
            this.bTileHeader2 = (byte)((int)(this.bTileHeader2 & 240) | ((wallFrameX / 36) & 15));
        }

        #endregion
        #region wallFrameY

        public int wallFrameY()
        {
            return (int)((this.bTileHeader3 & 7) * 36);
        }

        public void wallFrameY(int wallFrameY)
        {
            this.bTileHeader3 = (byte)((int)(this.bTileHeader3 & 248) | ((wallFrameY / 36) & 7));
        }

        #endregion
        #region wire

        public bool wire()
        {
            return (this.sTileHeader & 128) == 128;
        }

        public void wire(bool wire)
        {
            if (wire)
            {
                this.sTileHeader |= 128;
                return;
            }
            this.sTileHeader &= 65407;
        }

        #endregion
        #region wire2

        public bool wire2()
        {
            return (this.sTileHeader & 256) == 256;
        }

        public void wire2(bool wire2)
        {
            if (wire2)
            {
                this.sTileHeader |= 256;
                return;
            }
            this.sTileHeader &= 65279;
        }

        #endregion
        #region wire3

        public bool wire3()
        {
            return (this.sTileHeader & 512) == 512;
        }

        public void wire3(bool wire3)
        {
            if (wire3)
            {
                this.sTileHeader |= 512;
                return;
            }
            this.sTileHeader &= 65023;
        }

        #endregion
        #region wire4

        public bool wire4()
        {
            return (this.bTileHeader & 128) == 128;
        }

        public void wire4(bool wire4)
        {
            if (wire4)
            {
                this.bTileHeader |= 128;
                return;
            }
            this.bTileHeader &= 127;
        }

        #endregion

        #region ToString

        public override string ToString() =>
			$"Tile Type:{type} Active:{active()} " +
			$"Wall:{wall} Slope:{slope()} fX:{frameX} fY:{frameY}";

        #endregion
    }
}