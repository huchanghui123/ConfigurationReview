using QotomReview.model;
using QotomReview.Tool;
using QotomReview.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace QotomReview
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer systemTimeTimer = new DispatcherTimer() {
            IsEnabled = true
        };
        private DispatcherTimer cpuTemperatureTimer = new DispatcherTimer() {
            IsEnabled = true
        };

        private CpuTemperatureReader cpuCelsius;
        private List<SensorData> sensorList;
        private ObservableCollection<SensorData> baseDataList 
            = new ObservableCollection<SensorData>();

        private static string newName = String.Empty;
        private Thread it;
        private Thread nt;

        public MainWindow()
        {
            InitializeComponent();

            it = new Thread(InfoThread)
            {
                IsBackground = true
            };
            it.Start();

            nt = new Thread(NetThread)
            {
                IsBackground = true
            };
            nt.Start();

            systemTimeTimer.Tick += new EventHandler(SystemTimeTimerTick);
            systemTimeTimer.Interval = TimeSpan.FromSeconds(1);
            systemTimeTimer.Start();

            cpuCelsius = new CpuTemperatureReader();
            cpuTemperatureTimer.Tick += new EventHandler(CpuTemperatureTimerTick);
            cpuTemperatureTimer.Interval = TimeSpan.FromSeconds(2);
            cpuTemperatureTimer.Start();
        }

        private void SystemTimeTimerTick(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke((Action)delegate ()
            {
                time.Text = DateTime.Now.ToString();
            });
        }

        private void CpuTemperatureTimerTick(object sender, EventArgs e)
        {
            Console.WriteLine("CpuTemperatureTimerTick.....22");
            sensorList = cpuCelsius.GetTemperaturesInCelsius();
            if(sensorList.Count > 0)
            {
                if(temperatureList.Items.Count == 0)
                {
                    foreach(SensorData data in sensorList)
                    {
                        baseDataList.Add(data);
                    }
                    this.Dispatcher.Invoke((Action)delegate ()
                    {
                        temperatureList.ItemsSource = baseDataList;
                    });
                }
                else
                {
                    for (int i = 0; i < sensorList.Count; i++)
                    {
                        this.Dispatcher.Invoke((Action)delegate ()
                        {
                            baseDataList[i] = sensorList[i];
                        });
                    }
                }
            }
        }

        void InfoThread()
        {
            string systemVer = Computer.GetSystemVersion();
            string systemType = Computer.GetSystemType("SystemType");
            string cpuName = Computer.GetCpuName();
            string language = System.Threading.Thread.CurrentThread.CurrentCulture.Name + " " +
                System.Globalization.CultureInfo.InstalledUICulture.NativeName;
            string account = Computer.GetSystemType("Name");
            string boardType = Computer.GetBoardType();
            string biosName = Computer.GetBios();

            this.Dispatcher.Invoke((Action)delegate ()
            {
                os.Text = systemVer;
                os_type.Text = systemType;
                cpu.Text = cpuName;
                os_language.Text = language;
                input_name.Text = account;
                mb.Text = boardType;
                bios.Text = biosName;
                
            });
        }

        void NetThread()
        {
            List<BaseData> memList = Computer.GetMemoryInfo();
            if (memList != null && memList.Count > 0)
            {
                this.Dispatcher.Invoke((Action)delegate ()
                {
                    mem_List.ItemsSource = memList;
                });
            }

            List<BaseData> diskList = Computer.GetDiskInfo();
            if (diskList != null && diskList.Count > 0)
            {
                this.Dispatcher.Invoke((Action)delegate ()
                {
                    disk_List.ItemsSource = diskList;
                });
            }
            List<NetWorkData> netList = Computer.GetNetWorkAdpterInfo();
            if (netList != null && netList.Count > 0)
            {
                this.Dispatcher.Invoke((Action)delegate ()
                {
                    net_List.ItemsSource = netList;
                });
            }
        }

        private void UpdateTimeClick(object sender, RoutedEventArgs e)
        {
            Thread twt = new Thread(TimeWorkThread)
            {
                IsBackground = true
            };
            twt.Start();
        }

        void TimeWorkThread()
        {
            string ntp_address = String.Empty;
            this.Dispatcher.Invoke((Action)delegate ()
            {
                ntp_address = ntp_server.Text;
                systemTimeTimer.Stop();
                time.Text = "synchronizationing...";
            });
                 
            try
            {
                DateTime dt = Computer.GetNetworkTime(ntp_address);
                Console.WriteLine("UpdateTimeClick......" + dt.ToString());
                SetLocalMachineTime(dt);
                systemTimeTimer.Start();
            }
            catch (Exception)
            {
                this.Dispatcher.Invoke((Action)delegate ()
                {
                    time.Text = "synchronizationing failed,check the network!";
                });
            }
        }

        //修改本地系统时间
        public struct SYSTEMTIME
        {
            public ushort Year;
            public ushort Month;
            public ushort DayOfWeek;
            public ushort Day;
            public ushort Hour;
            public ushort Minute;
            public ushort Second;
            public ushort Millisecond;
        };

        [DllImport("kernel32.dll", EntryPoint = "GetSystemTime", SetLastError = true)]
        public extern static void Win32GetSystemTime(ref SYSTEMTIME sysTime);

        [DllImport("kernel32.dll", EntryPoint = "SetSystemTime", SetLastError = true)]
        public extern static bool Win32SetSystemTime(ref SYSTEMTIME sysTime);

        public static void SetLocalMachineTime(DateTime dt)
        {
            //转换System.DateTime到SYSTEMTIME
            SYSTEMTIME st = new SYSTEMTIME
            {
                Year = (ushort)dt.Year,
                Month = (ushort)dt.Month,
                Day = (ushort)dt.Day,
                Hour = (ushort)dt.Hour,
                Minute = (ushort)dt.Minute,
                Second = (ushort)dt.Second,
                Millisecond = (ushort)dt.Millisecond
            };
            //调用立即设置新日期和时间
            Win32SetSystemTime(ref st);
        }

        private void GenerateNameClick(object sender, RoutedEventArgs e)
        {
            Random r = new Random();
            int num = r.Next(0,999);
            newName = "QT"+DateTime.Now.ToShortDateString().Replace("/","")
                + DateTime.Now.ToShortTimeString().Replace(":","") + num;
            input_name.Text = newName;
        }

        private void UpdateNameClick(object sender, RoutedEventArgs e)
        {
            newName = input_name.Text;
            if (newName.Length == 0)
            {
                return;
            }
            // Create a new process
            ProcessStartInfo process = new ProcessStartInfo
            {
                // set name of process to "WMIC.exe"
                FileName = "WMIC.exe",
                // pass rename PC command as argument
                Arguments = "computersystem where caption='" + 
                    System.Environment.MachineName + 
                    "' rename " + newName
            };
            // Run the external process & wait for it to finish
            using (Process proc = Process.Start(process))
            {
                proc.WaitForExit();
                // print the status of command
                Console.WriteLine("Exit code = " + proc.ExitCode);
            }
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Console.WriteLine("Window OnClosing!!!");
            if (systemTimeTimer.IsEnabled)
            {
                systemTimeTimer.Stop();
            }
            if (cpuTemperatureTimer.IsEnabled)
            {
                cpuTemperatureTimer.Stop();
            }
            if(cpuCelsius != null)
            {
                cpuCelsius.Dispose();
            }
        }
    }
}
