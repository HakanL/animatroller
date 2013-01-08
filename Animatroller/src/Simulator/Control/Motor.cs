using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Animatroller.Simulator.Extensions;

namespace Animatroller.Simulator.Control
{
    public partial class Motor : UserControl
    {
        private System.Threading.Timer timer;
        private double speed;
        private double currentSpeed;
        private double currentPosition;
        private int targetPosition;
        private Action<int?, bool> trigger;
        private DateTime startTime;
        private TimeSpan timeout;
        private bool motorFailed;

        public Motor()
        {
            InitializeComponent();

            this.timer = new System.Threading.Timer(new System.Threading.TimerCallback(timer_Tick), null,
                System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

            CurrentPosition = 0;
            this.motorFailed = false;
        }

        public Action<int?, bool> Trigger
        {
            private get { return this.trigger; }
            set { this.trigger = value; }
        }

        private void UpdateTrackbar()
        {
            this.UIThread(delegate
            {
                if (Target > CurrentPosition)
                    trackBarMotor.Value = (int)(currentSpeed * 10);
                else if (Target < CurrentPosition)
                    trackBarMotor.Value = (int)(-currentSpeed * 10);
                else
                    trackBarMotor.Value = 0;
            });
        }

        public TimeSpan Timeout
        {
            get { return this.timeout; }
            set { this.timeout = value; }
        }

        public double Speed
        {
            get { return speed; }
            set
            {
                this.speed = value;
                this.currentSpeed = 0;

                UpdateTrackbar();

                if (Target != CurrentPosition && Speed != 0)
                    StartMotor();
            }
        }

        private void StartMotor()
        {
            timer.Change(100, 100);
            startTime = DateTime.Now;
        }

        public int Target
        {
            get { return targetPosition; }
            set
            {
                this.targetPosition = value;

                UpdateTrackbar();

                if (Target != CurrentPosition && Speed != 0)
                    StartMotor();
            }
        }

        public double CurrentPosition
        {
            get { return currentPosition; }
            private set
            {
                currentPosition = value;
                this.UIThread(delegate
                {
                    labelMotorPos.Text = currentPosition.ToString("F0");
                });
            }
        }

        private void timer_Tick(object state)
        {
            if (Monitor.TryEnter(timer))
            {
                try
                {
                    if (Speed != 0 && !motorFailed)
                    {
                        if ((DateTime.Now - this.startTime) > Timeout)
                        {
                            motorFailed = true;
                            Speed = 0;
                            Trigger((int)CurrentPosition, motorFailed);
                            timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                            return;
                        }

                        double distance = Math.Abs(Target - CurrentPosition);
                        if (distance <= 2)
                        {
                            // Stop
                            Speed = 0;
                            Trigger((int)CurrentPosition, motorFailed);
                            timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                        }
                        else
                        {
                            // Ramp up/down
                            if (distance < 10)
                            {
                                // Slow down
                                if (currentSpeed > 0.1)
                                {
                                    currentSpeed -= 0.1;
                                    UpdateTrackbar();
                                }
                            }
                            else
                            {
                                // Speed up
                                if (currentSpeed < Speed)
                                {
                                    currentSpeed += 0.1;
                                    UpdateTrackbar();
                                }
                            }

                            if (Target < CurrentPosition)
                                CurrentPosition -= currentSpeed * 5;
                            else if (Target > CurrentPosition)
                                CurrentPosition += currentSpeed * 5;
                        }

                    }
                }
                finally
                {
                    Monitor.Exit(timer);
                }
            }
        }
    }
}
