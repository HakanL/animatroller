using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LedControl
{
    /// <summary>
    /// Simple LED
    /// </summary>
    public partial class SimpleLed : UserControl
    {
        /// <summary>Dependency property to Get/Set Color</summary>
        public static readonly DependencyProperty LedColorProperty =
            DependencyProperty.Register("LedColor", typeof(Color), typeof(SimpleLed),
                new PropertyMetadata(Colors.Green, new PropertyChangedCallback(OnLedColorPropertyChanged)));

        /// <summary>Gets/Sets Color when led is True</summary>
        public Color LedColor
        {
            get
            {
                return (Color)GetValue(LedColorProperty);
            }
            set
            {
                SetValue(LedColorProperty, value);
            }
        }

        public SimpleLed()
        {
            InitializeComponent();

            this.backgroundColor.Color = this.LedColor;
        }


        private static void OnLedColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var led = (SimpleLed)d;
            led.LedColor = (Color)e.NewValue;
            led.backgroundColor.Color = led.LedColor;
        }
    }
}
