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
using System.Windows.Media;
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
        private ConfigData config = new ConfigData();
        private ConfigData old_config = null;
        private CpuTemperatureReader cpuCelsius;
        private List<SensorData> sensorList;
        private ObservableCollection<SensorData> baseDataList 
            = new ObservableCollection<SensorData>();

        private static string newName = String.Empty;
        private Thread it;
        private Thread nt;
        private bool compared = true;

        static readonly string configPath = System.AppDomain.CurrentDomain.BaseDirectory + "config.xml";

        public MainWindow()
        {
            InitializeComponent();
            compared = check.IsChecked == true ? true : false;
            if(compared)
            {
                //如果存在配置
                if(System.IO.File.Exists(configPath))
                {
                    int i = 3;
                    while (old_config == null && i > 0)
                    {
                        old_config = SaveXml.ReadConfigurationFromXml(configPath);
                        i--;
                    }
                    if(old_config == null)
                    {
                        save_config.Text = "读取配置失败!";
                        save_config.Foreground = Brushes.Red;
                    }
                    else
                    {
                        save_config.Text = "读取配置成功!";
                    }
                }
                else
                {
                    compared = false;
                }
            }

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
            string biosVer = Computer.GetBios();

            config.OSVer = systemVer;
            config.OSType = systemType;
            config.Processor = cpuName;
            config.Language = language;
            config.Motherboard = boardType;
            config.BIOS = biosVer;

            this.Dispatcher.Invoke((Action)delegate ()
            {
                os.Text = systemVer;
                os_type.Text = systemType;
                cpu.Text = cpuName;
                os_language.Text = language;
                mb.Text = boardType;
                bios.Text = biosVer;
                if(compared && old_config != null)
                {
                    if (!old_config.OSVer.Equals(systemVer))
                    {
                        os.Foreground = Brushes.Red;
                    }
                    if (!old_config.OSType.Equals(systemType))
                    {
                        os_type.Foreground = Brushes.Red;
                    }
                    if (!old_config.Processor.Equals(cpuName))
                    {
                        cpu.Foreground = Brushes.Red;
                    }
                    if (!old_config.Language.Equals(language))
                    {
                        os_language.Foreground = Brushes.Red;
                    }
                    if (!old_config.Motherboard.Equals(boardType))
                    {
                        mb.Foreground = Brushes.Red;
                    }
                    if (!old_config.BIOS.Equals(biosVer))
                    {
                        bios.Foreground = Brushes.Red;
                    }
                }
                
                input_name.Text = account;
            });
        }

        void NetThread()
        {
            //内存信息获取
            List<BaseData> memList = Computer.GetMemoryInfo();
            if (memList != null && memList.Count > 0)
            {
                int i = 0;
                string[] mem = new string[memList.Count];
                foreach(BaseData data in memList)
                {
                    mem[i] = data.Name + " " + data.Size;
                    i++;
                }
                config.Memory = mem;
                //对比配置，如果不一样，给item添加check属性
                if (compared && old_config != null)
                {
                    List<BaseData> newMemList = new List<BaseData>();
                    string[] old_mem = old_config.Memory;
                    int j = 0;
                    foreach (BaseData data in memList)
                    {
                        if(!old_mem[j].Equals(mem[j]))
                        {
                            data.Check = "error";
                        }
                        j++;
                        newMemList.Add(data);
                    }
                    this.Dispatcher.Invoke((Action)delegate ()
                    {
                        mem_List.ItemsSource = newMemList;
                    });
                }
                else
                {
                    this.Dispatcher.Invoke((Action)delegate ()
                    {
                        mem_List.ItemsSource = memList;
                    });
                }
                
            }
            //磁盘信息获取
            List<BaseData> diskList = Computer.GetDiskInfo();
            if (diskList != null && diskList.Count > 0)
            {
                int i = 0;
                string[] disk = new string[diskList.Count];
                foreach (BaseData data in diskList)
                {
                    disk[i] = data.Name + " " + data.Size;
                    i++;
                }
                config.Storage = disk;
                if (compared && old_config != null)
                {
                    List<BaseData> newDiskList = new List<BaseData>();
                    string[] old_storage = old_config.Storage;
                    int j = 0;
                    foreach (BaseData data in diskList)
                    {
                        if (!old_storage[j].Equals(disk[j]))
                        {
                            data.Check = "error";
                        }
                        j++;
                        newDiskList.Add(data);
                    }
                    this.Dispatcher.Invoke((Action)delegate ()
                    {
                        disk_List.ItemsSource = newDiskList;
                    });
                }
                else
                {
                    this.Dispatcher.Invoke((Action)delegate ()
                    {
                        disk_List.ItemsSource = diskList;
                    });
                }
                
            }
            //网卡信息获取
            List<NetWorkData> netList = Computer.GetNetWorkAdpterInfo();
            if (netList != null && netList.Count > 0)
            {
                int i = 0;
                string[] net = new string[netList.Count];
                foreach (NetWorkData data in netList)
                {
                    net[i] = data.Adapter;
                    i++;
                }
                config.Network = net;
                if (compared && old_config != null)
                {
                    List<NetWorkData> newNetList = new List<NetWorkData>();
                    string[] old_network = old_config.Network;
                    int j = 0;
                    foreach (NetWorkData data in netList)
                    {
                        if (!old_network[j].Equals(net[j]))
                        {
                            data.Check = "error";
                        }
                        j++;
                        newNetList.Add(data);
                    }
                    this.Dispatcher.Invoke((Action)delegate ()
                    {
                        net_List.ItemsSource = newNetList;
                    });
                }
                else
                {
                    this.Dispatcher.Invoke((Action)delegate ()
                    {
                        net_List.ItemsSource = netList;
                    });
                }
                
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
                + DateTime.Now.Hour + DateTime.Now.Minute + num;
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

        private void SaveConfigClick(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveXml.SaveConfigurationToXml(config, configPath);
                save_config.Text = "Save Configuration success!";
            }
            catch (Exception)
            {
                save_config.Text = "Save Configuration failed!";
                save_config.Foreground = Brushes.Red;
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
