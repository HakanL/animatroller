using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using Animatroller.Simulator.Extensions;
using Animatroller.Framework.Extensions;
using NLog;
using System.Threading;

namespace Animatroller.Simulator
{
    public partial class SimulatorForm : Form, IPort
    {
        private List<IUpdateableControl> updateableControls = new List<IUpdateableControl>();
        protected static Logger log = LogManager.GetCurrentClassLogger();

        public SimulatorForm()
        {
            InitializeComponent();

            this.SetStyle(
              ControlStyles.AllPaintingInWmPaint |
              ControlStyles.UserPaint |
              ControlStyles.DoubleBuffer, true);
        }

        public SimulatorForm AutoWireUsingReflection(IScene scene, params IRunningDevice[] excludeDevices)
        {
            AutoWireUsingReflection_Simple(scene, excludeDevices);

            var fields = scene.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                object fieldValue = field.GetValue(scene);
                if (fieldValue == null)
                    continue;

                // Auto-wire
                if (typeof(IRunningDevice).IsInstanceOfType(fieldValue))
                {
                    if (excludeDevices.Contains((IRunningDevice)fieldValue))
                        // Excluded
                        continue;
                }

                if (field.GetCustomAttributes(typeof(Animatroller.Framework.SimulatorSkipAttribute), false).Any())
                    continue;

                if (typeof(IPort).IsInstanceOfType(fieldValue))
                    continue;

                if (field.FieldType == typeof(Dimmer2))
                    this.Connect(new Animatroller.Simulator.TestLight((IApiVersion3)fieldValue));
                else if (field.FieldType == typeof(Dimmer3))
                    this.Connect(new Animatroller.Simulator.TestLight((Dimmer3)fieldValue));
                else if (field.FieldType == typeof(ColorDimmer2))
                    this.Connect(new Animatroller.Simulator.TestLight((IApiVersion3)fieldValue));
                else if (field.FieldType == typeof(ColorDimmer3))
                    this.Connect(new Animatroller.Simulator.TestLight((ColorDimmer3)fieldValue));
                else if (field.FieldType == typeof(StrobeColorDimmer2))
                    this.Connect(new Animatroller.Simulator.TestLight((IApiVersion3)fieldValue));
                else if (field.FieldType == typeof(StrobeColorDimmer3))
                    this.Connect(new Animatroller.Simulator.TestLight((StrobeColorDimmer3)fieldValue));
                else if (field.FieldType == typeof(MovingHead))
                    this.Connect(new Animatroller.Simulator.TestLight((MovingHead)fieldValue));
                else if (field.FieldType == typeof(Pixel1D))
                    this.Connect(new Animatroller.Simulator.TestPixel1D((Pixel1D)fieldValue));
                else if (field.FieldType == typeof(Pixel1D))
                    this.Connect(new Animatroller.Simulator.TestPixel1D((Pixel1D)fieldValue));
                else if (field.FieldType == typeof(VirtualPixel1D))
                    this.Connect(new Animatroller.Simulator.TestPixel1D((VirtualPixel1D)fieldValue));
                else if (field.FieldType == typeof(VirtualPixel2D))
                    this.Connect(new Animatroller.Simulator.TestPixel2D((VirtualPixel2D)fieldValue));
                else if (field.FieldType == typeof(AnalogInput2))
                    this.AddAnalogInput((AnalogInput2)fieldValue);
                else if (field.FieldType == typeof(AnalogInput3))
                    this.AddAnalogInput((AnalogInput3)fieldValue);
                else if (field.FieldType == typeof(MotorWithFeedback))
                {
                    // Skip
                    //                    this.AddMotor((MotorWithFeedback)fieldValue);
                }
                else if (field.FieldType == typeof(Switch))
                {
                    // Skip
                }
                else if (field.FieldType == typeof(DigitalInput2))
                {
                    var buttonType = (Animatroller.Framework.SimulatorButtonTypeAttribute)
                        field.GetCustomAttributes(typeof(Animatroller.Framework.SimulatorButtonTypeAttribute), false).FirstOrDefault();

                    if (buttonType != null)
                    {
                        switch (buttonType.Type)
                        {
                            case Framework.SimulatorButtonTypes.FlipFlop:
                                AddDigitalInput_FlipFlop((DigitalInput2)fieldValue, buttonType.ShowOutput);
                                break;

                            case Framework.SimulatorButtonTypes.Momentarily:
                                AddDigitalInput_Momentarily((DigitalInput2)fieldValue);
                                break;
                        }
                    }
                    else
                        AddDigitalInput_Momentarily((DigitalInput2)fieldValue);
                }
                else if (field.FieldType == typeof(AudioPlayer))
                {
                    // Skip
                }
                else if (field.FieldType == typeof(Animatroller.Framework.Expander.OscServer))
                {
                    // Skip
                }
                else if (field.FieldType == typeof(Animatroller.Framework.Controller.Sequence))
                {
                    // Skip
                }
                else if (field.FieldType == typeof(Animatroller.Framework.Import.LorImport))
                {
                    // Skip
                }
                else if (field.FieldType == typeof(Animatroller.Framework.Import.VixenImport))
                {
                    // Skip
                }
                else if (field.FieldType == typeof(Animatroller.Framework.Import.BaseImporter.Timeline))
                {
                    // Skip
                }
                else if (field.FieldType == typeof(Animatroller.Framework.LogicalDevice.OperatingHours))
                {
                    // Skip
                }
                else if (field.FieldType.Name.StartsWith("EnumStateMachine") ||
                    field.FieldType.Name.StartsWith("IntStateMachine") ||
                    field.FieldType.Name.StartsWith("Timeline"))
                {
                    // Skip
                }
                else
                {
                    log.Info("Unknown field {0}", field.FieldType);
                }
            }

            return this;
        }

        public SimulatorForm AutoWireUsingReflection_Simple(IScene scene, params IRunningDevice[] excludeDevices)
        {
            var fields = scene.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                object fieldValue = field.GetValue(scene);
                if (fieldValue == null)
                    continue;

                // Auto-wire
                if (typeof(IRunningDevice).IsInstanceOfType(fieldValue))
                {
                    if (excludeDevices.Contains((IRunningDevice)fieldValue))
                        // Excluded
                        continue;
                }

                if (field.FieldType == typeof(Switch))
                    this.AddDigitalOutput((Switch)fieldValue);
                else if (field.FieldType == typeof(DigitalOutput2))
                    this.AddDigitalOutput((DigitalOutput2)fieldValue);
                else if (field.FieldType.Name.StartsWith("EnumStateMachine") ||
                    field.FieldType.Name.StartsWith("IntStateMachine"))
                {
                    var stateMachine = (Animatroller.Framework.Controller.IStateMachine)fieldValue;

                    var control = AddLabel(stateMachine.Name);
                    if (string.IsNullOrEmpty(stateMachine.CurrentStateString))
                        control.Text = "<idle>";
                    else
                        control.Text = stateMachine.CurrentStateString;

                    stateMachine.StateChangedString += (sender, e) =>
                    {
                        this.UIThread(delegate
                        {
                            if (string.IsNullOrEmpty(e.NewState))
                                control.Text = "<idle>";
                            else
                                control.Text = e.NewState;
                        });
                    };
                }
                else if (field.FieldType == typeof(Animatroller.Framework.Controller.CueList))
                {
                    var cueList = (Animatroller.Framework.Controller.CueList)fieldValue;

                    var control = AddLabel(cueList.Name);

                    cueList.CurrentCueId.Subscribe(x =>
                        {
                            this.UIThread(delegate
                            {
                                if (x.HasValue)
                                    control.Text = x.ToString();
                                else
                                    control.Text = "<idle>";
                            });
                        });
                }
            }

            return this;
        }

        public Control.StrobeBulb AddNewLight(string name)
        {
            var moduleControl = new Control.ModuleControl();
            moduleControl.Text = name;
            moduleControl.Size = new System.Drawing.Size(80, 80);

            var control = new Control.StrobeBulb();
            moduleControl.ChildControl = control;
            control.Color = Color.Black;

            flowLayoutPanelLights.Controls.Add(moduleControl);

            return control;
        }

        public Control.RopeLight AddNewRope(string name, int pixels)
        {
            var moduleControl = new Control.ModuleControl();
            moduleControl.Text = name;
            moduleControl.Size = new System.Drawing.Size(4 * pixels, 50);

            var control = new Control.RopeLight();
            moduleControl.ChildControl = control;
            control.Pixels = pixels;

            flowLayoutPanelLights.Controls.Add(moduleControl);

            return control;
        }

        public Control.MatrixLight AddNewMatrix(string name, int width, int height)
        {
            var moduleControl = new Control.ModuleControl();
            moduleControl.Text = name;
            moduleControl.Size = new System.Drawing.Size(8 * width + 6, 8 * height + 27);

            var control = new Control.MatrixLight(width, height);
            moduleControl.ChildControl = control;

            flowLayoutPanelLights.Controls.Add(moduleControl);

            return control;
        }

        public Label AddLabel(string label)
        {
            var moduleControl = new Control.ModuleControl();
            moduleControl.Text = label;
            moduleControl.Size = new System.Drawing.Size(150, 80);
            flowLayoutPanelLights.Controls.Add(moduleControl);

            var centerControl = new Control.CenterControl();
            moduleControl.ChildControl = centerControl;

            var labelControl = new Label();
            labelControl.Size = new System.Drawing.Size(150, 60);
            labelControl.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            labelControl.TextAlign = ContentAlignment.MiddleCenter;
            centerControl.ChildControl = labelControl;

            return labelControl;
        }

        public Animatroller.Framework.PhysicalDevice.MotorWithFeedback AddMotor(MotorWithFeedback logicalDevice)
        {
            var moduleControl = new Control.ModuleControl();
            moduleControl.Text = logicalDevice.Name;
            moduleControl.Size = new System.Drawing.Size(160, 80);

            var control = new Control.Motor();
            moduleControl.ChildControl = control;

            flowLayoutPanelLights.Controls.Add(moduleControl);

            var device = new Animatroller.Framework.PhysicalDevice.MotorWithFeedback((target, speed, timeout) =>
            {
                control.Target = target;
                control.Speed = speed;
                control.Timeout = timeout;
            });

            control.Trigger = device.Trigger;

            device.Connect(logicalDevice);

            return device;
        }

        public Animatroller.Framework.PhysicalDevice.DigitalOutput AddDigitalOutput(Switch logicalDevice)
        {
            var moduleControl = new Control.ModuleControl();
            moduleControl.Text = logicalDevice.Name;
            moduleControl.Size = new System.Drawing.Size(80, 80);

            var centerControl = new Control.CenterControl();
            moduleControl.ChildControl = centerControl;

            var control = new Animatroller.Simulator.Control.Bulb.LedBulb();
            control.On = false;
            control.Size = new System.Drawing.Size(20, 20);
            centerControl.ChildControl = control;

            flowLayoutPanelLights.Controls.Add(moduleControl);

            var device = new Animatroller.Framework.PhysicalDevice.DigitalOutput(x =>
            {
                this.UIThread(delegate
                {
                    control.On = x;
                });
            });

            device.Connect(logicalDevice);

            return device;
        }

        public Animatroller.Framework.PhysicalDevice.DigitalOutput AddDigitalOutput(DigitalOutput2 logicalDevice)
        {
            var moduleControl = new Control.ModuleControl();
            moduleControl.Text = logicalDevice.Name;
            moduleControl.Size = new System.Drawing.Size(80, 80);

            var centerControl = new Control.CenterControl();
            moduleControl.ChildControl = centerControl;

            var control = new Animatroller.Simulator.Control.Bulb.LedBulb();
            control.On = false;
            control.Size = new System.Drawing.Size(20, 20);
            centerControl.ChildControl = control;

            flowLayoutPanelLights.Controls.Add(moduleControl);

            var device = new Animatroller.Framework.PhysicalDevice.DigitalOutput(x =>
            {
                this.UIThread(delegate
                {
                    control.On = x;
                });
            });

            device.Connect(logicalDevice);

            return device;
        }

        public Animatroller.Framework.PhysicalDevice.DigitalInput AddDigitalInput_FlipFlop(DigitalInput2 logicalDevice, bool showOutput)
        {
            var control = new CheckBox();
            control.Text = logicalDevice.Name;
            control.Size = new System.Drawing.Size(80, 60);
            control.ImageAlign = ContentAlignment.TopLeft;

            var indicator = new Animatroller.Simulator.Control.Bulb.LedBulb();
            indicator.On = false;
            indicator.Size = new System.Drawing.Size(12, 12);
            indicator.Left = 0;
            indicator.Top = 0;
            var imageOff = new Bitmap(12, 12);
            indicator.DrawToBitmap(imageOff, new Rectangle(0, 0, 12, 12));
            var imageOn = new Bitmap(12, 12);
            indicator.On = true;
            indicator.DrawToBitmap(imageOn, new Rectangle(0, 0, 12, 12));

            flowLayoutPanelLights.Controls.Add(control);

            var device = new Animatroller.Framework.PhysicalDevice.DigitalInput();

            control.CheckedChanged += (sender, e) =>
            {
                device.Trigger((sender as CheckBox).Checked);
            };

            device.Connect(logicalDevice);

            control.Checked = logicalDevice.Value;

            if (showOutput)
            {
                logicalDevice.Output.Subscribe(x =>
                    {
                        control.Image = x ? imageOn : imageOff;
                    });
            }

            return device;
        }

        public Animatroller.Framework.PhysicalDevice.AnalogInput AddAnalogInput(AnalogInput2 logicalDevice)
        {
            var control = new TrackBar();
            control.Text = logicalDevice.Name;
            control.Size = new System.Drawing.Size(80, 80);
            control.Maximum = 255;

            flowLayoutPanelLights.Controls.Add(control);

            var device = new Animatroller.Framework.PhysicalDevice.AnalogInput();

            control.ValueChanged += (sender, e) =>
                {
                    device.Trigger((sender as TrackBar).Value / 255.0);
                };

            device.Connect(logicalDevice);

            control.Value = logicalDevice.Value.GetByteScale();

            logicalDevice.Output.Subscribe(x =>
                {
                    control.Value = x.Value.GetByteScale();
                });

            return device;
        }

        public Animatroller.Framework.PhysicalDevice.AnalogInput AddAnalogInput(AnalogInput3 logicalDevice)
        {
            var moduleControl = new Control.ModuleControl();
            moduleControl.Text = logicalDevice.Name;
            moduleControl.Size = new System.Drawing.Size(80, 80);

            var control = new TrackBar();
            moduleControl.ChildControl = control;
            control.Size = new System.Drawing.Size(80, 80);
            control.Maximum = 255;
            control.TickFrequency = 26;

            flowLayoutPanelLights.Controls.Add(moduleControl);

            var device = new Animatroller.Framework.PhysicalDevice.AnalogInput();

            control.ValueChanged += (sender, e) =>
            {
                device.Trigger((sender as TrackBar).Value / 255.0);
            };

            device.Connect(logicalDevice);

            control.Value = logicalDevice.Value.GetByteScale();

            logicalDevice.Output.Subscribe(x =>
            {
                control.Value = x.GetByteScale();
            });

            return device;
        }

        public Animatroller.Framework.PhysicalDevice.DigitalInput AddDigitalInput_Momentarily(DigitalInput2 logicalDevice)
        {
            var control = new Button();
            control.Text = logicalDevice.Name;
            control.UseMnemonic = false;
            control.Size = new System.Drawing.Size(80, 80);

            flowLayoutPanelLights.Controls.Add(control);

            var device = new Animatroller.Framework.PhysicalDevice.DigitalInput();

            control.MouseDown += (sender, e) =>
                {
                    device.Trigger(true);
                };

            control.MouseUp += (sender, e) =>
                {
                    device.Trigger(false);
                };

            device.Connect(logicalDevice);

            return device;
        }

        public void Connect(INeedsLabelLight output)
        {
            output.LabelLightControl = AddNewLight(output.Name);

            if (output is IUpdateableControl)
            {
                lock (this.updateableControls)
                {
                    this.updateableControls.Add((IUpdateableControl)output);
                }
            }
        }

        public void Connect(INeedsRopeLight output)
        {
            output.LightControl = AddNewRope(output.ConnectedDevice.Name, output.Pixels);
        }

        public void Connect(INeedsMatrixLight output)
        {
            output.LightControl = AddNewMatrix(output.Name, 20, 10);
        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            if (Monitor.TryEnter(this.updateableControls))
            {
                try
                {
                    foreach (var control in this.updateableControls)
                    {
                        control.Update();
                    }
                }
                finally
                {
                    Monitor.Exit(this.updateableControls);
                }
            }
        }
    }
}
