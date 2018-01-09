using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.Utility
{
    public enum ColorComponent
    {
        None = 0,
        R,
        G,
        B,
        Brightness
    }

    public struct PixelMap
    {
        public int X { get; set; }

        public int Y { get; set; }

        public ColorComponent ColorComponent { get; set; }
    }

    public enum RgbOrder
    {
        RGB
    }

    public enum PixelOrder
    {
        HorizontalLineWiseTopLeft,
        HorizontalSnakeTopLeft,
        HorizontalSnakeBottomLeft,
        VerticalSnakeStartAtTopRight
    }

    public static class PixelMapping
    {
        public static Dictionary<int, PixelMap[]> GeneratePixelMappingFromGlediatorPatch(string fileName)
        {
            var channelMapping = new Dictionary<Tuple<Point, ColorComponent>, int>();
            var universeIdMapping = new Dictionary<Point, int>();

            var universeMapping = new Dictionary<int, int>();

            using (var fs = File.OpenText(fileName))
            {
                string line;
                string[] kvp;
                string[] parts;

                while ((line = fs.ReadLine()) != null)
                {
                    if (line.StartsWith("Patch_Pixel_X_"))
                    {
                        kvp = line.Split('=');
                        if (kvp.Length != 2)
                            continue;

                        parts = kvp[0].Split('_');
                        if (parts.Length != 8)
                            continue;

                        var pt = new Point(int.Parse(parts[3]), int.Parse(parts[5]));

                        switch (parts[6])
                        {
                            case "Ch":
                                switch (parts[7])
                                {
                                    case "R":
                                        channelMapping[Tuple.Create(pt, ColorComponent.R)] = int.Parse(kvp[1]);
                                        break;

                                    case "G":
                                        channelMapping[Tuple.Create(pt, ColorComponent.G)] = int.Parse(kvp[1]);
                                        break;

                                    case "B":
                                        channelMapping[Tuple.Create(pt, ColorComponent.B)] = int.Parse(kvp[1]);
                                        break;
                                }
                                break;

                            case "Uni":
                                universeIdMapping[pt] = int.Parse(kvp[1]);
                                break;
                        }
                    }
                    else if (line.StartsWith("Patch_Uni_ID_"))
                    {
                        kvp = line.Split('=');
                        if (kvp.Length != 2)
                            continue;

                        parts = kvp[0].Split('_');
                        if (parts.Length != 6)
                            continue;

                        if (parts[4] == "Uni" && parts[5] == "Nr")
                            universeMapping[int.Parse(parts[3])] = int.Parse(kvp[1]);
                    }
                }
            }

            var pixelMapping = new Dictionary<int, PixelMap[]>();

            foreach (var kvp in channelMapping)
            {
                int universeId;
                if (!universeIdMapping.TryGetValue(kvp.Key.Item1, out universeId))
                    continue;

                int universe;
                if (!universeMapping.TryGetValue(universeId, out universe))
                    continue;

                PixelMap[] pMapping;
                if (!pixelMapping.TryGetValue(universe, out pMapping))
                {
                    pMapping = new PixelMap[512];
                    pixelMapping.Add(universe, pMapping);
                }

                if (kvp.Value >= 0 && kvp.Value <= 511)
                {
                    pMapping[kvp.Value] = new PixelMap
                    {
                        ColorComponent = kvp.Key.Item2,
                        X = kvp.Key.Item1.X,
                        Y = kvp.Key.Item1.Y
                    };
                }
            }

            return pixelMapping;
        }

        public static Dictionary<int, PixelMap[]> GeneratePixelMapping(
            int width,
            int height,
            int startUniverse = 0,
            RgbOrder rgbOrder = RgbOrder.RGB,
            PixelOrder pixelOrder = PixelOrder.HorizontalSnakeTopLeft,
            int channelShift = 0,
            int? maxPixelsPerPort = null)
        {
            int universe = startUniverse;
            int mappingPos = channelShift;
            int pixelCounter = 0;

            var pixelMapping = new Dictionary<int, PixelMap[]>();

            if (pixelOrder == PixelOrder.HorizontalLineWiseTopLeft)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                        MapPixelRGB(pixelMapping, x, y, ref universe, ref mappingPos, rgbOrder, channelShift, ref pixelCounter, maxPixelsPerPort);
                }
            }
            else if (pixelOrder == PixelOrder.HorizontalSnakeTopLeft)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                        MapPixelRGB(pixelMapping, x, y, ref universe, ref mappingPos, rgbOrder, channelShift, ref pixelCounter, maxPixelsPerPort);

                    y++;

                    if (y >= height)
                        break;

                    for (int x = width - 1; x >= 0; x--)
                        MapPixelRGB(pixelMapping, x, y, ref universe, ref mappingPos, rgbOrder, channelShift, ref pixelCounter, maxPixelsPerPort);
                }
            }

            else if (pixelOrder == PixelOrder.HorizontalSnakeBottomLeft)
            {
                for (int y = height - 1; y >= 0; y--)
                {
                    for (int x = 0; x < width; x++)
                        MapPixelRGB(pixelMapping, x, y, ref universe, ref mappingPos, rgbOrder, channelShift, ref pixelCounter, maxPixelsPerPort);

                    y--;

                    if (y < 0)
                        break;

                    for (int x = width - 1; x >= 0; x--)
                        MapPixelRGB(pixelMapping, x, y, ref universe, ref mappingPos, rgbOrder, channelShift, ref pixelCounter, maxPixelsPerPort);
                }
            }
            else if (pixelOrder == PixelOrder.VerticalSnakeStartAtTopRight)
            {
                for (int x = width - 1; x >= 0; x--)
                {
                    for (int y = 0; y < height; y++)
                        MapPixelRGB(pixelMapping, x, y, ref universe, ref mappingPos, rgbOrder, channelShift, ref pixelCounter, maxPixelsPerPort);

                    x--;
                    if (x < 0)
                        break;

                    for (int y = height - 1; y >= 0; y--)
                        MapPixelRGB(pixelMapping, x, y, ref universe, ref mappingPos, rgbOrder, channelShift, ref pixelCounter, maxPixelsPerPort);
                }
            }

            return pixelMapping;
        }

        public static Dictionary<int, PixelMap[]> GeneratePixelMapping(
            int pixels,
            int startPixel = 0,
            int startUniverse = 0,
            RgbOrder rgbOrder = RgbOrder.RGB,
            int channelShift = 0)
        {
            int universe = startUniverse;
            int mappingPos = channelShift;
            int pixelCounter = 0;

            var pixelMapping = new Dictionary<int, PixelMap[]>();

            for (int x = 0; x < pixels; x++)
                MapPixelRGB(pixelMapping, x + startPixel, 0, ref universe, ref mappingPos, rgbOrder, channelShift, ref pixelCounter, null);

            return pixelMapping;
        }

        private static void MapPixelRGB(Dictionary<int, PixelMap[]> pixelMapping, int x, int y, ref int universe, ref int mappingPos, RgbOrder rgbOrder, int channelShift, ref int pixelCounter, int? maxPixelsPerPort = null)
        {
            switch (rgbOrder)
            {
                case RgbOrder.RGB:
                    MapPixel(pixelMapping, universe, ColorComponent.R, x, y, ref mappingPos);
                    MapPixel(pixelMapping, universe, ColorComponent.G, x, y, ref mappingPos);
                    MapPixel(pixelMapping, universe, ColorComponent.B, x, y, ref mappingPos);
                    break;

                default:
                    throw new NotImplementedException();
            }

            pixelCounter++;

            if (mappingPos + 2 >= 512)
            {
                // Skip to the next universe when we can't fit a full RGB pixel in the current universe
                universe++;
                mappingPos = channelShift;
            }
            else
            {
                if (maxPixelsPerPort.HasValue && pixelCounter >= maxPixelsPerPort.Value)
                {
                    universe++;
                    mappingPos = channelShift;
                    pixelCounter = 0;
                }
            }
        }

        private static void MapPixel(Dictionary<int, PixelMap[]> pixelMapping, int universe, ColorComponent colorComponent, int x, int y, ref int mappingPos)
        {
            PixelMap[] mapping;
            if (!pixelMapping.TryGetValue(universe, out mapping))
            {
                mapping = new PixelMap[512];
                pixelMapping.Add(universe, mapping);
            }

            mapping[mappingPos++] = new PixelMap
            {
                X = x,
                Y = y,
                ColorComponent = colorComponent
            };
        }
    }
}
