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
using NLog;

namespace Animatroller.Simulator
{
    public partial class SimulatorForm : Form, IPort
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();

        public SimulatorForm()
        {
            InitializeComponent();
        }

        public SimulatorForm AutoWireUsingReflection(IScene scene, params IDevice[] excludeDevices)
        {
            var fields = scene.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                object fieldValue = field.GetValue(scene);
                if (fieldValue == null)
                    continue;

                // Auto-wire
                if (typeof(IDevice).IsInstanceOfType(fieldValue))
                {
                    if (excludeDevices.Contains((IDevice)fieldValue))
                        // Excluded
                        continue;
                }

                if (field.FieldType == typeof(Dimmer))
                    this.Connect(new Animatroller.Simulator.TestLight((Dimmer)fieldValue));
                else if (field.FieldType == typeof(ColorDimmer))
                    this.Connect(new Animatroller.Simulator.TestLight((ColorDimmer)fieldValue));
                else if (field.FieldType == typeof(StrobeDimmer))
                    this.Connect(new Animatroller.Simulator.TestLight((StrobeDimmer)fieldValue));
                else if (field.FieldType == typeof(StrobeColorDimmer))
                    this.Connect(new Animatroller.Simulator.TestLight((StrobeColorDimmer)fieldValue));
                else if (field.FieldType == typeof(Pixel1D))
                    this.Connect(new Animatroller.Simulator.TestPixel1D((Pixel1D)fieldValue));
                else if (field.FieldType == typeof(ColorDimmer))
                    this.Connect(new Animatroller.Simulator.TestLight((ColorDimmer)fieldValue));
                else if (field.FieldType == typeof(Pixel1D))
                    this.Connect(new Animatroller.Simulator.TestPixel1D((Pixel1D)fieldValue));
                else if (field.FieldType == typeof(VirtualPixel1D))
                    this.Connect(new Animatroller.Simulator.TestPixel1D((VirtualPixel1D)fieldValue));
                else if (field.FieldType == typeof(Switch))
                    this.AddDigitalOutput((Switch)fieldValue);
                else if (field.FieldType == typeof(MotorWithFeedback))
                {
                    // Skip
//                    this.AddMotor((MotorWithFeedback)fieldValue);
                }
                else if (field.FieldType == typeof(DigitalInput))
                {
                    // Skip
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
                else if (field.FieldType.Name.StartsWith("StateMachine"))
                {
                    var stateMachine = (Animatroller.Framework.Controller.IStateMachine)fieldValue;

                    var control = AddLabel(stateMachine.Name);
                    control.Text = stateMachine.CurrentStateString;

                    stateMachine.StateChangedString += (sender, e) =>
                        {
                            this.UIThread(delegate
                            {
                                control.Text = e.NewState;
                            });
                        };
                }
                else
                {
                    log.Info("Unknown field {0}", field.FieldType);
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

        public Animatroller.Framework.PhysicalDevice.DigitalInput AddDigitalInput_FlipFlop(DigitalInput logicalDevice)
        {
            var control = new CheckBox();
            control.Text = logicalDevice.Name;
            control.Size = new System.Drawing.Size(80, 80);

            flowLayoutPanelLights.Controls.Add(control);

            var device = new Animatroller.Framework.PhysicalDevice.DigitalInput();

            control.CheckedChanged += (sender, e) =>
                {
                    device.Trigger((sender as CheckBox).Checked);
                };

            device.Connect(logicalDevice);

            return device;
        }

        public Animatroller.Framework.PhysicalDevice.DigitalInput AddDigitalInput_Momentarily(DigitalInput logicalDevice)
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
            output.LabelLightControl = AddNewLight(output.ConnectedDevice.Name);
        }

        public void Connect(INeedsRopeLight output)
        {
            output.RopeLightControl = AddNewRope(output.ConnectedDevice.Name, output.Pixels);
        }
    }
}
