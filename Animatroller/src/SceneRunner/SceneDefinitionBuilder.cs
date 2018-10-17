using System;
using System.Collections.Generic;
using System.Linq;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.SceneRunner
{
    public class SceneDefinitionBuilder
    {
        public (AdminMessage.SceneDefinition SceneDefinition, IList<SendControls.ISendControl> SendControls) AutoWireUsingReflection(
            IScene scene,
            Action updateAvailable,
            params IRunningDevice[] excludeDevices)
        {
            var definition = new AdminMessage.SceneDefinition
            {
                Name = scene.GetType().Name,
                Components = new List<AdminMessage.SceneComponent>()
            };

            var sendControls = new List<SendControls.ISendControl>();

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

                string componentName = null;
                if (fieldValue is IDevice device)
                    componentName = device.Name;

                void addComponent(string id, string name, SendControls.ISendControl sendControl)
                {
                    definition.Components.Add(new AdminMessage.SceneComponent
                    {
                        Id = id,
                        Name = name,
                        Type = sendControl.ComponentType
                    });

                    sendControls.Add(sendControl);
                };

                switch (fieldValue)
                {
                    case StrobeColorDimmer3 instance:
                        addComponent(field.Name, componentName, new SendControls.LightSendControl(instance, field.Name, updateAvailable));
                        break;

                    case StrobeDimmer3 instance:
                        addComponent(field.Name, componentName, new SendControls.LightSendControl(instance, field.Name, updateAvailable));
                        break;

                    case ColorDimmer3 instance:
                        addComponent(field.Name, componentName, new SendControls.LightSendControl(instance, field.Name, updateAvailable));
                        break;

                    case Dimmer3 instance:
                        addComponent(field.Name, componentName, new SendControls.LightSendControl(instance, field.Name, updateAvailable));
                        break;
                }

                //if (field.FieldType == typeof(Dimmer3))
                //{
                //    definition.Components.Add(new AdminMessage.SceneComponent
                //    {
                //        Id = field.Name,
                //        Name = componentName,
                //        Type = AdminMessage.ComponentType.StrobeColorDimmer
                //    });

                //    var sendControl = new SendControls.LightSendControl((Dimmer3)fieldValue, field.Name, updateAvailable);
                //    sendControls.Add(sendControl);
                //}


                //                    Connect(new Animatroller.Simulator.TestLight(this, (Dimmer3)fieldValue));
                /*                else if (field.FieldType == typeof(ColorDimmer3))
                                    this.Connect(new Animatroller.Simulator.TestLight(this, (ColorDimmer3)fieldValue));
                                else if (field.FieldType == typeof(StrobeColorDimmer3))
                                    this.Connect(new Animatroller.Simulator.TestLight(this, (StrobeColorDimmer3)fieldValue));
                                else if (field.FieldType == typeof(StrobeDimmer3))
                                    this.Connect(new Animatroller.Simulator.TestLight(this, (StrobeDimmer3)fieldValue));
                                else if (field.FieldType == typeof(MovingHead))
                                    this.Connect(new Animatroller.Simulator.TestLight(this, (MovingHead)fieldValue));
                                //else if (field.FieldType == typeof(Pixel1D))
                                //    this.Connect(new Animatroller.Simulator.TestPixel1D((Pixel1D)fieldValue));
                                //else if (field.FieldType == typeof(Pixel1D))
                                //    this.Connect(new Animatroller.Simulator.TestPixel1D((Pixel1D)fieldValue));
                                //else if (field.FieldType == typeof(VirtualPixel1D2))
                                //    this.Connect(new Animatroller.Simulator.TestPixel1D((VirtualPixel1D2)fieldValue));
                                else if (field.FieldType == typeof(VirtualPixel1D3))
                                    this.Connect(new Animatroller.Simulator.TestPixel1D(this, (VirtualPixel1D3)fieldValue));
                                else if (field.FieldType == typeof(VirtualPixel2D3))
                                    this.Connect(new Animatroller.Simulator.TestPixel2D(this, (VirtualPixel2D3)fieldValue));
                                //else if (field.FieldType == typeof(VirtualPixel2D))
                                //    this.Connect(new Animatroller.Simulator.TestPixel2D((VirtualPixel2D)fieldValue));
                                else if (field.FieldType == typeof(AnalogInput3))
                                    this.AddAnalogInput((AnalogInput3)fieldValue);
                                else if (field.FieldType == typeof(MotorWithFeedback))
                                {
                                    // Skip
                                    //                    this.AddMotor((MotorWithFeedback)fieldValue);
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
                                else if (typeof(DigitalOutput2).IsAssignableFrom(field.FieldType))
                                {
                                    this.AddDigitalOutput((DigitalOutput2)fieldValue);
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
                                else if (field.FieldType == typeof(OperatingHours2))
                                {
                                    // Skip
                                }
                                else if (field.FieldType == typeof(Animatroller.Framework.Controller.Subroutine))
                                {
                                    // Skip
                                }
                                else if (field.FieldType == typeof(Animatroller.Framework.LogicalDevice.VideoPlayer))
                                {
                                    // Skip
                                }
                                else if (field.FieldType.Name.StartsWith("EnumStateMachine") ||
                                    field.FieldType.Name.StartsWith("IntStateMachine") ||
                                    field.FieldType.Name.StartsWith("Timeline"))
                                {
                                    // Skip
                                }
                                else if (field.FieldType.FullName.StartsWith("Animatroller.Framework.Effect."))
                                {
                                    // Skip
                                }
                                else if (field.FieldType.FullName.StartsWith("Animatroller.Framework.Import."))
                                {
                                    // Skip
                                }
                                else
                                {
                                    this.log.Verbose("Unknown field {0}", field.FieldType);
                                }*/
            }

            return (definition, sendControls);
        }

        public void AutoWireUsingReflection_Simple(IScene scene, params IRunningDevice[] excludeDevices)
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

                /*                else if (field.FieldType.Name.StartsWith("EnumStateMachine") ||
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
                                        if (PendingClose)
                                            return;

                                        this.UIThread(delegate
                                        {
                                            if (string.IsNullOrEmpty(e.NewState))
                                                control.Text = "<idle>";
                                            else
                                                control.Text = e.NewState;
                                        });
                                    };
                                }*/
                //FIXME
                //else if (field.FieldType == typeof(Animatroller.Framework.Controller.CueList))
                //{
                //    var cueList = (Animatroller.Framework.Controller.CueList)fieldValue;

                //    var control = AddLabel(cueList.Name);

                //    cueList.CurrentCueId.Subscribe(x =>
                //        {
                //            this.UIThread(delegate
                //            {
                //                if (x.HasValue)
                //                    control.Text = x.ToString();
                //                else
                //                    control.Text = "<idle>";
                //            });
                //        });
                //}
            }
        }
    }
}
