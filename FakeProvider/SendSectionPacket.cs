#region Using
using OTAPI.Tile;
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
            Send(((Who == -1) ? FakeProvider.AllPlayers : new int[] { Who }),
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
                    if (FakeProvider.NetSendBytes(client, data, 0, data.Length))
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
            if (X < 0)
            {
                Width += (short)X;
                X = 0;
            }
            if (Y < 0)
            {
                Height += (short)Y;
                Y = 0;
            }
            if ((X + Width) > Main.maxTilesX)
                Width = (short)(Main.maxTilesX - X);
            if ((Y + Height) > Main.maxTilesY)
                Height = (short)(Main.maxTilesY - Y);
            if ((Width == 0) || (Height == 0))
                return 0;

            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(X);
                bw.Write(Y);
                bw.Write((short)Width);
                bw.Write((short)Height);
                //NetMessage.CompressTileBlock_Inner(bw, X, Y, Width, Height);
                CompressTileBlock_Inner3(bw, X, Y, Width, Height);
                ms.Position = 0L;
                using (MemoryStream ms2 = new MemoryStream())
                {
                    using (DeflateStream ds = new DeflateStream(ms2, CompressionMode.Compress, true))
                    {
                        ms.CopyTo(ds);
                        ds.Flush();
                        ds.Close();
                        ds.Dispose();
                    }

                    if (ms.Length <= ms2.Length)
                    {
                        ms.Position = 0L;
                        BinaryWriter.Write((byte)0);
                        BinaryWriter.Write(ms.GetBuffer());
                    }
                    else
                    {
                        ms2.Position = 0L;
                        BinaryWriter.Write((byte)1);
                        BinaryWriter.Write(ms2.GetBuffer());
                    }
                }
            }
            return 0;
        }

        #endregion
        #region CompressTileBlock_Inner

        private static void CompressTileBlock_Inner(BinaryWriter BinaryWriter,
            int X, int Y, int Width, int Height)
        {
            short[] array3 = new short[1000];
            short num3 = 0;
            short num4 = 0;
            int num5 = 0;
            int num6 = 0;
            byte b = 0;
            byte[] array4 = new byte[13];
            ITile tile = null;

            ITileCollection tiles = Main.tile;
            for (int i = Y; i < Y + Height; i++)
            {
                for (int j = X; j < X + Width; j++)
                {
                    ITile tile2 = tiles[j - X, i - Y];
                    if (tile2.isTheSameAs(tile))
                    {
                        num4 += 1;
                    }
                    else
                    {
                        if (tile != null)
                        {
                            if (num4 > 0)
                            {
                                array4[num5] = (byte)(num4 & 255);
                                num5++;
                                if (num4 > 255)
                                {
                                    b |= 128;
                                    array4[num5] = (byte)(((int)num4 & 65280) >> 8);
                                    num5++;
                                }
                                else
                                {
                                    b |= 64;
                                }
                            }
                            array4[num6] = b;
                            BinaryWriter.Write(array4, num6, num5 - num6);
                            num4 = 0;
                        }
                        num5 = 3;
                        byte b3;
                        byte b2 = b = (b3 = 0);
                        if (tile2.active())
                        {
                            b |= 2;
                            array4[num5] = (byte)tile2.type;
                            num5++;
                            if (tile2.type > 255)
                            {
                                array4[num5] = (byte)(tile2.type >> 8);
                                num5++;
                                b |= 32;
                            }
                            if (tile2.type == 378 && tile2.frameX % 36 == 0 && tile2.frameY == 0)
                            {
                                int num15 = Terraria.GameContent.Tile_Entities.TETrainingDummy.Find(j, i);
                                if (num15 != -1)
                                {
                                    short[] array8 = array3;
                                    short num16 = num3;
                                    num3 = (short)(num16 + 1);
                                    array8[(int)num16] = (short)num15;
                                }
                            }
                            if (tile2.type == 395 && tile2.frameX % 36 == 0 && tile2.frameY == 0)
                            {
                                int num17 = Terraria.GameContent.Tile_Entities.TEItemFrame.Find(j, i);
                                if (num17 != -1)
                                {
                                    short[] array9 = array3;
                                    short num18 = num3;
                                    num3 = (short)(num18 + 1);
                                    array9[(int)num18] = (short)num17;
                                }
                            }
                            if (Main.tileFrameImportant[(int)tile2.type])
                            {
                                array4[num5] = (byte)(tile2.frameX & 255);
                                num5++;
                                array4[num5] = (byte)(((int)tile2.frameX & 65280) >> 8);
                                num5++;
                                array4[num5] = (byte)(tile2.frameY & 255);
                                num5++;
                                array4[num5] = (byte)(((int)tile2.frameY & 65280) >> 8);
                                num5++;
                            }
                            if (tile2.color() != 0)
                            {
                                b3 |= 8;
                                array4[num5] = tile2.color();
                                num5++;
                            }
                        }
                        if (tile2.wall != 0)
                        {
                            b |= 4;
                            array4[num5] = tile2.wall;
                            num5++;
                            if (tile2.wallColor() != 0)
                            {
                                b3 |= 16;
                                array4[num5] = tile2.wallColor();
                                num5++;
                            }
                        }
                        if (tile2.liquid != 0)
                        {
                            if (tile2.lava())
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
                            array4[num5] = tile2.liquid;
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
                        int num19;
                        if (tile2.halfBrick())
                        {
                            num19 = 16;
                        }
                        else if (tile2.slope() != 0)
                        {
                            num19 = (int)(tile2.slope() + 1) << 4;
                        }
                        else
                        {
                            num19 = 0;
                        }
                        b2 |= (byte)num19;
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
                        num6 = 2;
                        if (b3 != 0)
                        {
                            b2 |= 1;
                            array4[num6] = b3;
                            num6--;
                        }
                        if (b2 != 0)
                        {
                            b |= 1;
                            array4[num6] = b2;
                            num6--;
                        }
                        tile = tile2;
                    }
                }
            }
            if (num4 > 0)
            {
                array4[num5] = (byte)(num4 & 255);
                num5++;
                if (num4 > 255)
                {
                    b |= 128;
                    array4[num5] = (byte)(((int)num4 & 65280) >> 8);
                    num5++;
                }
                else
                {
                    b |= 64;
                }
            }
            array4[num6] = b;
            BinaryWriter.Write(array4, num6, num5 - num6);

            BinaryWriter.Write((short)0);
            BinaryWriter.Write((short)0);
            BinaryWriter.Write((short)0);
            /*
            BinaryWriter.Write((short)0); // Chests
            Dictionary<int, Sign> signs = FakeProvider.GetAppliedSigns(X, Y, Width, Height);
            BinaryWriter.Write((short)signs.Count);
            foreach (KeyValuePair<int, Sign> pair in signs)
            {
                Sign sign = pair.Value;
                BinaryWriter.Write((short)pair.Key);
                BinaryWriter.Write((short)sign.x);
                BinaryWriter.Write((short)sign.y);
                BinaryWriter.Write(sign.text);
            }

            BinaryWriter.Write(num3);
            for (int m = 0; m < (int)num3; m++)
            {
                Terraria.DataStructures.TileEntity.Write(BinaryWriter, Terraria.DataStructures.TileEntity.ByID[(int)array3[m]], false);
            }
            */
        }

        private static void CompressTileBlock_Inner2(BinaryWriter BinaryWriter,
            int X, int Y, int Width, int Height)
        {
            short num4 = 0;
            int num5 = 0;
            int num6 = 0;
            byte b = 0;
            byte[] array4 = new byte[13];
            ITile tile = null;
            for (int i = Y; i < Y + Height; i++)
            {
                for (int j = X; j < X + Width; j++)
                {
                    ITile tile2 = Main.tile[j, i];
                    if (tile2.isTheSameAs(tile))
                    {
                        num4 += 1;
                    }
                    else
                    {
                        if (tile != null)
                        {
                            if (num4 > 0)
                            {
                                array4[num5] = (byte)(num4 & 255);
                                num5++;
                                if (num4 > 255)
                                {
                                    b |= 128;
                                    array4[num5] = (byte)(((int)num4 & 65280) >> 8);
                                    num5++;
                                }
                                else
                                {
                                    b |= 64;
                                }
                            }
                            array4[num6] = b;
                            BinaryWriter.Write(array4, num6, num5 - num6);
                            num4 = 0;
                        }
                        num5 = 3;
                        byte b3;
                        byte b2 = b = (b3 = 0);
                        if (tile2.active())
                        {
                            b |= 2;
                            array4[num5] = (byte)tile2.type;
                            num5++;
                            if (tile2.type > 255)
                            {
                                array4[num5] = (byte)(tile2.type >> 8);
                                num5++;
                                b |= 32;
                            }
                            if (Main.tileFrameImportant[(int)tile2.type])
                            {
                                array4[num5] = (byte)(tile2.frameX & 255);
                                num5++;
                                array4[num5] = (byte)(((int)tile2.frameX & 65280) >> 8);
                                num5++;
                                array4[num5] = (byte)(tile2.frameY & 255);
                                num5++;
                                array4[num5] = (byte)(((int)tile2.frameY & 65280) >> 8);
                                num5++;
                            }
                            if (tile2.color() != 0)
                            {
                                b3 |= 8;
                                array4[num5] = tile2.color();
                                num5++;
                            }
                        }
                        if (tile2.wall != 0)
                        {
                            b |= 4;
                            array4[num5] = tile2.wall;
                            num5++;
                            if (tile2.wallColor() != 0)
                            {
                                b3 |= 16;
                                array4[num5] = tile2.wallColor();
                                num5++;
                            }
                        }
                        if (tile2.liquid != 0)
                        {
                            if (tile2.lava())
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
                            array4[num5] = tile2.liquid;
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
                        int num19;
                        if (tile2.halfBrick())
                        {
                            num19 = 16;
                        }
                        else if (tile2.slope() != 0)
                        {
                            num19 = (int)(tile2.slope() + 1) << 4;
                        }
                        else
                        {
                            num19 = 0;
                        }
                        b2 |= (byte)num19;
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
                        num6 = 2;
                        if (b3 != 0)
                        {
                            b2 |= 1;
                            array4[num6] = b3;
                            num6--;
                        }
                        if (b2 != 0)
                        {
                            b |= 1;
                            array4[num6] = b2;
                            num6--;
                        }
                        tile = tile2;
                    }
                }
            }
            if (num4 > 0)
            {
                array4[num5] = (byte)(num4 & 255);
                num5++;
                if (num4 > 255)
                {
                    b |= 128;
                    array4[num5] = (byte)(((int)num4 & 65280) >> 8);
                    num5++;
                }
                else
                {
                    b |= 64;
                }
            }
            array4[num6] = b;
            BinaryWriter.Write(array4, num6, num5 - num6);
            /*BinaryWriter.Write(num);
            for (int k = 0; k < (int)num; k++)
            {
                Chest chest = Main.chest[(int)array[k]];
                BinaryWriter.Write(array[k]);
                BinaryWriter.Write((short)chest.x);
                BinaryWriter.Write((short)chest.y);
                BinaryWriter.Write(chest.name);
            }
            BinaryWriter.Write(num2);
            for (int l = 0; l < (int)num2; l++)
            {
                Sign sign = Main.sign[(int)array2[l]];
                BinaryWriter.Write(array2[l]);
                BinaryWriter.Write((short)sign.x);
                BinaryWriter.Write((short)sign.y);
                BinaryWriter.Write(sign.text);
            }
            BinaryWriter.Write(num3);
            for (int m = 0; m < (int)num3; m++)
            {
                TileEntity.Write(BinaryWriter, TileEntity.ByID[(int)array3[m]], false);
            }*/
            BinaryWriter.Write((short)0);
            BinaryWriter.Write((short)0);
            BinaryWriter.Write((short)0);
        }

        private static void CompressTileBlock_Inner3(BinaryWriter BinaryWriter,
            int X, int Y, int Width, int Height)
        {
            short[] array = new short[1000];
            short[] array2 = new short[1000];
            short[] array3 = new short[1000];
            short num = 0;
            short num2 = 0;
            short num3 = 0;
            short num4 = 0;
            int num5 = 0;
            int num6 = 0;
            byte b = 0;
            byte[] array4 = new byte[13];
            ITile tile = null;
            for (int i = Y; i < Y + Height; i++)
            {
                for (int j = X; j < X + Width; j++)
                {
                    ITile tile2 = Main.tile[j, i];
                    if (tile2.isTheSameAs(tile))
                    {
                        num4 += 1;
                    }
                    else
                    {
                        if (tile != null)
                        {
                            if (num4 > 0)
                            {
                                array4[num5] = (byte)(num4 & 255);
                                num5++;
                                if (num4 > 255)
                                {
                                    b |= 128;
                                    array4[num5] = (byte)(((int)num4 & 65280) >> 8);
                                    num5++;
                                }
                                else
                                {
                                    b |= 64;
                                }
                            }
                            array4[num6] = b;
                            BinaryWriter.Write(array4, num6, num5 - num6);
                            num4 = 0;
                        }
                        num5 = 3;
                        byte b3;
                        byte b2 = b = (b3 = 0);
                        if (tile2.active())
                        {
                            b |= 2;
                            array4[num5] = (byte)tile2.type;
                            num5++;
                            if (tile2.type > 255)
                            {
                                array4[num5] = (byte)(tile2.type >> 8);
                                num5++;
                                b |= 32;
                            }
                            if (TileID.Sets.BasicChest[(int)tile2.type] && tile2.frameX % 36 == 0 && tile2.frameY % 36 == 0)
                            {
                                short num7 = (short)Chest.FindChest(j, i);
                                if (num7 != -1)
                                {
                                    array[(int)num] = num7;
                                    num += 1;
                                }
                            }
                            if (tile2.type == 88 && tile2.frameX % 54 == 0 && tile2.frameY % 36 == 0)
                            {
                                short num8 = (short)Chest.FindChest(j, i);
                                if (num8 != -1)
                                {
                                    array[(int)num] = num8;
                                    num += 1;
                                }
                            }
                            if (tile2.type == 85 && tile2.frameX % 36 == 0 && tile2.frameY % 36 == 0)
                            {
                                short num9 = (short)Sign.ReadSign(j, i, true);
                                if (num9 != -1)
                                {
                                    short[] array5 = array2;
                                    short num10 = num2;
                                    num2 = (short)(num10 + 1);
                                    array5[(int)num10] = num9;
                                }
                            }
                            if (tile2.type == 55 && tile2.frameX % 36 == 0 && tile2.frameY % 36 == 0)
                            {
                                short num11 = (short)Sign.ReadSign(j, i, true);
                                if (num11 != -1)
                                {
                                    short[] array6 = array2;
                                    short num12 = num2;
                                    num2 = (short)(num12 + 1);
                                    array6[(int)num12] = num11;
                                }
                            }
                            if (tile2.type == 425 && tile2.frameX % 36 == 0 && tile2.frameY % 36 == 0)
                            {
                                short num13 = (short)Sign.ReadSign(j, i, true);
                                if (num13 != -1)
                                {
                                    short[] array7 = array2;
                                    short num14 = num2;
                                    num2 = (short)(num14 + 1);
                                    array7[(int)num14] = num13;
                                }
                            }
                            if (tile2.type == 378 && tile2.frameX % 36 == 0 && tile2.frameY == 0)
                            {
                                int num15 = TETrainingDummy.Find(j, i);
                                if (num15 != -1)
                                {
                                    short[] array8 = array3;
                                    short num16 = num3;
                                    num3 = (short)(num16 + 1);
                                    array8[(int)num16] = (short)num15;
                                }
                            }
                            if (tile2.type == 395 && tile2.frameX % 36 == 0 && tile2.frameY == 0)
                            {
                                int num17 = TEItemFrame.Find(j, i);
                                if (num17 != -1)
                                {
                                    short[] array9 = array3;
                                    short num18 = num3;
                                    num3 = (short)(num18 + 1);
                                    array9[(int)num18] = (short)num17;
                                }
                            }
                            if (Main.tileFrameImportant[(int)tile2.type])
                            {
                                array4[num5] = (byte)(tile2.frameX & 255);
                                num5++;
                                array4[num5] = (byte)(((int)tile2.frameX & 65280) >> 8);
                                num5++;
                                array4[num5] = (byte)(tile2.frameY & 255);
                                num5++;
                                array4[num5] = (byte)(((int)tile2.frameY & 65280) >> 8);
                                num5++;
                            }
                            if (tile2.color() != 0)
                            {
                                b3 |= 8;
                                array4[num5] = tile2.color();
                                num5++;
                            }
                        }
                        if (tile2.wall != 0)
                        {
                            b |= 4;
                            array4[num5] = tile2.wall;
                            num5++;
                            if (tile2.wallColor() != 0)
                            {
                                b3 |= 16;
                                array4[num5] = tile2.wallColor();
                                num5++;
                            }
                        }
                        if (tile2.liquid != 0)
                        {
                            if (tile2.lava())
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
                            array4[num5] = tile2.liquid;
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
                        int num19;
                        if (tile2.halfBrick())
                        {
                            num19 = 16;
                        }
                        else if (tile2.slope() != 0)
                        {
                            num19 = (int)(tile2.slope() + 1) << 4;
                        }
                        else
                        {
                            num19 = 0;
                        }
                        b2 |= (byte)num19;
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
                        num6 = 2;
                        if (b3 != 0)
                        {
                            b2 |= 1;
                            array4[num6] = b3;
                            num6--;
                        }
                        if (b2 != 0)
                        {
                            b |= 1;
                            array4[num6] = b2;
                            num6--;
                        }
                        tile = tile2;
                    }
                }
            }
            if (num4 > 0)
            {
                array4[num5] = (byte)(num4 & 255);
                num5++;
                if (num4 > 255)
                {
                    b |= 128;
                    array4[num5] = (byte)(((int)num4 & 65280) >> 8);
                    num5++;
                }
                else
                {
                    b |= 64;
                }
            }
            array4[num6] = b;
            BinaryWriter.Write(array4, num6, num5 - num6);
            BinaryWriter.Write(num);
            for (int k = 0; k < (int)num; k++)
            {
                Chest chest = Main.chest[(int)array[k]];
                BinaryWriter.Write(array[k]);
                BinaryWriter.Write((short)chest.x);
                BinaryWriter.Write((short)chest.y);
                BinaryWriter.Write(chest.name);
            }
            BinaryWriter.Write(num2);
            for (int l = 0; l < (int)num2; l++)
            {
                Sign sign = Main.sign[(int)array2[l]];
                BinaryWriter.Write(array2[l]);
                BinaryWriter.Write((short)sign.x);
                BinaryWriter.Write((short)sign.y);
                BinaryWriter.Write(sign.text);
            }
            BinaryWriter.Write(num3);
            for (int m = 0; m < (int)num3; m++)
            {
                TileEntity.Write(BinaryWriter, TileEntity.ByID[(int)array3[m]], false);
            }
        }

        #endregion
    }
}