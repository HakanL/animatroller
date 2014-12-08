using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Subjects;
using System.Drawing;
using System.Linq;
using System.Text;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public interface IPixel1D : ILogicalDevice
    {
        int Pixels { get; }
        event EventHandler<PixelChangedEventArgs> PixelChanged;
        event EventHandler<MultiPixelChangedEventArgs> MultiPixelChanged;
    }

    public interface IPixel2D : ILogicalDevice
    {
        int PixelWidth { get; }

        int PixelHeight { get; }

        IObservable<Color[,]> Output { get; }

        void SetPixel(int x, int y, Color color, double brightness = 1.0);
    }
}
