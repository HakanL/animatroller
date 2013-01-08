using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Animatroller.Framework.Effect
{
    public class Sweeper
    {
        public delegate void PerformAction(double zeroToOne, double negativeOneToOne, double oneToZeroToOne, bool forced);

        private object lockObject = new object();
        private object lockJobs = new object();
        private Timer timer;
        private int index1;
        private int index2;
        private int index3;
        private int positions;
        private List<PerformAction> jobs;
        private double[] dataValues1;
        private double[] dataValues2;
        private double[] dataValues3;
        private TimeSpan interval;
        private bool oneShot;
        private int hitCounter;

        public Sweeper(TimeSpan duration, int dataPoints, bool startRunning)
        {
            if (dataPoints < 2)
                throw new ArgumentOutOfRangeException("dataPoints");

            dataValues1 = new double[dataPoints];
            dataValues2 = new double[dataPoints];
            dataValues3 = new double[dataPoints];

            for (int i = 0; i < dataPoints; i++)
            {
                dataValues1[i] = i / (double)(dataPoints - 1);

                if (i < (dataPoints / 2))
                    dataValues2[i] = 1 - 4 * i / (double)dataPoints;
                else
                    dataValues2[i] = -1 + 4 * (i - dataPoints / 2) / (double)dataPoints;

                dataValues3[i] = Math.Abs(1 - 2 * i / (double)dataPoints);
            }

            this.index1 = 0;
            this.index2 = dataPoints / 4;
            this.index3 = dataPoints / 2;
            this.positions = dataPoints - 1;
            this.jobs = new List<PerformAction>();
            this.timer = new Timer(new TimerCallback(TimerCallback));

            this.interval = new TimeSpan(duration.Ticks / dataPoints);
            Console.WriteLine(string.Format("Interval {0:N1} ms", this.interval.TotalMilliseconds));

            if(startRunning)
                Resume();
        }

        public Sweeper OneShot()
        {
            this.oneShot = true;

            return this;
        }

        public Sweeper Pause()
        {
            this.timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

            return this;
        }

        public Sweeper Resume()
        {
            this.timer.Change(TimeSpan.FromMilliseconds(0), this.interval);

            return this;
        }

        public Sweeper Reset()
        {
            this.hitCounter = 0;
            this.index1 = 0;
            this.index2 = positions / 4;
            this.index3 = positions / 2;

            Resume();

            return this;
        }

        public Sweeper ForceValue(double zeroToOne, double negativeOneToOne, double oneToZeroToOne)
        {
            lock (lockJobs)
            {
                foreach (var job in jobs)
                    job(zeroToOne, negativeOneToOne, oneToZeroToOne, true);
            }

            return this;
        }

        public Sweeper RegisterJob(PerformAction job)
        {
            lock (lockJobs)
            {
                jobs.Add(job);
            }

            return this;
        }

        private void TimerCallback(object state)
        {
            if (Monitor.TryEnter(lockJobs))
            {
                try
                {
                    Task task = new Task(() =>
                        {
                            foreach (var job in jobs)
                                job(dataValues1[index1], dataValues2[index2], dataValues3[index3], false);
                        });
                    task.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception in Sweeper job" + ex.ToString());
                }
                finally
                {
                    Monitor.Exit(lockJobs);
                }
            }
            else
                Console.WriteLine("Missed execute task in Sweeper job");

            lock (lockObject)
            {
                if (++index1 > positions)
                    index1 = 0;
                if (++index2 > positions)
                    index2 = 0;
                if (++index3 > positions)
                    index3 = 0;

                if (hitCounter++ >= positions && this.oneShot)
                    Pause();
            }
        }
    }
}
