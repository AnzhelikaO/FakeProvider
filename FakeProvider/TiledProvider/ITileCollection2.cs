using OTAPI.Tile;
namespace FakeProvider
{
    public interface ITileCollection2 : ITileCollection
    {
        string Name { get; }
        int X { get; set; }
        int Y { get; set; }
        int Layer { get; }
        bool Enabled { get; set; }
    }
}