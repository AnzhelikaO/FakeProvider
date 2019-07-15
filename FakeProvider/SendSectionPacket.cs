#region Using
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Terraria;
using Terraria.Net.Sockets;
#endregion
namespace FakeProvider
{
    class SendSectionPacket
    {
        #region Send

        public static void Send(int Who, int IgnoreIndex,
                int X, int Y, short Width, short Height) =>
            Send(((Who == -1) ? FakeProvider.AllPlayers : new int[] { Who }),
                IgnoreIndex, X, Y, Width, Height);

        public static void Send(IEnumerable<int> Who, int IgnoreIndex,
            int X, int Y, short Width, short Height)
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
                if (client?.IsActive == true)
                    clients.Add(client);
            }
            if (clients.Count == 0)
                return;

            byte[] data;
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
                    client.Socket.AsyncSend(data, 0, data.Length,
                        new SocketSendCallback(client.ServerWriteCallBack), null);
                }
                catch (IOException) { }
                catch (ObjectDisposedException) { }
        }

        #endregion
        #region CompressTileBlock

        private static int CompressTileBlock(int X, int Y,
            short Width, short Height, BinaryWriter BinaryWriter)
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
                bw.Write(Width);
                bw.Write(Height);
                CompressTileBlock_Inner(bw, X, Y, Width, Height);
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
            OTAPI.Tile.ITile tile = null;

            OTAPI.Tile.ITile[,] tiles = FakeProvider.GetAppliedTiles(X, Y, Width, Height);
            for (int i = Y; i < Y + Height; i++)
            {
                for (int j = X; j < X + Width; j++)
                {
                    OTAPI.Tile.ITile tile2 = tiles[j - X, i - Y];
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
        }

        #endregion
    }
}