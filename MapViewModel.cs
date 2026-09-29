//   Copyright 2021 Esri
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//   https://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Rasters;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DisplayAMap.Models;

namespace DisplayAMap
{

    internal class MapViewModel : INotifyPropertyChanged
    {

        public MapViewModel()
        {
            SetupMap();

            CreateGraphicsOverlays();
        }

        // Return a SimpleMarkerSymbol whose color and size depend on earthquake magnitude.
        private SimpleMarkerSymbol GetSymbolForMagnitude(double magnitude)
        {
            var symbol = new SimpleMarkerSymbol
            {
                Style = SimpleMarkerSymbolStyle.Circle,
                Outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Black, 1.0)
            };

            // Choose color/size buckets
            if (magnitude < 2.0)
            {
                symbol.Color = System.Drawing.Color.LightGreen;
                symbol.Size = 4.0;
            }
            else if (magnitude < 4.0)
            {
                symbol.Color = System.Drawing.Color.Yellow;
                symbol.Size = 6.0;
            }
            else if (magnitude < 6.0)
            {
                symbol.Color = System.Drawing.Color.Orange;
                symbol.Size = 8.0;
            }
            else
            {
                symbol.Color = System.Drawing.Color.Red;
                symbol.Size = 10.0;
            }

            return symbol;
        }

        // Read earthquake data from a CSV file and return a list of EarthquakeInfo.
        // Expected CSV columns (in order): Date, Time, Latitude, Longitude, Depth, Magnitude
        private List<EarthquakeInfo> ReadEarthquakeInfoFromCSV(string filePath)
        {
            var results = new List<EarthquakeInfo>();

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return results;

            string[] lines;
            try
            {
                lines = File.ReadAllLines(filePath);
            }
            catch
            {
                return results;
            }

            if (lines.Length <= 1)
                return results;

            // Skip header (assumed to be the first line)
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Simple CSV split. This assumes fields do not contain commas.
                var row = line.Split(',');
                if (row.Length < 6)
                    continue;

                // Parse doubles using invariant culture to handle decimal points.
                if (!double.TryParse(row[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double lat))
                    lat = 0.0;
                if (!double.TryParse(row[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double lon))
                    lon = 0.0;
                if (!double.TryParse(row[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double depth))
                    depth = 0.0;
                if (!double.TryParse(row[5], NumberStyles.Float, CultureInfo.InvariantCulture, out double mag))
                    mag = 0.0;

                var info = new EarthquakeInfo()
                {
                    Date = row[0],
                    Time = row[1],
                    Latitude = lat,
                    Longitude = lon,
                    Depth = depth,
                    Magnitude = mag
                };

                results.Add(info);
            }

            return results;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Map? _map;
        public Map? Map
        {
            get { return _map; }
            set
            {
                _map = value;
                OnPropertyChanged();
            }
        }

        private GraphicsOverlayCollection? graphicsOverlays;
        public GraphicsOverlayCollection? GraphicsOverlays
        {
            get { return graphicsOverlays; }
            set
            {
                graphicsOverlays = value;
                OnPropertyChanged();
            }
        }

        private void SetupMap()
        {

            // 1. Get the path to the basemap raster.

            string path = @"C:\RealDisplayMap\MapFiles\HYP_HR_SR_OB_DR.tif";

            // 2. Create an Esri Raster from the TIFF (*.tif) file.

            Raster raster = new Raster(path);

            // 3. Create a Layer from the raster. Layers act as an access point for the data.

            RasterLayer layer = new RasterLayer(raster);

            // 4. Create a basemap from the layer - basemaps go 'below' all the data and help configure the map.

            Basemap basemap = new Basemap(layer);

            // 5. Create a map from the basemap.

            Map = new Map(basemap);

        }

        private void CreateGraphicsOverlays()
        {
            // Create a new graphics overlay to contain a variety of graphics.
            var malibuGraphicsOverlay = new GraphicsOverlay();

            // Add the overlay to a graphics overlay collection.
            GraphicsOverlayCollection overlays = new GraphicsOverlayCollection
            {
                malibuGraphicsOverlay
            };

            // Set the view model's "GraphicsOverlays" property (will be consumed by the map view).
            this.GraphicsOverlays = overlays;

            // Create a point geometry.
            var dumeBeachPoint = new MapPoint(-118.8066, 34.0006, SpatialReferences.Wgs84);

            // Create a symbol to define how the point is displayed.
            var pointSymbol = new SimpleMarkerSymbol
            {
                Style = SimpleMarkerSymbolStyle.Circle,
                Color = System.Drawing.Color.Orange,
                Size = 10.0
            };

            // Add an outline to the symbol.
            pointSymbol.Outline = new SimpleLineSymbol
            {
                Style = SimpleLineSymbolStyle.Solid,
                Color = System.Drawing.Color.Blue,
                Width = 2.0
            };

            // Create a point graphic with the geometry and symbol.
            var pointGraphic = new Graphic(dumeBeachPoint, pointSymbol);

            // Add the point graphic to the graphics overlay.
            malibuGraphicsOverlay.Graphics.Add(pointGraphic);

            // === Load earthquake CSV and add those points to the overlay ===
            // Update this path to where you placed the downloaded CSV file.
            string earthquakesCsvPath = @"C:\RealDisplayMap\MapFiles\database.csv";

            var quakeList = ReadEarthquakeInfoFromCSV(earthquakesCsvPath);
            if (quakeList != null && quakeList.Count > 0)
            {
                foreach (var quake in quakeList)
                {
                    // Create the map point from longitude, latitude
                    var p = new MapPoint(quake.Longitude, quake.Latitude, SpatialReferences.Wgs84);

                    // Attach attributes for use in a popup/inspect tool
                    var attributes = new Dictionary<string, object>()
                    {
                        { "Depth", quake.Depth },
                        { "Magnitude", quake.Magnitude },
                        { "Date", quake.Date },
                        { "Time", quake.Time }
                    };

                    // Create a symbol based on magnitude
                    var quakeSymbol = GetSymbolForMagnitude(quake.Magnitude);

                    var g = new Graphic(p, attributes, quakeSymbol);
                    malibuGraphicsOverlay.Graphics.Add(g);
                }
            }

            // Create a list of points that define a polyline.
            List<MapPoint> linePoints = new List<MapPoint>
            {
                new MapPoint(-118.8215, 34.0140, SpatialReferences.Wgs84),
                new MapPoint(-118.8149, 34.0085, SpatialReferences.Wgs84),
                new MapPoint(-118.8089, 34.0017, SpatialReferences.Wgs84)
            };

            // Create polyline geometry from the points.
            var westwardBeachPolyline = new Polyline(linePoints);

            // Create a symbol for displaying the line.
            var polylineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Green, 3.0);

            // Create a polyline graphic with geometry and symbol.
            var polylineGraphic = new Graphic(westwardBeachPolyline, polylineSymbol);

            // Add polyline to graphics overlay.
            malibuGraphicsOverlay.Graphics.Add(polylineGraphic);

            // Create a list of points that define a polygon boundary.
            List<MapPoint> polygonPoints = new List<MapPoint>
            {
                new MapPoint(-118.8190, 34.0138, SpatialReferences.Wgs84),
                new MapPoint(-118.8068, 34.0216, SpatialReferences.Wgs84),
                new MapPoint(-118.7914, 34.0164, SpatialReferences.Wgs84),
                new MapPoint(-118.7960, 34.0035, SpatialReferences.Wgs84),
                new MapPoint(-118.8086, 34.0035, SpatialReferences.Wgs84)
            };

            // Create polygon geometry.
            var mahouRivieraPolygon = new Polygon(polygonPoints);

            // Create a fill symbol to display the polygon.
            var polygonSymbolOutline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Red, 2.0);
            var polygonFillSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.Yellow, polygonSymbolOutline);

            // Create a polygon graphic with the geometry and fill symbol.
            var polygonGraphic = new Graphic(mahouRivieraPolygon, polygonFillSymbol);

            // Add the polygon graphic to the graphics overlay.
            malibuGraphicsOverlay.Graphics.Add(polygonGraphic);
        }

    }

}
