#region Using
using OTAPI.Tile;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Net.Sockets;
#endregion
namespace FakeProvider
{
    class SendTileSquarePacket
    {
        #region Send

        public static void Send(int Who, int IgnoreIndex,
                int Width, int Height, int X, int Y, int TileChangeType = 0) =>
            Send(((Who == -1) ? FakeProviderPlugin.AllPlayers : new int[] { Who }),
                IgnoreIndex, Width, Height, X, Y, TileChangeType);

        public static void Send(IEnumerable<int> Who, int IgnoreIndex,
            int Width, int Height, int X, int Y, int TileChangeType = 0)
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
                if (NetMessage.buffer[i].broadcast && client.IsConnected() && client.SectionRange(Width, X, Y))
                    clients.Add(client);
            }
            if (clients.Count == 0)
                return;

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.BaseStream.Position = 2L;
                bw.Write((byte)PacketTypes.TileSendSquare);
                WriteTiles(bw, X, Y, Width, Height, TileChangeType);
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
        #region WriteTiles

        /// <param name="number">Size</param>
        /// <param name="number2">X</param>
        /// <param name="number3">Y</param>
        /// <param name="number5">TileChangeType</param>
        private static void WriteTiles(BinaryWriter binaryWriter,
            int number, int number2, int number3, int number4, int number5 = 0)
		{
			int num4 = number;
			int num5 = (int)number2;
			int num6 = (int)number3;
			if (num6 < 0)
			{
				num6 = 0;
			}
			int num7 = (int)number4;
			if (num7 < 0)
			{
				num7 = 0;
			}
			if (num4 < num6)
			{
				num4 = num6;
			}
			if (num4 >= Main.maxTilesX + num6)
			{
				num4 = Main.maxTilesX - num6 - 1;
			}
			if (num5 < num7)
			{
				num5 = num7;
			}
			if (num5 >= Main.maxTilesY + num7)
			{
				num5 = Main.maxTilesY - num7 - 1;
			}
			binaryWriter.Write((short)num4);
			binaryWriter.Write((short)num5);
			binaryWriter.Write((byte)num6);
			binaryWriter.Write((byte)num7);
			binaryWriter.Write((byte)number5);
			for (int num8 = num4; num8 < num4 + num6; num8++)
			{
				for (int num9 = num5; num9 < num5 + num7; num9++)
				{
					BitsByte bb17 = 0;
					BitsByte bb18 = 0;
					byte b = 0;
					byte b2 = 0;
					ITile tile = Main.tile[num8, num9];
					bb17[0] = tile.active();
					bb17[2] = (tile.wall > 0);
					bb17[3] = (tile.liquid > 0 && Main.netMode == 2);
					bb17[4] = tile.wire();
					bb17[5] = tile.halfBrick();
					bb17[6] = tile.actuator();
					bb17[7] = tile.inActive();
					bb18[0] = tile.wire2();
					bb18[1] = tile.wire3();
					if (tile.active())// && tile.color() > 0) // Allow clearing paint
					{
						bb18[2] = true;
						b = tile.color();
					}
					if (tile.wall > 0)// && tile.wallColor() > 0) // Allow clearing paint
					{
						bb18[3] = true;
						b2 = tile.wallColor();
					}
					bb18 += (byte)(tile.slope() << 4);
					bb18[7] = tile.wire4();
					binaryWriter.Write(bb17);
					binaryWriter.Write(bb18);
					//if (b > 0) // Allow clearing paint
					if (tile.active()) // Allow clearing paint
					{
						binaryWriter.Write(b);
					}
					//if (b2 > 0) // Allow clearing paint
					if (tile.wall > 0) // Allow clearing paint
					{
						binaryWriter.Write(b2);
					}
					if (tile.active())
					{
						binaryWriter.Write(tile.type);
						if (Main.tileFrameImportant[(int)tile.type])
						{
							binaryWriter.Write(tile.frameX);
							binaryWriter.Write(tile.frameY);
						}
					}
					if (tile.wall > 0)
					{
						binaryWriter.Write(tile.wall);
					}
					if (tile.liquid > 0 && Main.netMode == 2)
					{
						binaryWriter.Write(tile.liquid);
						binaryWriter.Write(tile.liquidType());
					}
				}
			}
		}

        #endregion
    }
}
