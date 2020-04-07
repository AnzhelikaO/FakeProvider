# FakeProvider

This plugin is an alternative to FakeManager and allows to have fake tile rectangles.
Unlike FakeManager this plugin changes Main.tile to a custom tile provider, so make sure you don't use plugins like *Tiled*
(but don't panic, this plugins takes the same approach as *Tiled* to minimize memory usage and maximize performance).
This means all fake rectangles are stored inside the provider and Main.tile[x, y] actually returns a tile from a tile rectange that
is on top of the Main.tile (world map is also a tile rectangle).

TODO Adding, removing, processing TileProvider, etc
