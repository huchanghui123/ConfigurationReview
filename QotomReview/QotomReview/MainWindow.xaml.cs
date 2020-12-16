using QotomReview.model;
using QotomReview.Tool;
using QotomReview.Tools;
using QotomReview.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Ports;
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
        private Thread read;
        private bool compared = true;

        //private int baudRate = 115200;
        //private int dataBits = 8;
        //private String stopBits = "One";
        //private String parity = "None";
        //private String handshake = "None";

        private ComWindow comWindow;

        static readonly string configPath = System.AppDomain.CurrentDomain.BaseDirectory + "config.xml";

        public MainWindow()
        {
            InitializeComponent();

            it = new Thread(InfoThread)
            {
                IsBackground = true
            };
            //it.Start();

            nt = new Thread(NetThread)
            {
                IsBackground = true
            };
            //nt.Start();

            compared = check.IsChecked == true ? true : false;
            if (compared)
            {
                read = new Thread(ReadConfig)
                {
                    IsBackground = true
                };
                read.Start();
                //ReadConfig(configPath);
            }
            else
            {
                it.Start();
                nt.Start();
            }

            systemTimeTimer.Tick += new EventHandler(SystemTimeTimerTick);
            systemTimeTimer.Interval = TimeSpan.FromSeconds(1);
            systemTimeTimer.Start();

            cpuCelsius = new CpuTemperatureReader();
            cpuTemperatureTimer.Tick += new EventHandler(CpuTemperatureTimerTick);
            cpuTemperatureTimer.Interval = TimeSpan.FromSeconds(2);
            cpuTemperatureTimer.Start();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            portName.ItemsSource = SerialPort.GetPortNames();
            portName.SelectedIndex = 0;
            string[] baudDatas = { "9600", "19200", "38400", "56000", "57600", "115200", "128000" };
            baudRate.ItemsSource = baudDatas;
            baudRate.SelectedIndex = 5;
            string[] stopBitData = { StopBits.One + "", StopBits.Two + "", StopBits.OnePointFive + "" };
            stopBits.ItemsSource = stopBitData;
            stopBits.SelectedIndex = 0;
            string[] parityData = { Parity.None + "", Parity.Odd + "", Parity.Even + "", Parity.Mark + "", Parity.Space + "" };
            parity.ItemsSource = parityData;
            parity.SelectedIndex = 0;
            string[] handShakeData = { Handshake.None + "", Handshake.XOnXOff + "", Handshake.RequestToSend + "", Handshake.RequestToSendXOnXOff + "" };
            handShake.ItemsSource = handShakeData;
            handShake.SelectedIndex = 0;
        }

        void ReadConfig()
        {
            //如果存在配置
            if (System.IO.File.Exists(configPath))
            {
                old_config = SaveXml.ReadConfigurationFromXml(configPath);
                this.Dispatcher.Invoke((Action)delegate ()
                {
                    save_config.Text = "读取中...";
                    if (old_config == null)
                    {
                        save_config.Text = "读取配置失败!";
                        save_config.Foreground = Brushes.Red;
                    }
                    else
                    {
                        save_config.Text = "读取配置成功!";
                        save_config.Foreground = Brushes.Black;
                    }
                });
            }
            else
            {
                this.Dispatcher.Invoke((Action)delegate ()
                {
                    save_config.Text = "找不到配置!";
                    save_config.Foreground = Brushes.Red;
                });
                compared = false;
            }
            //Console.WriteLine(it.ThreadState+"-----------"+ nt.ThreadState);
            if (it.ThreadState != System.Threading.ThreadState.Stopped)
            {
                it.Start();
            }
            else
            {
                it = new Thread(InfoThread)
                {
                    IsBackground = true
                };
                it.Start();
            }
            if (nt.ThreadState != System.Threading.ThreadState.Stopped)
            {
                nt.Start();
            }
            else
            {
                nt = new Thread(NetThread)
                {
                    IsBackground = true
                };
                nt.Start();
            }
        }

        private void SystemTimeTimerTick(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke((Action)delegate ()
            {
                local_time.Text = DateTime.Now.ToString();
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
            Console.WriteLine("InfoThread.....");
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
                os.Foreground = Brushes.Black;
                os_type.Foreground = Brushes.Black;
                cpu.Foreground = Brushes.Black;
                os_language.Foreground = Brushes.Black;
                mb.Foreground = Brushes.Black;
                bios.Foreground = Brushes.Black;

                if (compared && old_config != null)
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
            Console.WriteLine("NetThread.....");
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
                        if (old_mem.Length != memList.Count)
                        {
                            data.Check = "error";
                        }
                        else
                        {
                            if (!old_mem[j].Equals(mem[j]))
                            {
                                data.Check = "error";
                            }
                            j++;
                        }
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
                        if (old_storage.Length != diskList.Count)
                        {
                            data.Check = "error";
                        }
                        else
                        {
                            if (!old_storage[j].Equals(disk[j]))
                            {
                                data.Check = "error";
                            }
                            j++;
                        }
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
                        if (old_network.Length != netList.Count)
                        {
                            data.Check = "error";
                        }
                        else
                        {
                            if (!old_network[j].Equals(net[j]))
                            {
                                data.Check = "error";
                            }
                            j++;
                        }
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
                local_time.Text = "synchronizationing...";
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
                    local_time.Text = "synchronizationing failed!";
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
            newName = "QT"+DateTime.Now.ToShortDateString()
                .Replace("/","").Replace("-", "").Replace(".", "")
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
                if(proc.ExitCode == 0)
                {
                    reboot.Text = "更新成功，重启生效！";
                }
            }
        }

        private void SaveConfigClick(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveXml.SaveConfigurationToXml(config, configPath);
                save_config.Text = "保存当前配置成功!";
            }
            catch (Exception)
            {
                save_config.Text = "保存当前配置失败!";
                save_config.Foreground = Brushes.Red;
            }
        }

        private void DeleteConfigClick(object sender, RoutedEventArgs e)
        {
            //如果存在配置
            if (System.IO.File.Exists(configPath))
            {
                System.IO.File.Delete(configPath);
                save_config.Text = "配置已删除!";
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

        private void My_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            reload.BorderBrush = Brushes.Red;
            reload.BorderThickness = new Thickness(2.0);
            reload.Opacity = 0.5;
        }

        private void My_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            reload.BorderBrush = Brushes.Black;
            reload.BorderThickness = new Thickness(1.0);
            reload.Opacity = 1;
        }

        //快捷修改时间和计算机名
        private void Fast_Click(object sender, RoutedEventArgs e)
        {
            UpdateTimeClick(update_time, new RoutedEventArgs());
            GenerateNameClick(generate_name, new RoutedEventArgs());
            UpdateNameClick(update_name, new RoutedEventArgs());
        }

        //强制更新配置信息
        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            //清空配置，重新读取
            save_config.Text = "读取中...";
            old_config = null;
            compared = check.IsChecked == true ? true : false;
            ReadConfig();
        }

        private void OpenAudioClick(object sender, RoutedEventArgs e)
        {
            Process.Start("mmsys.cpl");
        }

        private void OpenSerialPort(object sender, RoutedEventArgs e)
        {
            comWindow = new ComWindow(portName.Text, baudRate.Text, dataBits.Text,
                stopBits.Text, parity.Text, handShake.Text)
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            comWindow.Closed += new EventHandler(ComWindowClosed);
            open_com.IsEnabled = false;
            open_all_com.IsEnabled = false;
            comWindow.Show();
        }

        private void OpenAllSerialPort(object sender, RoutedEventArgs e)
        {
            AllComWindow w1 = new AllComWindow(baudRate.Text, dataBits.Text,
                stopBits.Text, parity.Text, handShake.Text)
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            w1.Closed += new EventHandler(ComWindowClosed);
            open_com.IsEnabled = false;
            open_all_com.IsEnabled = false;
            w1.Show();
        }

        private void ComWindowClosed(object sender, EventArgs e)
        {
            open_com.IsEnabled = true;
            open_all_com.IsEnabled = true;
        }
    }
}
