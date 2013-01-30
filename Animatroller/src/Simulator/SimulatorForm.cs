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

namespace Animatroller.Simulator
{
    public partial class SimulatorForm : Form, IPort
    {
        public SimulatorForm()
        {
            InitializeComponent();
        }

        public SimulatorForm AutoWireUsingReflection(IScene scene)
        {
            var fields = scene.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                // Auto-wire
                if (field.FieldType == typeof(Dimmer))
                    this.Connect(new Animatroller.Simulator.TestLight((Dimmer)field.GetValue(scene)));
                else if (field.FieldType == typeof(ColorDimmer))
                    this.Connect(new Animatroller.Simulator.TestLight((ColorDimmer)field.GetValue(scene)));
                else if (field.FieldType == typeof(StrobeDimmer))
                    this.Connect(new Animatroller.Simulator.TestLight((StrobeDimmer)field.GetValue(scene)));
                else if (field.FieldType == typeof(StrobeColorDimmer))
                    this.Connect(new Animatroller.Simulator.TestLight((StrobeColorDimmer)field.GetValue(scene)));
                else if (field.FieldType == typeof(Pixel1D))
                    this.Connect(new Animatroller.Simulator.TestPixel1D((Pixel1D)field.GetValue(scene)));
                else
                    if (field.FieldType == typeof(Switch))
                        this.AddDigitalOutput((Switch)field.GetValue(scene));
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

        public Animatroller.Framework.PhysicalDevice.DigitalInput AddDigitalInput_FlipFlop(string name)
        {
            var control = new CheckBox();
            control.Text = name;
            control.Size = new System.Drawing.Size(80, 80);

            flowLayoutPanelLights.Controls.Add(control);

            var device = new Animatroller.Framework.PhysicalDevice.DigitalInput();

            control.CheckedChanged += (sender, e) =>
                {
                    device.Trigger((sender as CheckBox).Checked);
                };

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
