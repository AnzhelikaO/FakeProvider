#region Using
using OTAPI.Tile;
#endregion
namespace FakeManager
{
    public interface ITileCollection2 : ITileCollection
    {
        string Name { get; }
        int X { get; set; }
        int Y { get; set; }
    }
}