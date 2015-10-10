using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        private OscReceiver receiver;
        private Task receiverTask;
        private CancellationTokenSource cancelSource;

        public MainWindow()
        {
            InitializeComponent();

            //            this.sender = new OscSender(System.Net.IPAddress.Loopback, 0, 5005);
//            this.sender = new OscSender(System.Net.IPAddress.Parse("192.168.240.123"), 0, 5005);
            this.sender = new OscSender(System.Net.IPAddress.Parse("192.168.240.226"), 0, 5005);
//            this.sender = new OscSender(System.Net.IPAddress.Parse("192.168.1.106"), 0, 5005);
            this.sender.Connect();

            this.receiver = new OscReceiver(8000);
            this.cancelSource = new System.Threading.CancellationTokenSource();

            this.receiverTask = new Task(x =>
            {
                try
                {
                    while (!this.cancelSource.IsCancellationRequested)
                    {
                        while (this.receiver.State != Rug.Osc.OscSocketState.Closed)
                        {
                            if (this.receiver.State == Rug.Osc.OscSocketState.Connected)
                            {
                                var packet = this.receiver.Receive();

                                listBoxLog.Dispatcher.Invoke(
                                  System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                                    {
                                        listBoxLog.Items.Add(string.Format("Received OSC message: {0}", packet));
                                    }
                                ));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message == "The receiver socket has been disconnected")
                        // Ignore
                        return;
                }

            }, this.cancelSource.Token, TaskCreationOptions.LongRunning);

            this.receiver.Connect();
            this.receiverTask.Start();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            var msg = new OscMessage("/audio/bg/next");
            //var msg = new OscMessage("/output",
            //    3,
            //    false);

            this.sender.Send(msg);
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxVideo.SelectedValue == null)
                return;

            var item = (ListBoxItem)comboBoxVideo.SelectedValue;
            //            var msg = new OscMessage("/audio/bg/next");
            var msg = new OscMessage("/video/play", item.Content);

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

        private void Window_Closed(object sender, EventArgs e)
        {
            this.cancelSource.Cancel();
            this.receiver.Close();
        }

        private void listBoxLog_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            listBoxLog.Items.Clear();
        }
    }
}
