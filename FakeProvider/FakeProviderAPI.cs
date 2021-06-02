using OTAPI.Tile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TShockAPI;

namespace FakeProvider
{
    public static class FakeProviderAPI
    {
        #region Data

        public const string WorldProviderName = "__world__";
        public static TileProviderCollection Tile { get; internal set; }
        public static INamedTileCollection World { get; internal set; }
        private static ObserversEqualityComparer OEC = new ObserversEqualityComparer();

        #endregion

        #region CreateTileProvider

        public static INamedTileCollection CreateTileProvider(string Name, int X, int Y, int Width, int Height, int Layer = 0)
        {
            Type newType = Helper.CreateType();
            Type tileProviderType = typeof(TileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(tileProviderType, true);
            ((dynamic)result).Initialize(Name, X, Y, Width, Height, Layer);
            typeof(TileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            lock (FakeProviderPlugin.ProvidersToAdd)
            {
                if (FakeProviderPlugin.ProvidersLoaded)
                    Tile.Add(result);
                else
                    FakeProviderPlugin.ProvidersToAdd.Add(result);
            }
            return result;
        }

        public static INamedTileCollection CreateTileProvider(string Name, int X, int Y, int Width, int Height, ITileCollection CopyFrom, int Layer = 0)
        {
            Type newType = Helper.CreateType();
            Type tileProviderType = typeof(TileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(tileProviderType, true);
            ((dynamic)result).Initialize(Name, X, Y, Width, Height, CopyFrom, Layer);
            typeof(TileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            lock (FakeProviderPlugin.ProvidersToAdd)
            {
                if (FakeProviderPlugin.ProvidersLoaded)
                    Tile.Add(result);
                else
                    FakeProviderPlugin.ProvidersToAdd.Add(result);
            }
            return result;
        }

        public static INamedTileCollection CreateTileProvider(string Name, int X, int Y, int Width, int Height, ITile[,] CopyFrom, int Layer = 0)
        {
            Type newType = Helper.CreateType();
            Type tileProviderType = typeof(TileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(tileProviderType, true);
            ((dynamic)result).Initialize(Name, X, Y, Width, Height, CopyFrom, Layer);
            typeof(TileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            lock (FakeProviderPlugin.ProvidersToAdd)
            {
                if (FakeProviderPlugin.ProvidersLoaded)
                    Tile.Add(result);
                else
                    FakeProviderPlugin.ProvidersToAdd.Add(result);
            }
            return result;
        }

        #endregion
        #region CreateReadonlyTileProvider

        public static INamedTileCollection CreateReadonlyTileProvider(string Name, int X, int Y, int Width, int Height, int Layer = 0)
        {
            Type newType = Helper.CreateType();
            Type tileProviderType = typeof(ReadonlyTileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(tileProviderType, true);
            ((dynamic)result).Initialize(Name, X, Y, Width, Height, Layer);
            typeof(TileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            typeof(ReadonlyTileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            lock (FakeProviderPlugin.ProvidersToAdd)
            {
                if (FakeProviderPlugin.ProvidersLoaded)
                    Tile.Add(result);
                else
                    FakeProviderPlugin.ProvidersToAdd.Add(result);
            }
            return result;
        }

        public static INamedTileCollection CreateReadonlyTileProvider(string Name, int X, int Y, int Width, int Height, ITileCollection CopyFrom, int Layer = 0)
        {
            Type newType = Helper.CreateType();
            Type tileProviderType = typeof(ReadonlyTileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(tileProviderType, true);
            ((dynamic)result).Initialize(Name, X, Y, Width, Height, CopyFrom, Layer);
            typeof(TileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            typeof(ReadonlyTileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            lock (FakeProviderPlugin.ProvidersToAdd)
            {
                if (FakeProviderPlugin.ProvidersLoaded)
                    Tile.Add(result);
                else
                    FakeProviderPlugin.ProvidersToAdd.Add(result);
            }
            return result;
        }

        public static INamedTileCollection CreateReadonlyTileProvider(string Name, int X, int Y, int Width, int Height, ITile[,] CopyFrom, int Layer = 0)
        {
            Type newType = Helper.CreateType();
            Type tileProviderType = typeof(ReadonlyTileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(tileProviderType, true);
            ((dynamic)result).Initialize(Name, X, Y, Width, Height, CopyFrom, Layer);
            typeof(TileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            typeof(ReadonlyTileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);
            lock (FakeProviderPlugin.ProvidersToAdd)
            {
                if (FakeProviderPlugin.ProvidersLoaded)
                    Tile.Add(result);
                else
                    FakeProviderPlugin.ProvidersToAdd.Add(result);
            }
            return result;
        }

        #endregion
        #region CreatePersonalTileProvider

        public static INamedTileCollection CreatePersonalTileProvider(string Name, HashSet<int> Players, int X, int Y, int Width, int Height, int Layer = 0)
        {
            Type newType = Helper.CreateType();
            Type tileProviderType = typeof(TileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(tileProviderType, true);
            ((dynamic)result).Initialize(Name, X, Y, Width, Height, Layer, Players);
            typeof(TileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);

            Tile.AddPersonal(result);
            result.Enable(false);

            return result;
        }

        public static INamedTileCollection CreatePersonalTileProvider(string Name, HashSet<int> Players, int X, int Y, int Width, int Height, ITileCollection CopyFrom, int Layer = 0)
        {
            Type newType = Helper.CreateType();
            Type tileProviderType = typeof(TileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(tileProviderType, true);
            ((dynamic)result).Initialize(Name, X, Y, Width, Height, CopyFrom, Layer, Players);
            typeof(TileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);

            Tile.AddPersonal(result);
            result.Enable(false);

            return result;
        }

        public static INamedTileCollection CreatePersonalTileProvider(string Name, HashSet<int> Players, int X, int Y, int Width, int Height, ITile[,] CopyFrom, int Layer = 0)
        {
            Type newType = Helper.CreateType();
            Type tileProviderType = typeof(TileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(tileProviderType, true);
            ((dynamic)result).Initialize(Name, X, Y, Width, Height, CopyFrom, Layer, Players);
            typeof(TileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);

            Tile.AddPersonal(result);
            result.Enable(false);

            return result;
        }

        #endregion
        #region SelectObservers

        public static HashSet<int> SelectObservers(int player = -1, int except = -1)
        {
            HashSet<int> result = new HashSet<int>();
            if (player >= 0)
                result.Add(player);
            else
                for (int i = 0; i < 255; i++)
                    result.Add(i);
            result.Remove(except);
            return result;
        }

        #endregion
        #region ApplyPersonal

        public static (ITileCollection tiles, int sx, int sy) ApplyPersonal(IEnumerable<INamedTileCollection> Providers, int X, int Y, int Width, int Height)
        {
            if (Providers.Count() == 0)
                return (Tile, X, Y);

            TileCollection result = new TileCollection(new ITile[Width, Height]);

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    result[x, y] = Tile[X + x, Y + y];

            foreach (INamedTileCollection provider in Providers)
                provider?.Apply(result, X, Y);

            return (result, 0, 0);
        }

        #endregion
        #region GroupBy

        // TODO: Optimize
        public static IEnumerable<IGrouping<IEnumerable<INamedTileCollection>, RemoteClient>> GroupByPersonal(
                List<RemoteClient> Clients, int X, int Y, int Width, int Height)
        {
            IEnumerable<INamedTileCollection> personal = Tile.CollidePersonal(X, Y, Width, Height);
            return Clients.GroupBy(client =>
                personal.Where(provider =>
                    provider.Observers.Contains(client.Id))
                , OEC);
        }

        #endregion
    }
}
