﻿#region Using
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Tile_Entities;
using Terraria.ID;
using Terraria.Net.Sockets;
#endregion
namespace FakeProvider
{
    class SendSectionPacket
    {
        #region Send

        public static void Send(int Who, int IgnoreIndex,
                int X, int Y, int Width, int Height) =>
            Send(((Who == -1) ? FakeProviderPlugin.AllPlayers : new int[] { Who }),
                IgnoreIndex, X, Y, Width, Height);

        public static void Send(IEnumerable<int> Who, int IgnoreIndex,
            int X, int Y, int Width, int Height)
        {
            if (Who == null)
                return;

            List<RemoteClient> clients = new List<RemoteClient>();
			foreach (int i in Who)
            {
                if (i == IgnoreIndex)
                    continue;
                if ((i < 0) || (i >= Main.maxPlayers))
                    throw new ArgumentOutOfRangeException(nameof(Who));
                RemoteClient client = Netplay.Clients[i];
				// Not checking NetMessage.buffer[i].broadcast since section packet (because of section packet on connection)
				if (client?.IsConnected() == true)
					clients.Add(client);
            }

			foreach (var group in FakeProviderAPI.GroupByPersonal(clients, X, Y, Width, Height))
            {
				FakeProviderPlugin.SendTo(group, Generate(group.Key, X, Y, Width, Height));
			}
		}

		#endregion
		#region Generate

		private static byte[] Generate(IEnumerable<TileProvider> providers, int X, int Y, int Width, int Height)
		{

			byte[] data;
			using (MemoryStream ms = new MemoryStream())
			using (BinaryWriter bw = new BinaryWriter(ms))
			{
				bw.BaseStream.Position = 2L;
				bw.Write((byte)PacketTypes.TileSendSection);
				CompressTileBlock(providers, bw, X, Y, (short)Width, (short)Height);
				long position = bw.BaseStream.Position;
				bw.BaseStream.Position = 0L;
				bw.Write((short)position);
				bw.BaseStream.Position = position;
				data = ms.ToArray();
			}
			return data;
		}

		#endregion
		#region CompressTileBlock

		private static int CompressTileBlock(IEnumerable<TileProvider> providers,
			BinaryWriter writer, int xStart, int yStart, short width, short height)
		{
			using (DeflateStream deflateStream = new DeflateStream(writer.BaseStream, (CompressionMode)1, true))
            {
                BinaryWriter binaryWriter = new BinaryWriter(deflateStream);
                binaryWriter.Write((int)xStart);
                binaryWriter.Write((int)yStart);
                binaryWriter.Write((short)width);
                binaryWriter.Write((short)height);

                CompressTileBlock_Inner(providers, binaryWriter, xStart, yStart, (int)width, (int)height);
            }
            return 0;
		}

        #endregion
        #region CompressTileBlock_Inner

        private static void CompressTileBlock_Inner(IEnumerable<TileProvider> providers,
            BinaryWriter writer, int xStart, int yStart, int width, int height)
		{
            short num = 0;
            short num2 = 0;
            short num3 = 0;
            short num4 = 0;
            int num5 = 0;
            int num6 = 0;
            byte b = 0;
            byte[] array = new byte[16];
            ITile tile = null;
            (ModFramework.ICollection<ITile> tiles, bool relative) = FakeProviderAPI.ApplyPersonal(providers, xStart, yStart, width, height); //
            int dj = relative ? -xStart : 0; //
            int di = relative ? -yStart : 0; //
            for (int i = yStart; i < yStart + height; i++)
            {
                for (int j = xStart; j < xStart + width; j++)
                {
                    ITile tile2 = tiles[j + dj, i + di]; //
                    if (tile2.isTheSameAs(tile) && TileID.Sets.AllowsSaveCompressionBatching[(int)tile2.type])
                    {
                        num4 += 1;
                    }
                    else
                    {
                        if (tile != null)
                        {
                            if (num4 > 0)
                            {
                                array[num5] = (byte)(num4 & 255);
                                num5++;
                                if (num4 > 255)
                                {
                                    b |= 128;
                                    array[num5] = (byte)(((int)num4 & 65280) >> 8);
                                    num5++;
                                }
                                else
                                {
                                    b |= 64;
                                }
                            }
                            array[num6] = b;
                            writer.Write(array, num6, num5 - num6);
                            num4 = 0;
                        }
                        num5 = 4;
                        byte b4;
                        byte b3;
                        byte b2 = b = (b3 = (b4 = 0));
                        if (tile2.active())
                        {
                            b |= 2;
                            array[num5] = (byte)tile2.type;
                            num5++;
                            if (tile2.type > 255)
                            {
                                array[num5] = (byte)(tile2.type >> 8);
                                num5++;
                                b |= 32;
                            }
                            if (TileID.Sets.BasicChest[(int)tile2.type] && tile2.frameX % 36 == 0 && tile2.frameY % 36 == 0)
                            {
                                short num7 = (short)Chest.FindChest(j, i);
                                if (num7 != -1)
                                {
                                    NetMessage._compressChestList[(int)num] = num7;
                                    num += 1;
                                }
                            }
                            if (tile2.type == 88 && tile2.frameX % 54 == 0 && tile2.frameY % 36 == 0)
                            {
                                short num8 = (short)Chest.FindChest(j, i);
                                if (num8 != -1)
                                {
                                    NetMessage._compressChestList[(int)num] = num8;
                                    num += 1;
                                }
                            }
                            if (tile2.type == 85 && tile2.frameX % 36 == 0 && tile2.frameY % 36 == 0)
                            {
                                short num9 = (short)Sign.ReadSign(j, i, true);
                                if (num9 != -1)
                                {
                                    short[] compressSignList = NetMessage._compressSignList;
                                    short num10 = num2;
                                    num2 = (short)(num10 + 1);
                                    compressSignList[(int)num10] = num9;
                                }
                            }
                            if (tile2.type == 55 && tile2.frameX % 36 == 0 && tile2.frameY % 36 == 0)
                            {
                                short num11 = (short)Sign.ReadSign(j, i, true);
                                if (num11 != -1)
                                {
                                    short[] compressSignList2 = NetMessage._compressSignList;
                                    short num12 = num2;
                                    num2 = (short)(num12 + 1);
                                    compressSignList2[(int)num12] = num11;
                                }
                            }
                            if (tile2.type == 425 && tile2.frameX % 36 == 0 && tile2.frameY % 36 == 0)
                            {
                                short num13 = (short)Sign.ReadSign(j, i, true);
                                if (num13 != -1)
                                {
                                    short[] compressSignList3 = NetMessage._compressSignList;
                                    short num14 = num2;
                                    num2 = (short)(num14 + 1);
                                    compressSignList3[(int)num14] = num13;
                                }
                            }
                            if (tile2.type == 573 && tile2.frameX % 36 == 0 && tile2.frameY % 36 == 0)
                            {
                                short num15 = (short)Sign.ReadSign(j, i, true);
                                if (num15 != -1)
                                {
                                    short[] compressSignList4 = NetMessage._compressSignList;
                                    short num16 = num2;
                                    num2 = (short)(num16 + 1);
                                    compressSignList4[(int)num16] = num15;
                                }
                            }
                            if (tile2.type == 378 && tile2.frameX % 36 == 0 && tile2.frameY == 0)
                            {
                                int num17 = TETrainingDummy.Find(j, i);
                                if (num17 != -1)
                                {
                                    short[] compressEntities = NetMessage._compressEntities;
                                    short num18 = num3;
                                    num3 = (short)(num18 + 1);
                                    compressEntities[(int)num18] = (short)num17;
                                }
                            }
                            if (tile2.type == 395 && tile2.frameX % 36 == 0 && tile2.frameY == 0)
                            {
                                int num19 = TEItemFrame.Find(j, i);
                                if (num19 != -1)
                                {
                                    short[] compressEntities2 = NetMessage._compressEntities;
                                    short num20 = num3;
                                    num3 = (short)(num20 + 1);
                                    compressEntities2[(int)num20] = (short)num19;
                                }
                            }
                            if (tile2.type == 520 && tile2.frameX % 18 == 0 && tile2.frameY == 0)
                            {
                                int num21 = TEFoodPlatter.Find(j, i);
                                if (num21 != -1)
                                {
                                    short[] compressEntities3 = NetMessage._compressEntities;
                                    short num22 = num3;
                                    num3 = (short)(num22 + 1);
                                    compressEntities3[(int)num22] = (short)num21;
                                }
                            }
                            if (tile2.type == 471 && tile2.frameX % 54 == 0 && tile2.frameY == 0)
                            {
                                int num23 = TEWeaponsRack.Find(j, i);
                                if (num23 != -1)
                                {
                                    short[] compressEntities4 = NetMessage._compressEntities;
                                    short num24 = num3;
                                    num3 = (short)(num24 + 1);
                                    compressEntities4[(int)num24] = (short)num23;
                                }
                            }
                            if (tile2.type == 470 && tile2.frameX % 36 == 0 && tile2.frameY == 0)
                            {
                                int num25 = TEDisplayDoll.Find(j, i);
                                if (num25 != -1)
                                {
                                    short[] compressEntities5 = NetMessage._compressEntities;
                                    short num26 = num3;
                                    num3 = (short)(num26 + 1);
                                    compressEntities5[(int)num26] = (short)num25;
                                }
                            }
                            if (tile2.type == 475 && tile2.frameX % 54 == 0 && tile2.frameY == 0)
                            {
                                int num27 = TEHatRack.Find(j, i);
                                if (num27 != -1)
                                {
                                    short[] compressEntities6 = NetMessage._compressEntities;
                                    short num28 = num3;
                                    num3 = (short)(num28 + 1);
                                    compressEntities6[(int)num28] = (short)num27;
                                }
                            }
                            if (tile2.type == 597 && tile2.frameX % 54 == 0 && tile2.frameY % 72 == 0)
                            {
                                int num29 = TETeleportationPylon.Find(j, i);
                                if (num29 != -1)
                                {
                                    short[] compressEntities7 = NetMessage._compressEntities;
                                    short num30 = num3;
                                    num3 = (short)(num30 + 1);
                                    compressEntities7[(int)num30] = (short)num29;
                                }
                            }
                            if (Main.tileFrameImportant[(int)tile2.type])
                            {
                                array[num5] = (byte)(tile2.frameX & 255);
                                num5++;
                                array[num5] = (byte)(((int)tile2.frameX & 65280) >> 8);
                                num5++;
                                array[num5] = (byte)(tile2.frameY & 255);
                                num5++;
                                array[num5] = (byte)(((int)tile2.frameY & 65280) >> 8);
                                num5++;
                            }
                            if (tile2.color() != 0)
                            {
                                b3 |= 8;
                                array[num5] = tile2.color();
                                num5++;
                            }
                        }
                        if (tile2.wall != 0)
                        {
                            b |= 4;
                            array[num5] = (byte)tile2.wall;
                            num5++;
                            if (tile2.wallColor() != 0)
                            {
                                b3 |= 16;
                                array[num5] = tile2.wallColor();
                                num5++;
                            }
                        }
                        if (tile2.liquid != 0)
                        {
                            if (tile2.shimmer())
                            {
                                b3 |= 128;
                                b |= 8;
                            }
                            else if (tile2.lava())
                            {
                                b |= 16;
                            }
                            else if (tile2.honey())
                            {
                                b |= 24;
                            }
                            else
                            {
                                b |= 8;
                            }
                            array[num5] = tile2.liquid;
                            num5++;
                        }
                        if (tile2.wire())
                        {
                            b2 |= 2;
                        }
                        if (tile2.wire2())
                        {
                            b2 |= 4;
                        }
                        if (tile2.wire3())
                        {
                            b2 |= 8;
                        }
                        int num31;
                        if (tile2.halfBrick())
                        {
                            num31 = 16;
                        }
                        else if (tile2.slope() != 0)
                        {
                            num31 = (int)(tile2.slope() + 1) << 4;
                        }
                        else
                        {
                            num31 = 0;
                        }
                        b2 |= (byte)num31;
                        if (tile2.actuator())
                        {
                            b3 |= 2;
                        }
                        if (tile2.inActive())
                        {
                            b3 |= 4;
                        }
                        if (tile2.wire4())
                        {
                            b3 |= 32;
                        }
                        if (tile2.wall > 255)
                        {
                            array[num5] = (byte)(tile2.wall >> 8);
                            num5++;
                            b3 |= 64;
                        }
                        if (tile2.invisibleBlock())
                        {
                            b4 |= 2;
                        }
                        if (tile2.invisibleWall())
                        {
                            b4 |= 4;
                        }
                        if (tile2.fullbrightBlock())
                        {
                            b4 |= 8;
                        }
                        if (tile2.fullbrightWall())
                        {
                            b4 |= 16;
                        }
                        num6 = 3;
                        if (b4 != 0)
                        {
                            b3 |= 1;
                            array[num6] = b4;
                            num6--;
                        }
                        if (b3 != 0)
                        {
                            b2 |= 1;
                            array[num6] = b3;
                            num6--;
                        }
                        if (b2 != 0)
                        {
                            b |= 1;
                            array[num6] = b2;
                            num6--;
                        }
                        tile = tile2;
                    }
                }
            }
            if (num4 > 0)
            {
                array[num5] = (byte)(num4 & 255);
                num5++;
                if (num4 > 255)
                {
                    b |= 128;
                    array[num5] = (byte)(((int)num4 & 65280) >> 8);
                    num5++;
                }
                else
                {
                    b |= 64;
                }
            }
            array[num6] = b;
            writer.Write(array, num6, num5 - num6);
            writer.Write((short)num);
            for (int k = 0; k < (int)num; k++)
            {
                Chest chest = Main.chest[(int)NetMessage._compressChestList[k]];
                writer.Write((short)NetMessage._compressChestList[k]);
                writer.Write((short)chest.x);
                writer.Write((short)chest.y);
                writer.Write(chest.name);
            }

            //writer.Write(num2); //
            //for (int l = 0; l < (int)num2; l++) //
            //{ //
            //    Sign sign = Main.sign[(int)NetMessage._compressSignList[l]]; //
            //    writer.Write((short)NetMessage._compressSignList[l]); //
            //    writer.Write((short)sign.x); //
            //    writer.Write((short)sign.y); //
            //    writer.Write(sign.text); //
            //} //

            {   // TODO: Optimize, add a custom sign that does not exist in the world //

                var entities = providers.SelectMany(p => p.Entities); //
                var fakeSigns = entities.Where(p => p is FakeSign).Select(p => p as FakeSign).ToList(); //
                int count = fakeSigns.Count(); //

                FakeSign FindSign(int x, int y) //
                { //
                    foreach (FakeSign sign in fakeSigns) //
                        if (sign.x == x && sign.y == y) //
                            return sign; //
                    return null; //
                } //

                writer.Write(num2); //
                for (int l = 0; l < (int)num2; l++) //
                { //
                    Sign sign = Main.sign[(int)NetMessage._compressSignList[l]]; //
                    FakeSign fakeSign = FindSign(sign.x, sign.y); //
                    if (fakeSign != null) //
                        sign = fakeSign; //

                    writer.Write((short)NetMessage._compressSignList[l]); //
                    writer.Write((short)sign.x); //
                    writer.Write((short)sign.y); //
                    writer.Write(sign.text); //
                } //
            } //

            writer.Write(num3);
            for (int m = 0; m < (int)num3; m++)
            {
                TileEntity.Write(writer, TileEntity.ByID[(int)NetMessage._compressEntities[m]], false);
            }
        }

		#endregion
	}
}
