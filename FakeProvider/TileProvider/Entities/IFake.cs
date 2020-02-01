namespace FakeProvider
{
    public interface IFake
    {
        INamedTileCollection Provider { get; }
        int Index { get; set; }
        int RelativeX { get; set; }
        int RelativeY { get; set; }
        int X { get; set; }
        int Y { get; set; }
        ushort[] TileTypes { get; }
    }
}
