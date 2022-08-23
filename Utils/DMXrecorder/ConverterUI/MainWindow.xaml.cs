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
using Animatroller.Processor;

namespace Animatroller.ConverterUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            textBoxInputFile.Text = string.Empty;
            textBoxInputConfigFile.Text = string.Empty;
        }

        private void buttonBrowseInput_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".fseq",
                Filter = "FSEQ Files (*.fseq)|*.fseq|All Files (*.*)|*.*",
                CheckFileExists = true,
                Title = "Select FSEQ for your xLights sequence"
            };

            if (dlg.ShowDialog() == true)
            {
                textBoxInputFile.Text = dlg.FileName;
            }
        }

        private void buttonBrowseInputConfig_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".xml",
                FileName = "xlights_networks.xml",
                Filter = "XLights Networks Files (*.xml)|*.xml|All Files (*.*)|*.*",
                CheckFileExists = true,
                Title = "Select xLights Networks file for your xLights setup"
            };

            if (dlg.ShowDialog() == true)
            {
                textBoxInputConfigFile.Text = dlg.FileName;
            }
        }

        private void buttonConvert_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxInputFile.Text) || string.IsNullOrEmpty(textBoxInputConfigFile.Text))
                return;

            try
            {
                string outputFileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(textBoxInputFile.Text), System.IO.Path.GetFileNameWithoutExtension(textBoxInputFile.Text) + ".cap");
                using (var fileReader = new Common.IO.FseqFileReader(textBoxInputFile.Text, textBoxInputConfigFile.Text))
                using (var fileWriter = new Common.IO.PCapAcnFileWriter(outputFileName))
                {
                    var inputReader = new Common.InputReader(fileReader);
                    progress.Value = 0;

                    var transformer = new OutputWriter(null, fileWriter, 0, false);
                    var converter = new Processor.Command.Convert();

                    // TODO: Report progress
                    var context = new ProcessorContext();
                    converter.Execute(context, inputReader, transformer);

                    progress.Value = 1000;
                }

                MessageBox.Show("Done!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to convert, error: {ex.Message}");
            }
        }
    }
}
