#region Using
using System.IO;
using Terraria;
using Terraria.Net.Sockets;
#endregion
namespace FakeProvider
{
    class SendChestOpenPacket
    {
        public static void Send(int Who, short ChestID, int X, int Y)
        {
            RemoteClient Client = Netplay.Clients[Who];
            if (Client?.IsActive != true)
                return;

            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((short)10);
                bw.Write((byte)PacketTypes.ChestOpen);
                bw.Write((short)ChestID);
                bw.Write((short)X);
                bw.Write((short)Y);
                bw.Write((byte)Who);
                byte[] data = ms.ToArray();
                Client.Socket.AsyncSend(data, 0, data.Length,
                    new SocketSendCallback(Client.ServerWriteCallBack), null);
            }
        }
    }

    class SendChestItemPacket
    {
        public static void SendMany(int PlayerIndex, short ChestID, Item[] Item)
        {
            RemoteClient client = Netplay.Clients[PlayerIndex];
            if (client?.IsActive != true)
                return;
            for (int i = 0; i < Item.Length; i++)
            {
                Item item = Item[i];
                Send(client, ChestID, i, item.stack, item.prefix, item.netID);
            }
        }

        public static void Send(RemoteClient Client, short ChestID,
            int Slot, int Stack, int Prefix, int NetID)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((short)11);
                bw.Write((byte)PacketTypes.ChestItem);
                bw.Write((short)ChestID);
                bw.Write((byte)Slot);
                bw.Write((short)Stack);
                bw.Write((byte)Prefix);
                bw.Write((short)NetID);
                byte[] data = ms.ToArray();
                Client.Socket.AsyncSend(data, 0, data.Length,
                    new SocketSendCallback(Client.ServerWriteCallBack), null);
            }
        }
    }
}