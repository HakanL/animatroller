using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Animatroller.AdminTool.Controls
{
    /// <summary>
    /// Interaction logic for ColorDimmer.xaml
    /// </summary>
    public partial class ColorDimmer : UserControl
    {
        public ColorDimmer()
        {
            InitializeComponent();
            //            ledControl2.LedColor = Colors.Aqua;
        }

        public string FooterText
        {
            get { return (string)GetValue(FooterTextProperty); }
            set { SetValue(FooterTextProperty, value); }
        }

        public Color GelColor
        {
            get { return (Color)GetValue(GelColorProperty); }
            set { SetValue(GelColorProperty, value); }
        }

        public Color LedColor
        {
            get { return (Color)GetValue(LedColorProperty); }
            set { SetValue(LedColorProperty, value); }
        }

        /// <summary>Dependency property to Get/Set the current FooterText</summary>
        public static readonly DependencyProperty FooterTextProperty =
            DependencyProperty.Register("FooterText", typeof(string), typeof(ColorDimmer), new PropertyMetadata("(FooterText)"));

        /// <summary>Dependency property to Get/Set the current GelColor</summary>
        public static readonly DependencyProperty GelColorProperty =
            DependencyProperty.Register("GelColor", typeof(Color), typeof(ColorDimmer), new PropertyMetadata(Colors.Blue));

        /// <summary>Dependency property to Get/Set the current LedColor</summary>
        public static readonly DependencyProperty LedColorProperty =
            DependencyProperty.Register("LedColor", typeof(Color), typeof(ColorDimmer), new PropertyMetadata(Colors.Purple, new PropertyChangedCallback(OnLedColorPropertyChanged)));

        private static void OnLedColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ColorDimmer)d;
            control.LedColor = (Color)e.NewValue;
            control.ledControl.LedColor = control.LedColor;
        }
    }
}
