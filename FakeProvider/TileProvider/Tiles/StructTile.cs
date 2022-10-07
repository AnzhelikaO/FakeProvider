﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;

namespace FakeProvider
{
	[StructLayout(LayoutKind.Sequential, Size = 14, Pack = 1)]
	public struct StructTile : ITile
	{
		public const int Type_Solid = 0;
		public const int Type_Halfbrick = 1;
		public const int Type_SlopeDownRight = 2;
		public const int Type_SlopeDownLeft = 3;
		public const int Type_SlopeUpRight = 4;
		public const int Type_SlopeUpLeft = 5;
		public const int Liquid_Water = 0;
		public const int Liquid_Lava = 1;
		public const int Liquid_Honey = 2;
		public const int Liquid_Shimmer = 3;

		public ushort type;
		public ushort wall;
		public byte liquid;
		public ushort sTileHeader;
		public byte bTileHeader;
		public byte bTileHeader2;
		public byte bTileHeader3;
		public short frameX;
		public short frameY;

		ushort ITile.type { get => type; set => type = value; }
		ushort ITile.wall { get => wall; set => wall = value; }
		byte ITile.liquid { get => liquid; set => liquid = value; }
		ushort ITile.sTileHeader { get => sTileHeader; set => sTileHeader = value; }
		byte ITile.bTileHeader { get => bTileHeader; set => bTileHeader = value; }
		byte ITile.bTileHeader2 { get => bTileHeader2; set => bTileHeader2 = value; }
		byte ITile.bTileHeader3 { get => bTileHeader3; set => bTileHeader3 = value; }
		short ITile.frameX { get => frameX; set => frameX = value; }
		short ITile.frameY { get => frameY; set => frameY = value; }

		public StructTile(StructTile copy)
		{
			this.type = copy.type;
			this.wall = copy.wall;
			this.liquid = copy.liquid;
			this.sTileHeader = copy.sTileHeader;
			this.bTileHeader = copy.bTileHeader;
			this.bTileHeader2 = copy.bTileHeader2;
			this.bTileHeader3 = copy.bTileHeader3;
			this.frameX = copy.frameX;
			this.frameY = copy.frameY;
		}

		public object Clone()
		{
			return base.MemberwiseClone();
		}

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

		public void ClearTile()
		{
			this.slope(0);
			this.halfBrick(false);
			this.active(false);
			this.inActive(false);
		}

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

		public int collisionType
		{
			get
			{
				if (!this.active())
				{
					return 0;
				}
				if (this.halfBrick())
				{
					return 2;
				}
				if (this.slope() > 0)
				{
					return (int)(2 + this.slope());
				}
				if (Main.tileSolid[(int)this.type] && !Main.tileSolidTop[(int)this.type])
				{
					return 1;
				}
				return -1;
			}
		}


		public bool isTheSameAs(ITile compTile)
		{
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
			return true;
		}

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
				return;
			}
		}

		public byte liquidType()
		{
			return (byte)((this.bTileHeader & 96) >> 5);
		}

		public bool nactive()
		{
			return (this.sTileHeader & 96) == 32;
		}

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

		public bool topSlope()
		{
			byte b = this.slope();
			return b == 1 || b == 2;
		}

		public bool bottomSlope()
		{
			byte b = this.slope();
			return b == 3 || b == 4;
		}

		public bool leftSlope()
		{
			byte b = this.slope();
			return b == 2 || b == 4;
		}

		public bool rightSlope()
		{
			byte b = this.slope();
			return b == 1 || b == 3;
		}

		public bool HasSameSlope(ITile tile)
		{
			return (this.sTileHeader & 29696) == (tile.sTileHeader & 29696);
		}

		public byte wallColor()
		{
			return (byte)(this.bTileHeader & 31);
		}

		public void wallColor(byte wallColor)
		{
			this.bTileHeader = (byte)((this.bTileHeader & 224) | wallColor);
		}

		public bool lava()
		{
			return (this.bTileHeader & 32) == 32;
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

		public bool honey()
		{
			return (this.bTileHeader & 64) == 64;
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

		public int wallFrameX()
		{
			return (int)((this.bTileHeader2 & 15) * 36);
		}

		public void wallFrameX(int wallFrameX)
		{
			this.bTileHeader2 = (byte)((int)(this.bTileHeader2 & 240) | (wallFrameX / 36 & 15));
		}

		public byte frameNumber()
		{
			return (byte)((this.bTileHeader2 & 48) >> 4);
		}

		public void frameNumber(byte frameNumber)
		{
			this.bTileHeader2 = (byte)((int)(this.bTileHeader2 & 207) | (int)(frameNumber & 3) << 4);
		}

		public byte wallFrameNumber()
		{
			return (byte)((this.bTileHeader2 & 192) >> 6);
		}

		public void wallFrameNumber(byte wallFrameNumber)
		{
			this.bTileHeader2 = (byte)((int)(this.bTileHeader2 & 63) | (int)(wallFrameNumber & 3) << 6);
		}

		public int wallFrameY()
		{
			return (int)((this.bTileHeader3 & 7) * 36);
		}

		public void wallFrameY(int wallFrameY)
		{
			this.bTileHeader3 = (byte)((int)(this.bTileHeader3 & 248) | (wallFrameY / 36 & 7));
		}

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

		public byte color()
		{
			return (byte)(this.sTileHeader & 31);
		}

		public void color(byte color)
		{
			this.sTileHeader = (ushort)(((int)this.sTileHeader & 65504) | (int)color);
		}

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
			this.sTileHeader = (ushort)((int)this.sTileHeader & 65503);
		}

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
			this.sTileHeader = (ushort)((int)this.sTileHeader & 65471);
		}

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
			this.sTileHeader = (ushort)((int)this.sTileHeader & 65407);
		}

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
			this.sTileHeader = (ushort)((int)this.sTileHeader & 65279);
		}

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
			this.sTileHeader = (ushort)((int)this.sTileHeader & 65023);
		}

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
			this.sTileHeader = (ushort)((int)this.sTileHeader & 64511);
		}

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
			this.sTileHeader = (ushort)((int)this.sTileHeader & 63487);
		}

		public byte slope()
		{
			return (byte)((this.sTileHeader & 28672) >> 12);
		}

		public void slope(byte slope)
		{
			this.sTileHeader = (ushort)(((int)this.sTileHeader & 36863) | (int)(slope & 7) << 12);
		}

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
				this.color(0);
			}
			if ((types & TileDataType.WallPaint) != (TileDataType)0)
			{
				this.wallColor(0);
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

		public bool shimmer()
		{
			return (this.bTileHeader & 96) == 96;
		}

		public void shimmer(bool shimmer)
		{
			if (shimmer)
			{
				this.bTileHeader = (byte)((this.bTileHeader & (byte)159) | (byte)96);
				return;
			}
			this.bTileHeader &= 159;
		}

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

		public void CopyPaintAndCoating(ITile other)
		{
			this.color(other.color());
			this.wallColor(other.wallColor());
			this.invisibleBlock(other.invisibleBlock());
			this.invisibleWall(other.invisibleWall());
			this.fullbrightBlock(other.fullbrightBlock());
			this.fullbrightWall(other.fullbrightWall());
		}

		public TileColorCache BlockColorAndCoating()
		{
			return new TileColorCache()
				{
					Color = color(),
					FullBright = fullbrightBlock(),
					Invisible = invisibleBlock()
				};
		}

		public TileColorCache WallColorAndCoating()
		{
			return new TileColorCache()
				{
					Color = wallColor(),
					FullBright = fullbrightWall(),
					Invisible = invisibleWall()
				};
		}

		public void UseBlockColors(TileColorCache cache)
		{
			this.color(cache.Color);
			this.fullbrightBlock(cache.FullBright);
			this.invisibleBlock(cache.Invisible);
		}

		public void UseWallColors(TileColorCache cache)
		{
			this.wallColor(cache.Color);
			this.fullbrightWall(cache.FullBright);
			this.invisibleWall(cache.Invisible);
		}

		public void ClearBlockPaintAndCoating()
		{
			this.color(0);
			this.fullbrightBlock(false);
			this.invisibleBlock(false);
		}

		public void ClearWallPaintAndCoating()
		{
			this.wallColor(0);
			this.fullbrightWall(false);
			this.invisibleWall(false);
		}
	}
}
