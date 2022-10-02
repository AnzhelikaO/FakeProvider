#region Using
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
        public static bool[] TileFrameImportant = new[] { false, false, false, true, true, true, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, false, false, true, false, true, true, true, true, false, true, false, true, true, true, true, false, false, false, false, false, true, false, false, false, false, false, false, true, true, false, false, false, false, true, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, true, true, true, true, false, false, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, true, false, false, true, true, false, false, false, false, false, false, false, false, false, false, true, true, false, true, true, false, false, true, true, true, true, true, true, true, true, false, true, true, true, true, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, false, false, false, false, false, true, true, true, true, false, false, false, true, false, false, false, false, false, true, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, true, false, false, false, false, false, true, false, true, true, false, true, false, false, true, true, true, true, true, true, false, false, false, false, false, false, true, true, false, false, true, false, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, true, false, false, false, true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, true, false, true, true, true, true, true, false, false, true, true, false, false, false, false, false, false, false, false, false, true, true, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, false, false, true, true, true, false, true, true, true, true, true, true, true, false, false, false, false, false, false, false, true, true, true, true, true, true, true, false, true, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, false, false, true, true, true, true, true, false, false, false, false, true, true, false, false, true, true, true, false, true, true, true, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, true, true, true, true, true, true, false, false, false, true, true, true, true, true, true, true, true, true, true, true, false, false, false, true, true, false, false, false, true, false, false, false, true, true, true, true, true, true, true, true, false, true, true, false, false, true, false, true, false, false, false, false, false, true, true, false, false, true, true, true, false, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, false, false, false, false, true, false, false, false, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, false, true, true, true, false, false, false, true, true, false, true, true, true, true, true, true, true, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, false, false, false, false, true, true, true, true, false, true, false, false, true, false, true, true, false, true, true, true, true, true, false, false, false, false, false, false, true, true, false, true, true, true, false, true, false, false, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };

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
                if (client?.IsConnected() == true)
                    clients.Add(client);
            }
            if (clients.Count == 0)
                return;

            byte[] data;

            /*data = new byte[250000];
            int count = NetMessage.CompressTileBlock(X, Y, (short)Width, (short)Height, data, 3);
            using (MemoryStream ms = new MemoryStream(data, 0, 3))
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((short)(3 + count));
                bw.Write((byte)10);
            }
            data = data.Take(3 + count).ToArray();*/

            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.BaseStream.Position = 2L;
                bw.Write((byte)PacketTypes.TileSendSection);
                CompressTileBlock(X, Y, Width, Height, bw);
                long position = bw.BaseStream.Position;
                bw.BaseStream.Position = 0L;
                bw.Write((short)position);
                bw.BaseStream.Position = position;
                data = ms.ToArray();
            }

            foreach (RemoteClient client in clients)
                try
                {
                    if (FakeProviderPlugin.NetSendBytes(client, data, 0, data.Length))
                        continue;

                    client.Socket.AsyncSend(data, 0, data.Length,
                        new SocketSendCallback(client.ServerWriteCallBack), null);
                }
                catch (IOException) { }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
        }

        #endregion
        #region CompressTileBlock

        private static int CompressTileBlock(int X, int Y,
            int Width, int Height, BinaryWriter BinaryWriter)
        {
            BinaryWriter.Write(X);
            BinaryWriter.Write(Y);
            BinaryWriter.Write((short)Width);
            BinaryWriter.Write((short)Height);
            CompressTileBlock_Inner(BinaryWriter, X, Y, Width, Height);
            return 0;
        }

        #endregion
        #region CompressTileBlock_Inner
        private static void CompressTileBlock_Inner(BinaryWriter BinaryWriter,
            int X, int Y, int Width, int Height)
        {
            short[] numArray1 = new short[8000];
            short[] numArray2 = new short[1000];
            short[] numArray3 = new short[1000];
            short num1 = 0;
            short num2 = 0;
            short num3 = 0;
            short num4 = 0;
            int index1 = 0;
            int index2 = 0;
            byte num5 = 0;
            byte[] buffer = new byte[16];
            ITile compTile = null;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    ITile tile = Main.tile[x, y];
                    if (tile.isTheSameAs(compTile))
                    {
                        ++num4;
                        continue;
                    }
                    if (compTile != null)
                    {
                        if (num4 > (short)0)
                        {
                            buffer[index1] = (byte)((uint)num4 & (uint)byte.MaxValue);
                            ++index1;
                            if (num4 > (short)byte.MaxValue)
                            {
                                num5 |= (byte)128;
                                buffer[index1] = (byte)(((int)num4 & 65280) >> 8);
                                ++index1;
                            }
                            else
                                num5 |= (byte)64;
                        }
                        buffer[index2] = num5;
                        BinaryWriter.Write(buffer, index2, index1 - index2);
                        num4 = (short)0;
                    }
                    index1 = 4;
                    int num6;
                    byte num7 = (byte)(num6 = 0);
                    byte num8 = (byte)num6;
                    num5 = (byte)num6;
                    if (tile.active())
                    {
                        num5 |= (byte)2;
                        buffer[index1] = (byte)tile.type;
                        ++index1;
                        if (tile.type > (ushort)byte.MaxValue)
                        {
                            buffer[index1] = (byte)((uint)tile.type >> 8);
                            ++index1;
                            num5 |= (byte)32;
                        }
                        if (TileID.Sets.BasicChest[(int)tile.type] && (int)tile.frameX % 36 == 0
                            && (int)tile.frameY % 36 == 0)
                        {
                            short chest = (short)Chest.FindChest(X + x, Y + y);
                            if (chest != (short)-1)
                            {
                                numArray1[(int)num1] = chest;
                                ++num1;
                            }
                        }
                        if (tile.type == (ushort)88 && (int)tile.frameX % 54 == 0
                            && (int)tile.frameY % 36 == 0)
                        {
                            short chest = (short)Chest.FindChest(X + x, Y + y);
                            if (chest != (short)-1)
                            {
                                numArray1[(int)num1] = chest;
                                ++num1;
                            }
                        }
                        if (tile.type == (ushort)85 && (int)tile.frameX % 36 == 0
                            && (int)tile.frameY % 36 == 0)
                        {
                            short num9 = (short)Sign.ReadSign(X + x, Y + y);
                            if (num9 != (short)-1)
                                numArray2[(int)num2++] = num9;
                        }
                        if (tile.type == (ushort)55 && (int)tile.frameX % 36 == 0
                            && (int)tile.frameY % 36 == 0)
                        {
                            short num9 = (short)Sign.ReadSign(X + x, Y + y);
                            if (num9 != (short)-1)
                                numArray2[(int)num2++] = num9;
                        }
                        if (tile.type == (ushort)425 && (int)tile.frameX % 36 == 0
                            && (int)tile.frameY % 36 == 0)
                        {
                            short num9 = (short)Sign.ReadSign(X + x, Y + y);
                            if (num9 != (short)-1)
                                numArray2[(int)num2++] = num9;
                        }
                        if (tile.type == (ushort)573 && (int)tile.frameX % 36 == 0
                            && (int)tile.frameY % 36 == 0)
                        {
                            short num9 = (short)Sign.ReadSign(X + x, Y + y);
                            if (num9 != (short)-1)
                                numArray2[(int)num2++] = num9;
                        }
                        if (tile.type == (ushort)378 && (int)tile.frameX % 36 == 0
                            && tile.frameY == (short)0)
                        {
                            int num9 = TETrainingDummy.Find(X + x, Y + y); // Training dummy
                            if (num9 != -1)
                                numArray3[(int)num3++] = (short)num9;
                        }
                        if (tile.type == (ushort)395 && (int)tile.frameX % 36 == 0
                            && tile.frameY == (short)0)
                        {
                            int num9 = TEItemFrame.Find(X + x, Y + y); // Item frame
                            if (num9 != -1)
                                numArray3[(int)num3++] = (short)num9;
                        }
                        if (tile.type == (ushort)520 && (int)tile.frameX % 18 == 0 && tile.frameY == (short)0)
                        {
                            int num9 = TEFoodPlatter.Find(X + x, Y + y); // Food platter (plate)
                            if (num9 != -1)
                                numArray3[(int)num3++] = (short)num9;
                        }
                        if (tile.type == (ushort)471 && (int)tile.frameX % 54 == 0 && tile.frameY == (short)0)
                        {
                            int num9 = TEWeaponsRack.Find(X + x, Y + y); // Weapons rack
                            if (num9 != -1)
                                numArray3[(int)num3++] = (short)num9;
                        }
                        if (tile.type == (ushort)470 && (int)tile.frameX % 36 == 0 && tile.frameY == (short)0)
                        {
                            int num9 = TEDisplayDoll.Find(X + x, Y + y); // Mannequin/womannequin
                            if (num9 != -1)
                                numArray3[(int)num3++] = (short)num9;
                        }
                        if (tile.type == (ushort)475 && (int)tile.frameX % 54 == 0 && tile.frameY == (short)0)
                        {
                            int num9 = TEHatRack.Find(X + x, Y + y); // Hat rack
                            if (num9 != -1)
                                numArray3[(int)num3++] = (short)num9;
                        }
                        if (tile.type == (ushort)597 && (int)tile.frameX % 54 == 0 && (int)tile.frameY % 72 == 0)
                        {
                            int num9 = TETeleportationPylon.Find(X + x, Y + y); // Pylon
                            if (num9 != -1)
                                numArray3[(int)num3++] = (short)num9;
                        }
                        if (TileFrameImportant[(int)tile.type])
                        {
                            buffer[index1] = (byte)((uint)tile.frameX & (uint)byte.MaxValue);
                            int index5 = index1 + 1;
                            buffer[index5] = (byte)(((int)tile.frameX & 65280) >> 8);
                            int index6 = index5 + 1;
                            buffer[index6] = (byte)((uint)tile.frameY & (uint)byte.MaxValue);
                            int index7 = index6 + 1;
                            buffer[index7] = (byte)(((int)tile.frameY & 65280) >> 8);
                            index1 = index7 + 1;
                        }
                        if (tile.color() != (byte)0)
                        {
                            num7 |= (byte)8;
                            buffer[index1] = tile.color();
                            ++index1;
                        }
                    }
                    if (tile.wall != (ushort)0)
                    {
                        num5 |= (byte)4;
                        buffer[index1] = (byte)tile.wall;
                        ++index1;
                        if (tile.wallColor() != (byte)0)
                        {
                            num7 |= (byte)16;
                            buffer[index1] = tile.wallColor();
                            ++index1;
                        }
                    }
                    if (tile.liquid != (byte)0)
                    {
                        if (!tile.shimmer())
                        {
                            if (tile.lava())
                                num5 |= (byte)16;
                            else if (tile.honey())
                                num5 |= (byte)24;
                            else
                                num5 |= (byte)8;
                        }
                        else
                        {
                            num7 |= 0x80;
                            num5 |= 8;
                        }
                        buffer[index1] = tile.liquid;
                        ++index1;
                    }
                    if (tile.wire())
                        num8 |= (byte)2;
                    if (tile.wire2())
                        num8 |= (byte)4;
                    if (tile.wire3())
                        num8 |= (byte)8;
                    int num10 =
                        !tile.halfBrick() ? (tile.slope() == (byte)0
                        ? 0 : (int)tile.slope() + 1 << 4) : 16;
                    byte num11 = (byte)((uint)num8 | (uint)(byte)num10);
                    if (tile.actuator())
                        num7 |= (byte)2;
                    if (tile.inActive())
                        num7 |= (byte)4;
                    if (tile.wire4())
                        num7 |= (byte)32;
                    if (tile.wall > (ushort)byte.MaxValue)
                    {
                        buffer[index1] = (byte)((uint)tile.wall >> 8);
                        ++index1;
                        num7 |= (byte)64;
                    }
                    byte b2 = 0;
                    if (tile.invisibleBlock())
                        b2 = (byte)(b2 | 2u);
                    if (tile.invisibleWall())
                        b2 = (byte)(b2 | 4u);
                    if (tile.fullbrightBlock())
                        b2 = (byte)(b2 | 8u);
                    if (tile.fullbrightWall())
                        b2  = (byte)(b2 | 0x10u);
                    index2 = 3;
                    if (b2 != 0)
                    {
                        num7 |= (byte)1;
                        buffer[index2] = b2;
                        index2--;
                    }
                    if (num7 != (byte)0)
                    {
                        num11 |= (byte)1;
                        buffer[index2] = num7;
                        --index2;
                    }
                    if (num11 != (byte)0)
                    {
                        num5 |= (byte)1;
                        buffer[index2] = num11;
                        --index2;
                    }
                    compTile = tile;
                }
            }
            if (num4 > (short)0)
            {
                buffer[index1] = (byte)((uint)num4 & (uint)byte.MaxValue);
                ++index1;
                if (num4 > (short)byte.MaxValue)
                {
                    num5 |= (byte)128;
                    buffer[index1] = (byte)(((int)num4 & 65280) >> 8);
                    ++index1;
                }
                else
                    num5 |= (byte)64;
            }
            buffer[index2] = num5;
            BinaryWriter.Write(buffer, index2, index1 - index2);

            BinaryWriter.Write(num1);
            for (int index3 = 0; index3 < (int)num1; ++index3)
            {
                Chest chest = Main.chest[numArray1[index3]];
                BinaryWriter.Write(numArray1[index3]);
                BinaryWriter.Write((short)chest.x);
                BinaryWriter.Write((short)chest.y);
                BinaryWriter.Write(chest.name);
            }
            BinaryWriter.Write(num2);
            for (int index3 = 0; index3 < (int)num2; ++index3)
            {
                Sign sign = Main.sign[(int)numArray2[index3]];
                BinaryWriter.Write(numArray2[index3]);
                BinaryWriter.Write((short)sign.x);
                BinaryWriter.Write((short)sign.y);
                BinaryWriter.Write(sign.text);
            }
            BinaryWriter.Write(num3);
            for (int index3 = 0; index3 < (int)num3; ++index3)
                TileEntity.Write(BinaryWriter, TileEntity.ByID[(int)numArray3[index3]], false);
        }

        #endregion
    }
}
