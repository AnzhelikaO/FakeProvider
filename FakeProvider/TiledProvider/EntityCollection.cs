#region Using
using System;
using Terraria;
using System.Collections.Generic;
using System.Linq;
#endregion
namespace FakeProvider
{
    public class EntityCollection
    {
        public Dictionary<int, Sign> FakeSigns = new Dictionary<int, Sign>();
        public List<Chest> FakeChests = new List<Chest>();
        private Sign SignPlaceholder = new Sign() { x = -1, y = -1 };

        #region AddSign

        public void AddSign(Sign Sign, bool Replace = true)
        {
            if (Sign == null)
                throw new ArgumentNullException(nameof(Sign), "Sign is null.");

            lock (FakeSigns)
            {
                KeyValuePair<int, Sign>[] signs = FakeSigns.Where(s =>
                    ((s.Value.x == Sign.x) && (s.Value.y == Sign.y))).ToArray();
                if ((signs.Length == 0) || !Replace)
                {
                    int index = -1;
                    for (int i = 999; i >= 0; i--)
                        if (Main.sign[i] == null)
                        {
                            index = i;
                            break;
                        }
                    if (index == -1)
                        throw new Exception("Could not add a sign.");
                    Main.sign[index] = SignPlaceholder;
                    FakeSigns.Add(index, Sign);
                }
                else
                    FakeSigns[signs[0].Key] = Sign;
            }
        }

        #endregion
        #region RemoveSign

        public bool RemoveSign(Sign Sign)
        {
            if (Sign == null)
                throw new ArgumentNullException(nameof(Sign), "Sign is null.");
            return RemoveSign(Sign.x, Sign.y);
        }

        public bool RemoveSign(int X, int Y)
        {
            lock (FakeSigns)
            {
                KeyValuePair<int, Sign>[] signs = FakeSigns.Where(s =>
                    ((s.Value.x == X) && (s.Value.y == Y))).ToArray();
                if (signs.Length != 1)
                    return false;
                int index = signs[0].Key;
                Main.sign[index] = null;
                FakeSigns.Remove(index);
                return true;
            }
        }

        #endregion
        #region AddChest

        public void AddChest(Chest Chest, bool Replace = true)
        {
            if (Chest == null)
                throw new ArgumentNullException(nameof(Chest), "Chest is null.");

            lock (FakeChests)
            {
                int x = Chest.x, y = Chest.y;
                int index = FakeChests.FindIndex(c => ((c.x == x) && (c.y == y)));
                if ((index == -1) || !Replace)
                    FakeChests.Add(Chest);
                else
                    FakeChests[index] = Chest;
            }
        }

        #endregion
        #region RemoveChest

        public bool RemoveChest(Chest Chest)
        {
            if (Chest == null)
                throw new ArgumentNullException(nameof(Chest), "Chest is null.");
            return RemoveChest(Chest.x, Chest.y);
        }

        public bool RemoveChest(int X, int Y)
        {
            lock (FakeChests)
            {
                int index = FakeChests.FindIndex(c => ((c.x == X) && (c.y == Y)));
                if (index == -1)
                    return false;
                FakeChests.RemoveAt(index);
                return true;
            }
        }

        #endregion
        
        #region ApplySigns

        internal void ApplySigns(Dictionary<int, Sign> Signs,
            int X1, int Y1, int X2, int Y2,
            bool ClearIntersectingSigns = false)
        {
            if (ClearIntersectingSigns)
                foreach (int key in Signs.Keys)
                {
                    int x = Signs[key].x, y = Signs[key].y;
                    if ((x >= X1) && (x < X2) && (y >= Y1) && (y < Y2))
                        Signs.Remove(key);
                }

            lock (FakeSigns)
                foreach (KeyValuePair<int, Sign> pair in FakeSigns)
                {
                    int x = pair.Value.x, y = pair.Value.y;
                    if ((x >= X1) && (x < X2) && (y >= Y1) && (y < Y2))
                        Signs.Add(pair.Key, pair.Value);
                }
        }

        #endregion
        #region ApplyChest

        internal void ApplyChest(ref Chest Chest, int AbsoluteX, int AbsoluteY)
        {
            lock (FakeChests)
                foreach (Chest chest in FakeChests)
                    if ((chest.x == AbsoluteX) && (chest.y == AbsoluteY))
                    {
                        Chest = chest;
                        return;
                    }
        }

        #endregion
    }
}