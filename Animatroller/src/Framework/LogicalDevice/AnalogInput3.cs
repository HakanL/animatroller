using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Animatroller.Framework.LogicalDevice.Event;
using System.Linq.Expressions;
using System.Reflection;

namespace Animatroller.Framework.LogicalDevice
{
    public class AnalogInput3 : BaseDevice, ISupportsPersistence
    {
        protected double currentValue;
        protected ISubject<double> control;
        protected ISubject<double> outputValue;

        public AnalogInput3(bool persistState = false, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name, persistState)
        {
            this.outputValue = new Subject<double>();
            this.control = new Subject<double>();

            this.control.Subscribe(x =>
                {
                    if (this.currentValue != x)
                    {
                        this.currentValue = x;

                        UpdateOutput();
                    }
                });
        }

        public void SetValueFromPersistence(Func<string, string, string> getKeyFunc)
        {
            double.TryParse(getKeyFunc("input", "0.0"), out this.currentValue);
        }

        public void SaveValueToPersistence(Action<string, string> setKeyFunc)
        {
            setKeyFunc("input", this.currentValue.ToString());
        }

        public bool PersistState
        {
            get { return this.persistState; }
        }

        public ISubject<double> Control
        {
            get
            {
                return this.control;
            }
        }

        public IObservable<double> Output
        {
            get
            {
                return this.outputValue;
            }
        }

        public void ConnectTo(ISubject<double> component)
        {
            this.outputValue.Subscribe(component);
        }

        public void ConnectTo(Action<double> component)
        {
            this.outputValue.Subscribe(component);
        }

        public void ConnectTo(IReceivesBrightness device)
        {
            Expression<Func<IReceivesBrightness, double>> expr = _ => device.Brightness;

            var memberSelectorExpression = expr.Body as MemberExpression;
            if (memberSelectorExpression != null)
            {
                var property = memberSelectorExpression.Member as PropertyInfo;
                if (property != null)
                {
                    this.outputValue.Subscribe(x => property.SetValue(device, x, null));
                }
            }
        }

        public double Value
        {
            get { return this.currentValue; }
            set
            {
                this.currentValue = value;

                UpdateOutput();
            }
        }

        protected override void UpdateOutput()
        {
            this.outputValue.OnNext(this.currentValue);
        }

        public void WhenOutputChanges(Action<double> action)
        {
            Output.Subscribe(action);
        }
    }
}
