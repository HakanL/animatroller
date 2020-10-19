using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Serilog;

namespace Animatroller.AdminTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string DebugTemplate = "{Timestamp:HH:mm:ss.fff} {Logger} [{Level}] {Message}{Exception}\r\n";
        private const string FileTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Logger} [{Level}] {Message}{NewLine}{Exception}";

        private ILogger log;
        private ExpanderCommunication.IClientCommunication communication;
        private readonly Dictionary<string, Type> typeCache = new Dictionary<string, Type>();
        private System.Threading.Timer pingTimer;
        private readonly Dictionary<string, Control> componentLookup = new Dictionary<string, Control>();

        public MainWindow()
        {
            InitializeComponent();

            var logConfig = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Verbose()
                .WriteTo.Debug(outputTemplate: DebugTemplate)
                .WriteTo.File(
                    path: System.IO.Path.Combine(AppContext.BaseDirectory, "Logs", "log-{Date}.txt"),
                    outputTemplate: FileTemplate);

            this.log = Log.Logger = logConfig.CreateLogger();

            string instanceId = Guid.NewGuid().ToString("n");

//FIXME            DotNetty.Common.Internal.Logging.InternalLoggerFactory.DefaultFactory.AddProvider(new Microsoft.Extensions.Logging.Debug.DebugLoggerProvider());
            //DotNetty.Common.ResourceLeakDetector.Level = DotNetty.Common.ResourceLeakDetector.DetectionLevel.Advanced;

            this.communication = new ExpanderCommunication.NettyClient(
                logger: this.log,
                host: "127.0.0.1",
                port: 54345,
                instanceId: instanceId,
                dataReceivedAction: (t, d) => DataReceived(t, d),
                connectedAction: async () => await SendMessage(new AdminMessage.Ping()));

            Task.Run(async () => await this.communication.StartAsync()).Wait();

            this.pingTimer = new System.Threading.Timer(PingTimerCallback, null, 1 * 60_000, 1 * 60_000);
        }

        private void PingTimerCallback(object state)
        {
            try
            {
                Task.Run(async () => await SendMessage(new AdminMessage.Ping())).Wait();
            }
            catch (Exception ex)
            {
                this.log.Error(ex, "Error in " + nameof(PingTimerCallback));
            }
        }

        private void DataReceived(string messageType, byte[] data)
        {
            object messageObject;
            Type type = GetMessageType(messageType);

            using (var ms = new System.IO.MemoryStream(data))
            {
                messageObject = AdminMessage.Serializer.DeserializeFromStream(ms, type);
            }

            switch (messageObject)
            {
                case AdminMessage.Ping ping:
                    break;

                case AdminMessage.ControlUpdate controlUpdate:
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        ProcessControlUpdate(controlUpdate.Updates);
                    });
                    break;

                case AdminMessage.NewSceneDefinition newSceneDefinition:
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LoadNewScene(newSceneDefinition);
                        ProcessControlUpdate(newSceneDefinition.InitialStatus);
                    });
                    break;

                default:
                    this.log.Warning("Unknown/unhandled message type {MessageType}", messageType);
                    break;
            }
        }

        private void LoadNewScene(AdminMessage.NewSceneDefinition sceneDefinition)
        {
            var groups = new Dictionary<string, GroupBox>();

            foreach (var control in sceneDefinition.Definition.Components)
            {
                var childControl = new Controls.ColorDimmer
                {
                    HeaderText = null
                };

                var parentControl = new GroupBox
                {
                    Name = control.Id,
                    Width = 100,
                    Height = 100,
                    Header = control.Name,
                    Content = childControl,
                    FontSize = 8,
                    Margin = new Thickness(4)
                };

                componentLookup[control.Id] = parentControl;

                if (!string.IsNullOrEmpty(control.Group))
                {
                    if (!groups.TryGetValue(control.Group, out var componentGroup))
                    {
                        componentGroup = new GroupBox
                        {
                            Name = control.Group.Replace(" ", ""),
                            Header = control.Group,
                            Content = new WrapPanel
                            {
                                Margin = new Thickness(8)
                            },
                            FontSize = 10,
                            Margin = new Thickness(4)
                        };

                        //componentGroup.SetResourceReference(Control.StyleProperty, "ComponentGroupBoxStyle");

                        controlPanel.Children.Add(componentGroup);
                        groups.Add(control.Group, componentGroup);
                    }

                    (componentGroup.Content as WrapPanel).Children.Add(parentControl);
                }
                else
                    controlPanel.Children.Add(parentControl);
            }
        }

        private Type GetMessageType(string messageType)
        {
            lock (this.typeCache)
            {
                if (!this.typeCache.TryGetValue(messageType, out Type type))
                {
                    type = typeof(AdminMessage.Ping).Assembly.GetType(messageType, true);
                    this.typeCache.Add(messageType, type);
                }

                return type;
            }
        }

        private void ProcessControlUpdate(AdminMessage.ComponentUpdate[] updates)
        {
            foreach (var update in updates)
            {
                try
                {
                    Type messageType = GetMessageType(update.MessageType);
                    using (var ms = new System.IO.MemoryStream(update.Object))
                    {
                        object updateObject = AdminMessage.Serializer.DeserializeFromStream(ms, messageType);

                        if (this.componentLookup.TryGetValue(update.ComponentId, out var component))
                        {
                            UpdateControl(component, updateObject);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.log.Warning(ex, "Error in " + nameof(ProcessControlUpdate));
                }
            }
        }

        private void UpdateControl(Control control, object updateObject)
        {
            switch (updateObject)
            {
                case AdminMessage.StrobeColorDimmer update:
                    var xyz = (control as GroupBox).Content as Controls.ColorDimmer;
                    xyz.FooterText = (update.Owned ? "* " : "") + update.Brightness.ToString("0%");
                    xyz.GelColor = Color.FromRgb(update.Red, update.Green, update.Blue);
                    xyz.LedColor = Controls.Utility.GetColorFromColorBrightness(update.Brightness, update.Red, update.Green, update.Blue);
                    break;

                case AdminMessage.Binary update:
                    break;
            }
        }

        private async Task SendMessage(object message)
        {
            using (var ms = new System.IO.MemoryStream())
            {
                AdminMessage.Serializer.Serialize(message, ms);

                await this.communication.SendData(message.GetType().FullName, ms.ToArray());
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Task.Run(async () => await this.communication.StopAsync()).Wait();
        }
    }
}
