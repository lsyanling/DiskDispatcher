using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using System.Threading;
using System.Windows;
using System.Collections.ObjectModel;
using System.Net;

namespace DiskDispatcher
{
    public class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel()
        {
            SelectedIndex = 0;
            StartRunText = "开始运行";
            ButtonEnabled = true;
            ThreadSleepTime = 1000;

            InputSerial = "";
            LocationSerial = new();
            Logs = new();
        }

        public int ThreadSleepTime { get; set; }

        public int NowLocation { get; set; }

        public int Destination { get; set; }

        private int selectedIndex;

        public int SelectedIndex { get => selectedIndex; set => SetProperty(ref selectedIndex, value); }

        private string startRunText;

        public string StartRunText { get => startRunText; set => SetProperty(ref startRunText, value); }

        private string inputSerial;

        public string InputSerial { get => inputSerial; set => SetProperty(ref inputSerial, value); }

        private string startLocation;

        public string StartLocation { get => startLocation; set => SetProperty(ref startLocation, value); }

        private int findLength;

        public int FindLength { get => findLength; set => SetProperty(ref findLength, value); }

        private float averageFindLength;

        public float AverageFindLength { get => averageFindLength; set => SetProperty(ref averageFindLength, value); }

        private bool buttonEnabled;

        public bool ButtonEnabled { get => buttonEnabled; set => SetProperty(ref buttonEnabled, value); }

        public Locations LocationSerial { get; set; }

        public ObservableCollection<Log> Logs { get; set; }

        private RelayCommand randomInput;
        public ICommand RandomInput => randomInput ??= new RelayCommand(PerformRandomInput);

        private void PerformRandomInput()
        {
            Random random = new();

            if (SelectedIndex == 0)
            {
                StartLocation = random.Next(0, 10000).ToString();
            }
            else
            {
                StartLocation = random.Next(0, 10000).ToString() + " " + random.Next(0, 10000).ToString();
            }

            InputSerial = random.Next(0, 10000).ToString();
            int size = random.Next(5, 20);
            for (int i = 0; i < size; i++)
            {
                InputSerial += " " + random.Next(0, 10000).ToString();
            }
        }

        private RelayCommand startRun;
        public ICommand StartRun => startRun ??= new RelayCommand(PerformStartRun);

        private void PerformStartRun()
        {
            Thread thread;
            try
            {
                // 尚未运行
                if (StartRunText == "开始运行")
                {
                    // 清空访问序列
                    LocationSerial.Clear();
                    LocationSerial.Used = null;
                    // 重置寻道长度
                    FindLength = 0;
                    AverageFindLength = 0;
                    // 清空日志
                    Logs.Clear();

                    // 访问序列转换
                    foreach (var location in InputSerial.Split(' '))
                    {
                        int vid = int.Parse(location);
                        LocationSerial.Add(new(int.Parse(location)));
                    }

                    NowLocation = int.Parse(StartLocation.Split(' ')[0]);
                    if (SelectedIndex == 1)
                    {
                        Destination = int.Parse(StartLocation.Split(' ')[1]);
                    }

                    // 输入可用性修改
                    ButtonEnabled = false;
                    StartRunText = "跳过动画";

                    if (SelectedIndex == 0)
                    {
                        thread = new(SSTF);
                        thread.Start();
                    }
                    else
                    {
                        thread = new(SCAN);
                        thread.Start();
                    }
                }
                // 运行中
                else
                {
                    // 输入可用性修改
                    ButtonEnabled = true;
                    StartRunText = "开始运行";

                    // 设置线程速度
                    ThreadSleepTime = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SSTF()
        {
            for (int i = 0; i < LocationSerial.Count; i++)
            {
                Thread.Sleep(ThreadSleepTime);

                int nextLocation = LocationSerial.NextLocation(NowLocation);
                int distance = LocationSerial.Distance;

                FindLength += distance;
                AverageFindLength = (float)FindLength / (i + 1);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Logs.Add(new(i, NowLocation, nextLocation, distance));
                });

                NowLocation = nextLocation;
            }

            // 运行完毕
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 输入可用性修改
                ButtonEnabled = true;
                StartRunText = "开始运行";

                // 设置线程速度
                ThreadSleepTime = 1000;
            });
        }

        private void SCAN()
        {
            int direction = Destination - NowLocation;
            direction = direction != 0 ? direction : 1;
            LocationSerial.Direction = direction;

            for (int i = 0; i < LocationSerial.Count; i++)
            {
                Thread.Sleep(ThreadSleepTime);

                int nextLocation = LocationSerial.NextLocation(NowLocation, LocationSerial.Direction);
                int distance = LocationSerial.Distance;

                FindLength += distance;
                AverageFindLength = (float)FindLength / (i + 1);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Logs.Add(new(i, NowLocation, nextLocation, distance));
                });

                NowLocation = nextLocation;
            }

            // 运行完毕
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 输入可用性修改
                ButtonEnabled = true;
                StartRunText = "开始运行";

                // 设置线程速度
                ThreadSleepTime = 1000;
            });
        }

        private RelayCommand accelerateRun;
        public ICommand AccelerateRun => accelerateRun ??= new RelayCommand(PerformAccelerateRun);

        private void PerformAccelerateRun()
        {
            if (ThreadSleepTime > 400)
            {
                ThreadSleepTime -= 200;
            }
        }
    }

    public class Log : ObservableObject
    {
        public Log(int times, int nowLocation, int nextLocation, int distance)
        {
            Logs = times.ToString() + ".      当前处于磁道    " +
                nowLocation + "    下一次调度的目标磁道    " + nextLocation +
                "    寻道长度为    " + distance;
        }

        private string logs;

        public string Logs
        {
            get => logs; set => SetProperty(ref logs, value);
        }
    }

    public class Location : ObservableObject
    {
        public Location(int location)
        {
            Loc = location;
        }

        private int loc;

        public int Loc { get => loc; set => SetProperty(ref loc, value); }
    }

    public class Locations : ObservableCollection<Location>
    {
        public int NextLocation(int nowLocation)
        {
            if (Used is null)
            {
                Used = new();
                foreach (var location in this)
                {
                    Used.Add(false);
                }
            }
            NextIndex = 0;
            int nextLocation = 10000;
            int minDistance = 10000;
            for (int i = 0; i < Count; i++)
            {
                if (Used[i])
                {
                    continue;
                }
                int temp = this[i].Loc - nowLocation;
                temp = temp >= 0 ? temp : -temp;
                if (minDistance > temp)
                {
                    nextLocation = this[i].Loc;
                    minDistance = temp;
                    NextIndex = i;
                }
            }
            Distance = minDistance;
            Used[NextIndex] = true;
            return nextLocation;
        }

        public int NextLocation(int nowLocation, int direction)
        {
            if (Used is null)
            {
                Used = new();
                foreach (var location in this)
                {
                    Used.Add(false);
                }
                NextIndex = -1;
            }
            int nextI = NextIndex;
            int nextLocation = 10000;
            int minDistance = 10000;
            for (int i = 0; i < Count; i++)
            {
                if (Used[i])
                {
                    continue;
                }
                int temp = this[i].Loc - nowLocation;
                if (temp < 0)
                {
                    if (direction > 0)
                        continue;
                    if (minDistance > -temp)
                    {
                        nextLocation = this[i].Loc;
                        minDistance = -temp;
                        NextIndex = i;
                    }
                }
                if (temp >= 0)
                {
                    if (direction < 0)                  
                        continue;
                    if (minDistance > temp)
                    {
                        nextLocation = this[i].Loc;
                        minDistance = temp;
                        NextIndex = i;
                    }
                }
            }
            Distance = minDistance;
            Used[NextIndex] = true;

            // 没变
            if (nextI == NextIndex)
            {
                Direction = -Direction;
                nextLocation = NextLocation(nowLocation, Direction);
            }

            return nextLocation;
        }

        public int Direction { get; set; }

        public int NextIndex { get; set; }

        public int Distance { get; set; }

        public List<bool>? Used { get; set; }
    }
}
