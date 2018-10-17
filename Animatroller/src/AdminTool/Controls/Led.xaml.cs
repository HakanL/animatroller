/*
 * Written by  Michele Cattafesta
 * released with BSD License (you can use, modify, redistribute, sell and everything else you want to do) 
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, 
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE  GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED 
 * AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT  LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Animatroller.AdminTool.Controls
{
    /// <summary>
    /// Logica di interazione per UserControl1.xaml
    /// </summary>
    public partial class Led : UserControl
    {
        #region Dependency properties

        /// <summary>Dependency property to Get/Set the current IsActive (True/False)</summary>
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool?), typeof(Led),
                new PropertyMetadata(null, new PropertyChangedCallback(IsActivePropertyChanged)));

        /// <summary>Dependency property to Get/Set Color when IsActive is true</summary>
        public static readonly DependencyProperty ColorOnProperty =
            DependencyProperty.Register("ColorOn", typeof(Color), typeof(Led),
                new PropertyMetadata(Colors.Green, new PropertyChangedCallback(OnColorOnPropertyChanged)));

        /// <summary>Dependency property to Get/Set Color when IsActive is false</summary>
        public static readonly DependencyProperty ColorOffProperty =
            DependencyProperty.Register("ColorOff", typeof(Color), typeof(Led),
                new PropertyMetadata(Colors.Red, new PropertyChangedCallback(OnColorOffPropertyChanged)));

        /// <summary>Dependency property to Get/Set Color when IsActive is false</summary>
        public static readonly DependencyProperty ColorNullProperty =
            DependencyProperty.Register("ColorNull", typeof(Color), typeof(Led),
                new PropertyMetadata(Colors.Gray, new PropertyChangedCallback(OnColorNullPropertyChanged)));

        /// <summary>Dependency property to Get/Set if led will flash</summary>
        public static readonly DependencyProperty FlashingProperty =
            DependencyProperty.Register("Flashing", typeof(bool), typeof(Led),
            new PropertyMetadata(false, new PropertyChangedCallback(OnFlashingPropertyChanged)));

        /// <summary>Dependency property to Get/Set period of flash in milliseconds</summary>
        public static readonly DependencyProperty FlashingPeriodProperty =
            DependencyProperty.Register("FlashingPeriod", typeof(int), typeof(Led),
                new PropertyMetadata(500, new PropertyChangedCallback(OnFlashingPeriodPropertyChanged)));

        #endregion 

        #region Wrapper Properties

        /// <summary>Gets/Sets Value</summary>
        public bool? IsActive
        {
            get { return (bool?)GetValue(IsActiveProperty); }
            set
            {
                SetValue(IsActiveProperty, value);
            }
        }

        /// <summary>Gets/Sets Color when led is True</summary>
        public Color ColorOn
        {
            get
            {
                return (Color)GetValue(ColorOnProperty);
            }
            set
            {
                SetValue(ColorOnProperty, value);
            }
        }

        /// <summary>Gets/Sets Color when led is False</summary>
        public Color ColorOff
        {
            get
            {
                return (Color)GetValue(ColorOffProperty);
            }
            set
            {
                SetValue(ColorOffProperty, value);
            }
        }

        /// <summary>Gets/Sets Color when led is False</summary>
        public Color ColorNull
        {
            get
            {
                return (Color)GetValue(ColorNullProperty);
            }
            set
            {
                SetValue(ColorNullProperty, value);
            }
        }

        /// <summary>Gets/Sets Flashing beaviour on true or false</summary>
        public bool Flashing
        {
            get
            {
                return (bool)GetValue(FlashingProperty);
            }
            set
            {
                SetValue(FlashingProperty, value);
            }
        }

        /// <summary>Gets/Sets Flashing period in ms</summary>
        public int FlashingPeriod
        {
            get
            {
                return (int)GetValue(FlashingPeriodProperty);
            }
            set
            {
                SetValue(FlashingPeriodProperty, value);
            }
        }

        #endregion 

        #region Private fields

        DispatcherTimer timer = new DispatcherTimer();

        #endregion

        #region Constructor

        public Led()
        {
            InitializeComponent();
            timer.Interval = TimeSpan.FromMilliseconds(FlashingPeriod);
            timer.Tick += new EventHandler(timer_Tick);
            if (this.IsActive == true)
                this.backgroundColor.Color = this.ColorOn;
            else if (this.IsActive == false)
                this.backgroundColor.Color = this.ColorOff;
            else
                this.backgroundColor.Color = this.ColorNull;
        }

        #endregion

        #region Callbacks

        /// <summary> tick of flashing timer </summary>
        void timer_Tick(object sender, EventArgs e)
        {
            if (this.IsActive == true)
            {
                if (this.backgroundColor.Color == this.ColorOn)
                    this.backgroundColor.Color = this.ColorNull;
                else
                    this.backgroundColor.Color = this.ColorOn;
            }

            if (this.IsActive == false)
            {
                if (this.backgroundColor.Color == this.ColorOff)
                    this.backgroundColor.Color = this.ColorNull;
                else
                    this.backgroundColor.Color = this.ColorOff;
            }

            if (this.IsActive == null && this.timer.IsEnabled)
                timer.Stop();
        }

        private static void OnFlashingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var led = (Led)d;
            if (led.timer.IsEnabled)
            {
                led.timer.Stop();
                if (led.backgroundColor.Color == led.ColorNull)
                    led.timer_Tick(null, new EventArgs());
            }
            else
                led.timer.Start();
        }

        private static void OnFlashingPeriodPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var led = (Led)d;
            led.timer.Interval = TimeSpan.FromMilliseconds((int)e.NewValue);
        }

        private static void IsActivePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var led = (Led)d;
            if (led.IsActive == null)
                led.backgroundColor.Color = led.ColorNull;
            else if (led.IsActive == true)
                led.backgroundColor.Color = led.ColorOn;
            else
                led.backgroundColor.Color = led.ColorOff;

        }

        private static void OnColorOnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var led = (Led)d;
            led.ColorOn = (Color)e.NewValue;
            if (led.IsActive == true)
                led.backgroundColor.Color = led.ColorOn;
        }

        private static void OnColorOffPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var led = (Led)d;
            led.ColorOff = (Color)e.NewValue;
            if (led.IsActive == false)
                led.backgroundColor.Color = led.ColorOff;
        }

        private static void OnColorNullPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var led = (Led)d;
            led.ColorOff = (Color)e.NewValue;
            if (led.IsActive == null)
                led.backgroundColor.Color = led.ColorNull;
        }


        #endregion

    }
}
