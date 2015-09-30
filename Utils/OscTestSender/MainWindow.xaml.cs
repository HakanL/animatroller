using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Rug.Osc;

namespace OscTestSender
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private OscSender sender;

        public MainWindow()
        {
            InitializeComponent();

            this.sender = new OscSender(System.Net.IPAddress.Loopback, 0, 9998);
            this.sender.Connect();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            var msg = new OscMessage("/output",
                3,
                false);

            this.sender.Send(msg);
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            var msg = new OscMessage("/audio/bg/next");

            this.sender.Send(msg);
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            var msg = new OscMessage("/audio/fx/playnew",
                "sixthsense-deadpeople.wav",
                1.0f,
                0.3f);

            this.sender.Send(msg);
        }
    }
}
