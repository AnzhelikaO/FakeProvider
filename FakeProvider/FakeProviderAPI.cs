using OTAPI.Tile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace FakeProvider
{
    public static class FakeProviderAPI
    {
        #region Data

        public const string WorldProviderName = "__world__";
        public static TileProviderCollection Tile { get; internal set; }
        public static INamedTileCollection World { get; internal set; }
        public static Dictionary<int, List<INamedTileCollection>> Personal { get; internal set; }

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

        public static INamedTileCollection CreatePersonalTileProvider(string Name, int PlayerIndex, int X, int Y, int Width, int Height, int Layer = 0)
        {
            Type newType = Helper.CreateType();
            Type tileProviderType = typeof(TileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(tileProviderType, true);
            ((dynamic)result).Initialize(Name, X, Y, Width, Height, Layer);
            typeof(TileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);

            Personal[PlayerIndex].Add(result);
            result.Enable(false);

            return result;
        }

        public static INamedTileCollection CreatePersonalTileProvider(string Name, int PlayerIndex, int X, int Y, int Width, int Height, ITileCollection CopyFrom, int Layer = 0)
        {
            Type newType = Helper.CreateType();
            Type tileProviderType = typeof(TileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(tileProviderType, true);
            ((dynamic)result).Initialize(Name, X, Y, Width, Height, CopyFrom, Layer);
            typeof(TileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);

            Personal[PlayerIndex].Add(result);
            result.Enable(false);

            return result;
        }

        public static INamedTileCollection CreatePersonalTileProvider(string Name, int PlayerIndex, int X, int Y, int Width, int Height, ITile[,] CopyFrom, int Layer = 0)
        {
            Type newType = Helper.CreateType();
            Type tileProviderType = typeof(TileProvider<>).MakeGenericType(newType);
            INamedTileCollection result = (INamedTileCollection)Activator.CreateInstance(tileProviderType, true);
            ((dynamic)result).Initialize(Name, X, Y, Width, Height, CopyFrom, Layer);
            typeof(TileReference<>)
                .MakeGenericType(newType)
                .GetField("_Provider", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, result);

            Personal[PlayerIndex].Add(result);
            result.Enable(false);

            return result;
        }

        #endregion
        #region CollidesPersonal

        public static bool CollidesPersonal(int PlayerIndex, int X, int Y, int Width, int Height)
        {
            foreach (INamedTileCollection provider in Personal[PlayerIndex])
                if (provider.HasCollision(X, Y, Width, Height))
                    return true;
            return false;
        }

        #endregion
        #region ApplyPersonal

        public static ITile[,] ApplyPersonal(int PlayerIndex, int X, int Y, int Width, int Height)
        {
            ITile[,] result = new ITile[Width, Height];

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    result[x, y] = Tile[X + x, Y + y];

            foreach (INamedTileCollection provider in Personal[PlayerIndex])
                if (provider.Enabled && provider.HasCollision(X, Y, Width, Height))
                    provider.Apply(result, X, Y);

            return result;
        }

        #endregion
    }
}
