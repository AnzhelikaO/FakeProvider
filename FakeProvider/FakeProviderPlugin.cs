#region Using
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.IO;
using Terraria.Net.Sockets;
using Terraria.Utilities;
using TerrariaApi.Server;
using TShockAPI;
#endregion
namespace FakeProvider
{
	[ApiVersion(2, 1)]
    public class FakeProviderPlugin : TerrariaPlugin
    {
		[DllImport("User32.dll", CharSet = CharSet.Unicode)]
		public static extern int MessageBox(IntPtr h, string m, string c, int type);

		#region Data

		public override string Name => "FakeProvider";
        public override string Author => "ASgo and Anzhelika";
        public override string Description => "TODO";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        internal static int[] AllPlayers;
        internal static Func<RemoteClient, byte[], int, int, bool> NetSendBytes;
		public static string Debug;

        public static bool CustomWorldLoad { get; private set; }

        internal static List<TileProvider> ProvidersToAdd = new List<TileProvider>();
        internal static bool ProvidersLoaded = false;
		private static FieldInfo StatusTextField;
        public static Command[] CommandList = new Command[]
        {
            new Command("FakeProvider.Control", FakeCommand, "fake"),
			new Command("FakeProvider.Control", PersonalFakeCommand, "pfake", "personalfake", "persfake")
        };

        #endregion

        #region Constructor

        public FakeProviderPlugin(Main game) : base(game)
        {
            Order = -1002; // TUI has -1000
            string[] args = Environment.GetCommandLineArgs();

			// WARNING: has not been heavily tested
			CustomWorldLoad = args.Any(x => (x.ToLower() == "-customworldload"));

			StatusTextField = typeof(Main).GetField("statusText", BindingFlags.NonPublic | BindingFlags.Static);
		}

        #endregion
        #region Initialize

        public override void Initialize()
        {
            NetSendBytes = (Func<RemoteClient, byte[], int, int, bool>)Delegate.CreateDelegate(
                typeof(Func<RemoteClient, byte[], int, int, bool>),
                ServerApi.Hooks,
                ServerApi.Hooks.GetType().GetMethod("InvokeNetSendBytes", BindingFlags.NonPublic | BindingFlags.Instance));

            AllPlayers = new int[Main.maxPlayers];
            for (int i = 0; i < Main.maxPlayers; i++)
                AllPlayers[i] = i;

			On.Terraria.IO.WorldFile.LoadWorld += OnLoadWorld;
			On.Terraria.IO.WorldFile.SaveWorld_bool_bool += OnSaveWorld;
            ServerApi.Hooks.NetSendData.Register(this, OnSendData, Int32.MaxValue);
            ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);

            Commands.ChatCommands.AddRange(CommandList);
        }

		#endregion
		#region Dispose

		protected override void Dispose(bool Disposing)
        {
            if (Disposing)
            {
				On.Terraria.IO.WorldFile.LoadWorld -= OnLoadWorld;
				On.Terraria.IO.WorldFile.SaveWorld_bool_bool -= OnSaveWorld;
				ServerApi.Hooks.NetSendData.Deregister(this, OnSendData);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnServerLeave);
            }
            base.Dispose(Disposing);
        }

        #endregion
        #region OnSaveWorld

        private void OnSaveWorld(On.Terraria.IO.WorldFile.orig_SaveWorld_bool_bool orig, bool Cloud, bool ResetTime)
        {
            if (FakeProviderAPI.World == null)
			{
				orig(Cloud, ResetTime);
				return;
			}

			try
            {
				List<TileProvider> disabled = FakeProviderAPI.Tile.Disable();

				orig(Cloud, ResetTime);

                FakeProviderAPI.Tile.Enable(disabled);

                Console.WriteLine("[FakeProvier] World saved.");
			}
			catch (Exception e)
            {
				TShock.Log.ConsoleError(e.ToString());
				throw;
            }
        }

        #endregion
        #region OnLoadWorld

        private void OnLoadWorld(On.Terraria.IO.WorldFile.orig_LoadWorld orig, bool loadFromCloud)
        {
			try
            {
                Dictionary<string, string> args = Terraria.Utils.ParseArguements(Environment.GetCommandLineArgs());
				if (!args.TryGetValue("-autocreate", out string worldSize) || worldSize == "0")
                {
                    ReadWorldSize(loadFromCloud);
                }
				if (args.TryGetValue("-customworldload", out string stage) && int.TryParse(stage, out int loadStages))
                {
					FakeGen.FakeGenManager.Stages = (FakeGen.LoadStage)loadStages;
				}
				CreateCustomTileProvider();

				System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
				sw.Start();
				if (CustomWorldLoad)
					FakeGen.FakeGenManager.CustomLoadWorld(loadFromCloud, this);
				else
					orig(loadFromCloud);
				sw.Stop();
				ServerApi.LogWriter.PluginWriteLine(this, $"The world loaded in {sw.Elapsed}.",
					System.Diagnostics.TraceLevel.Info);

				lock (ProvidersToAdd)
				{
					ProvidersLoaded = true;

					FakeProviderAPI.Tile.Add(FakeProviderAPI.Tile.Void);
					ProvidersToAdd.Remove(FakeProviderAPI.Tile.Void);

					FakeProviderAPI.Tile.Add(FakeProviderAPI.World);
					ProvidersToAdd.Remove(FakeProviderAPI.World);
					FakeProviderAPI.World.ScanEntities();

					foreach (TileProvider provider in ProvidersToAdd)
						FakeProviderAPI.Tile.Add(provider);
					ProvidersToAdd.Clear();
				}

				Main.tile = FakeProviderAPI.Tile;

				GC.Collect();
			}
			catch (Exception e)
			{
				TShock.Log.ConsoleError(e.ToString());
				throw;
			}
        }

        #endregion

		#region OnSendData

		private static void OnSendData(SendDataEventArgs args)
        {
            if (args.Handled)
                return;

            switch (args.MsgId)
            {
                case PacketTypes.TileSendSection:
                    args.Handled = true;
                    // We allow sending packet to custom list of players by specifying it in text parameter
                    if (args.text?._text?.Length > 0)
                        SendSectionPacket.Send(args.text._text.Select(c => (int)c), args.ignoreClient,
                            args.number, (int)args.number2, (short)args.number3, (short)args.number4);
                    else
                        SendSectionPacket.Send(args.remoteClient, args.ignoreClient,
                            args.number, (int)args.number2, (short)args.number3, (short)args.number4);
                    break;
                case PacketTypes.TileFrameSection:
					args.Handled = true;
					// We allow sending packet to custom list of players by specifying it in text parameter
					if (args.text?._text?.Length > 0)
						FrameSectionPacket.Send(args.text._text.Select(c => (int)c), args.ignoreClient,
							(short)args.number, (short)args.number2, (short)args.number3, (short)args.number4);
					else
						FrameSectionPacket.Send(args.remoteClient, args.ignoreClient,
							(short)args.number, (short)args.number2, (short)args.number3, (short)args.number4);
					break;
                case PacketTypes.TileSendSquare:
                    args.Handled = true;
					// We allow sending packet to custom list of players by specifying it in text parameter
					if (args.text?._text?.Length > 0)
                        SendTileSquarePacket.Send(args.text._text.Select(c => (int)c), args.ignoreClient,
                            (int)args.number3, (int)args.number4, (int)args.number, (int)args.number2, args.number5);
                    else
                        SendTileSquarePacket.Send(args.remoteClient, args.ignoreClient,
                            (int)args.number3, (int)args.number4, (int)args.number, (int)args.number2, args.number5);
                    break;
            }
        }

        #endregion
        #region OnServerLeave

        private static void OnServerLeave(LeaveEventArgs args)
        {
            //FakeProviderAPI.Personal.All(provider => provider.Observers.re)
        }

		#endregion

		#region SendTo

		internal static void SendTo(IEnumerable<RemoteClient> clients, byte[] data)
		{
			foreach (RemoteClient client in clients)
				try
				{

					if (NetSendBytes(client, data, 0, data.Length))
						return;

					client.Socket.AsyncSend(data, 0, data.Length,
						new SocketSendCallback(client.ServerWriteCallBack), null);
				}
				catch (IOException) { }
				catch (ObjectDisposedException) { }
				catch (InvalidOperationException) { }
		}

        #endregion
        #region FindProvider

		public static bool FindProvider(string name, TSPlayer player, out TileProvider provider, bool includeGlobal = true, bool includePersonal = false)
        {
			provider = null;
			var foundProviders = FakeProviderAPI.FindProvider(name, includeGlobal, includePersonal);

			if (foundProviders.Count() == 0)
			{
				player?.SendErrorMessage("Invalid provider '" + name + "'");
				return false;
			}
			if (foundProviders.Count() > 1)
			{
				player?.SendMultipleMatchError(foundProviders);
				return false;
			}
			provider = foundProviders.First();
			return true;
		}

        #endregion

        #region FakeCommand

        public static void FakeCommand(CommandArgs args)
        {
            string arg0 = args.Parameters.ElementAtOrDefault(0);
            switch (arg0?.ToLower())
            {
                case "l":
                case "list":
                {
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int page))
                        return;

					List<string> lines = PaginationTools.BuildLinesFromTerms(FakeProviderAPI.Tile.Global);
					PaginationTools.SendPage(args.Player, page, lines, new PaginationTools.Settings()
                    {
                        HeaderFormat = "Fake providers ({0}/{1}):",
                        FooterFormat = "Type '/fake list {0}' for more.",
                        NothingToDisplayString = "There are no fake providers yet."
                    });
                    break;
                }
                case "tp":
                case "teleport":
                {
                    if (args.Parameters.Count != 2)
                    {
                        args.Player.SendErrorMessage("/fake tp \"provider name\"");
                        return;
                    }
					if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider))
						return;

                    args.Player.Teleport((provider.X + provider.Width / 2) * 16,
                        (provider.Y + provider.Height / 2) * 16);
                    args.Player.SendSuccessMessage($"Teleported to fake provider '{provider.Name}'.");
                    break;
                }
                case "m":
                case "move":
                {
                    if (args.Parameters.Count != 4)
                    {
                        args.Player.SendErrorMessage("/fake move \"provider name\" <relative x> <relative y>");
                        return;
                    }
                    if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider))
						return;

                    if (!Int32.TryParse(args.Parameters[2], out int x)
                        || !Int32.TryParse(args.Parameters[3], out int y))
                    {
                        args.Player.SendErrorMessage("Invalid coordinates.");
                        return;
                    }

                    provider.Move(x, y, true);
                    args.Player.SendSuccessMessage($"Fake provider '{provider.Name}' moved to ({x}, {y}).");
                    break;
                }
                case "la":
                case "layer":
                {
                    if (args.Parameters.Count != 3)
                    {
                        args.Player.SendErrorMessage("/fake layer \"provider name\" <layer>");
                        return;
                    }
                    if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider))
						return;

                    if (!Int32.TryParse(args.Parameters[2], out int layer))
                    {
                        args.Player.SendErrorMessage("Invalid layer.");
                        return;
                    }

                    provider.SetLayer(layer, true);
                    args.Player.SendSuccessMessage($"Fake provider '{provider.Name}' layer set to {layer}.");
                    break;
                }
                case "i":
                case "info":
                {
                    if (args.Parameters.Count != 2)
                    {
                        args.Player.SendErrorMessage("/fake info \"provider name\"");
                        return;
                    }
                    if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider))
						return;

                    args.Player.SendInfoMessage(
$@"Fake provider '{provider.Name}' ({provider.GetType().Name})
Position and size: {provider.XYWH()}
Enabled: {provider.Enabled}
Entities: {provider.Entities.Count}");
                    break;
				}
				case "d":
				case "disable":
				{
					if (args.Parameters.Count != 2)
					{
						args.Player.SendErrorMessage("/fake disable \"provider name\"");
						return;
					}
					if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider))
						return;

					provider.Disable();
					break;
				}
				case "e":
				case "enable":
				{
					if (args.Parameters.Count != 2)
					{
						args.Player.SendErrorMessage("/fake enable \"provider name\"");
						return;
					}
					if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider))
						return;

					provider.Enable();
					break;
				}
				default:
                {
                    args.Player.SendSuccessMessage("/fake subcommands:");
                    args.Player.SendInfoMessage(
@"/fake info ""provider name""
/fake tp ""provider name""
/fake move ""provider name"" <relative x> <relative y>
/fake layer ""provider name"" <layer>
/fake disable ""provider name""
/fake enable ""provider name""
/fake list [page]");
                    break;
                }
            }
        }

		#endregion
		#region PersonalFakeCommand
		public static void PersonalFakeCommand(CommandArgs args)
		{
			string arg0 = args.Parameters.ElementAtOrDefault(0);
			switch (arg0?.ToLower())
			{
				case "l":
				case "list":
					{
						bool allPersonalProviders = args.Parameters.RemoveAll(s => s == "all") > 0;
						if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int page))
							return;

						List<string> lines = null;
						if (allPersonalProviders)
							lines = PaginationTools.BuildLinesFromTerms(FakeProviderAPI.Tile.Personal);
						else
							lines = PaginationTools.BuildLinesFromTerms(FakeProviderAPI.Tile.Personal.Where(provider => provider.Observers.Contains(args.Player.Index)));

						PaginationTools.SendPage(args.Player, page, lines, new PaginationTools.Settings()
						{
							HeaderFormat = "Fake providers ({0}/{1}):",
							FooterFormat = "Type '/pfake list {0}' for more.",
							NothingToDisplayString = "There are no personal fake providers yet."
						});
						break;
					}
				case "tp":
				case "teleport":
					{
						if (args.Parameters.Count != 2)
						{
							args.Player.SendErrorMessage("/fake tp \"provider name\"");
							return;
						}
						if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider, false, true))
							return;

						args.Player.Teleport((provider.X + provider.Width / 2) * 16,
							(provider.Y + provider.Height / 2) * 16);
						args.Player.SendSuccessMessage($"Teleported to fake provider '{provider.Name}'.");
						break;
					}
				case "m":
				case "move":
					{
						if (args.Parameters.Count != 4)
						{
							args.Player.SendErrorMessage("/fake move \"provider name\" <relative x> <relative y>");
							return;
						}
						if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider, false, true))
							return;

						if (!Int32.TryParse(args.Parameters[2], out int x)
							|| !Int32.TryParse(args.Parameters[3], out int y))
						{
							args.Player.SendErrorMessage("Invalid coordinates.");
							return;
						}

						provider.Move(x, y, true);
						args.Player.SendSuccessMessage($"Fake provider '{provider.Name}' moved to ({x}, {y}).");
						break;
					}
				case "la":
				case "layer":
					{
						if (args.Parameters.Count != 3)
						{
							args.Player.SendErrorMessage("/fake layer \"provider name\" <layer>");
							return;
						}
						if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider, false, true))
							return;

						if (!Int32.TryParse(args.Parameters[2], out int layer))
						{
							args.Player.SendErrorMessage("Invalid layer.");
							return;
						}

						provider.SetLayer(layer, true);
						args.Player.SendSuccessMessage($"Fake provider '{provider.Name}' layer set to {layer}.");
						break;
					}
				case "i":
				case "info":
					{
						if (args.Parameters.Count != 2)
						{
							args.Player.SendErrorMessage("/fake info \"provider name\"");
							return;
						}
						if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider, false, true))
							return;

						args.Player.SendInfoMessage(
	$@"Fake provider '{provider.Name}' ({provider.GetType().Name})
Position and size: {provider.XYWH()}
Enabled: {provider.Enabled}
Entities: {provider.Entities.Count}");
						break;
					}
				case "d":
				case "disable":
					{
						if (args.Parameters.Count != 2)
						{
							args.Player.SendErrorMessage("/fake disable \"provider name\"");
							return;
						}
						if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider, false, true))
							return;

						provider.Disable();
						break;
					}
				case "e":
				case "enable":
					{
						if (args.Parameters.Count != 2)
						{
							args.Player.SendErrorMessage("/fake enable \"provider name\"");
							return;
						}
						if (!FindProvider(args.Parameters[1], args.Player, out TileProvider provider, false, true))
							return;

						provider.Enable();
						break;
					}
				default:
					{
						args.Player.SendSuccessMessage("/fake subcommands:");
						args.Player.SendInfoMessage(
	@"/pfake info ""provider name""
/pfake tp ""provider name""
/pfake move ""provider name"" <relative x> <relative y>
/pfake layer ""provider name"" <layer>
/pfake disable ""provider name""
/pfake enable ""provider name""
/pfake list [page]");
						break;
					}
			}
		}
		#endregion

		#region ReadWorldSize

		private void ReadWorldSize(bool cloud)
		{
			using (MemoryStream memoryStream = new MemoryStream(FileUtilities.ReadAllBytes(Main.worldPathName, cloud)))
			using (BinaryReader binaryReader = new BinaryReader(memoryStream))
            {
				int version = binaryReader.ReadInt32();
				WorldFile._versionNumber = version;
				memoryStream.Position = 0L;
				TShock.Log.Error($"version: ${version}");
				if (!WorldFile.LoadFileFormatHeader(binaryReader, out _, out int[] array))
					throw new IOException("Invalid world file format 1");
				if (binaryReader.BaseStream.Position != (long)array[0])
                    throw new IOException("Invalid world file format 2");

                WorldFile.LoadHeader(binaryReader);
            }
		}

        #endregion
        #region CreateCustomTileProvider

        private static void CreateCustomTileProvider()
        {
            FakeProviderAPI.Tile = new TileProviderCollection();
            FakeProviderAPI.Tile.Initialize(Main.maxTilesX, Main.maxTilesY);

            FakeProviderAPI.World = FakeProviderAPI.CreateTileProvider(FakeProviderAPI.WorldProviderName, 0, 0,
                Main.maxTilesX, Main.maxTilesY, Int32.MinValue + 1);

			if (Main.tile is IDisposable previous)
				previous.Dispose();
			Main.tile = FakeProviderAPI.World;
        }

        #endregion
	}
}
