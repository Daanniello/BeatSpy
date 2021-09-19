using ChartDirector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace BeatSpy
{
    public class SimpleRadar
    {
        public static Image createChart(float highSpeed, float acc, float midSpeed, float tech)
        {
            // The labels for the chart
            string[] labels = { "High Speed", "Acc", "Mid Speed", "Tech" };

            // The data for the chart
            double[] data = { highSpeed, acc, midSpeed, tech };

            // Create a PolarChart object of size 450 x 350 pixels
            PolarChart c = new PolarChart(500, 500);

            // Set center of plot area at (225, 185) with radius 150 pixels
            c.setPlotArea(250, 250, 160);

            // Add an area layer to the polar chart
            c.addAreaLayer(data, 1942002);

            c.setBackground(4473924);


            // Set the labels to the angular axis as spokes
            var imgLabels = c.angularAxis().setLabels(labels);
            imgLabels.setFontColor(1942002);
            imgLabels.setFontSize(24);

            // Output the chart
            var image = c.makeImage();

            ////Remove stupid ad, fack off. Make a service for others, not yourself.  
            var bitmap = (Bitmap)image;
            
            for (var i = 0; i < bitmap.Width; i++)
            {
                for (var j = 0; j < bitmap.Height; j++)
                {
                    if (j > bitmap.Height - (bitmap.Height / 15)) bitmap.SetPixel(i, j, Color.FromArgb(68, 68, 68));
                }
            }

            //bitmap.MakeTransparent(Color.FromArgb(255, 255, 255));

            // Return the output
            return bitmap;
        }
    }
}
