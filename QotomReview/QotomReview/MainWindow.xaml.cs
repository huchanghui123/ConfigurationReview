using QotomReview.model;
using QotomReview.Tool;
using QotomReview.Tools;
using QotomReview.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
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
        private ComWindow comWindow;
        private bool comIsStartUp = false;
        private bool audioIsStartUp = false;

        public const int WM_DEVICECHANGE = 0x219;               //系统硬件改变发出的系统消息
        public const int DBT_DEVICEARRIVAL = 0x8000;            //系统检测到设备已经插入，并且已经处于可用转态
        public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;     //系统检测到设备已经卸载或者拔出

        private string os_name = "";
        private string os_version = "";

        static readonly string configPath = System.AppDomain.CurrentDomain.BaseDirectory + "qotom_config.xml";
        static readonly string iniPath = System.AppDomain.CurrentDomain.BaseDirectory + "qotom_review.ini";

        public MainWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

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
            
            if(!File.Exists(iniPath))
            {
                //Compare:0不对比，1对比；startup:0不启动，1启动
                Computer.Writeini("Compare", "Compare", "1",iniPath);
                Computer.Writeini("COM","startup", "0",iniPath);
                Computer.Writeini("Audio","startup", "0",iniPath);
            }
            else
            {
                string compareValue = Computer.Readini("Compare", "Compare", "1", iniPath);
                string comValue = Computer.Readini("COM", "startup", "0", iniPath);
                string audioValue = Computer.Readini("Audio", "startup", "0", iniPath);
                compared = Convert.ToInt16(compareValue) == 0 ? false : true;
                comIsStartUp = Convert.ToInt16(comValue) == 0 ? false : true;
                audioIsStartUp = Convert.ToInt16(audioValue) == 0 ? false : true;
                if(compared)
                {
                    check.IsChecked = compared;
                }
                if(comIsStartUp)
                {
                    com_start.IsChecked = comIsStartUp;
                }
                if(audioIsStartUp)
                {
                    audio_start.IsChecked = audioIsStartUp;
                }

                //Console.WriteLine("compareValue:{0}, compared:{1}, comValue:{2}, comIsStartUp:{3}, audioValue:{4}, audioIsStartUp:{5}",
                //    compareValue, compared, comValue, comIsStartUp, audioValue, audioIsStartUp);
            }
            if (compared)
            {
                read = new Thread(ReadConfig)
                {
                    IsBackground = true
                };
                read.Start();
            }
            else
            {
                it.Start();
                nt.Start();
            }

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            font_family.ItemsSource = Computer.LoadSysFontFamily();
            font_family.SelectedIndex = 0;
            string[] fontsizes = {"12", "13", "14", "15", "16", "17", "18", "19", "20"};
            font_size.ItemsSource = fontsizes;
            font_size.SelectedIndex = 3;

            portName.ItemsSource = SerialPort.GetPortNames();
            portName.SelectedIndex = 0;
            if(SerialPort.GetPortNames().Length < 1)
            {
                open_com.IsEnabled = false;
                open_all_com.IsEnabled = false;
            }
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

            //用于监听Windows消息 
            //注意获取窗口句柄一定要写在窗口loaded事件里，才能获取到窗口句柄，否则为空
            //窗口过程
            if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
            {
                hwndSource.AddHook(new HwndSourceHook(DeveiceChanged));  //挂钩
            }

            systemTimeTimer.Tick += new EventHandler(SystemTimeTimerTick);
            systemTimeTimer.Interval = TimeSpan.FromSeconds(1);
            systemTimeTimer.Start();

            cpuCelsius = new CpuTemperatureReader();
            cpuTemperatureTimer.Tick += new EventHandler(CpuTemperatureTimerTick);
            cpuTemperatureTimer.Interval = TimeSpan.FromSeconds(2);
            cpuTemperatureTimer.Start();

            if(comIsStartUp && open_all_com.IsEnabled)
            {
                OpenAllSerialPort(open_all_com, new RoutedEventArgs());
            }
            if(audioIsStartUp)
            {
                OpenAudioClick(open_audio, new RoutedEventArgs());
            }
        }
        //设备插拔改变函数
        private IntPtr DeveiceChanged(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            //Windows系统消息类型:设备改变
            if (msg == WM_DEVICECHANGE)
            {
                //获取具体的设备事件类型
                switch (wParam.ToInt32())
                {
                    //U盘插入事件
                    case DBT_DEVICEARRIVAL:
                        DriveInfo[] s = DriveInfo.GetDrives();
                        foreach (DriveInfo drive in s)
                        {
                            if (drive.DriveType == DriveType.Removable)
                            {
                                //Console.WriteLine(string.Format("U盘已插入，盘符是" + drive.Name.ToString()));
                                Reload_Click(reload, new RoutedEventArgs());
                                break;
                            }
                        }
                        break;
                    //设备卸载事件
                    case DBT_DEVICEREMOVECOMPLETE:
                        //Console.WriteLine("检测到设备卸载");
                        Reload_Click(reload, new RoutedEventArgs());
                        break;
                }
            }
            return IntPtr.Zero;
        }


        void ReadConfig()
        {
            //如果存在配置
            if (System.IO.File.Exists(configPath))
            {
                old_config = SaveXml.ReadConfigurationFromXml(configPath);
                this.Dispatcher.Invoke((Action)delegate ()
                {
                    status_info.Text = "读取中...";
                    if (old_config == null)
                    {
                        status_info.Text = "读取配置失败!";
                        status_info.Foreground = Brushes.Red;
                    }
                    else
                    {
                        status_info.Text = "读取配置成功!";
                        status_info.Foreground = Brushes.Black;
                    }
                });
            }
            else
            {
                this.Dispatcher.Invoke((Action)delegate ()
                {
                    status_info.Text = "找不到配置!";
                    status_info.Foreground = Brushes.Red;
                });
                compared = false;
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
            os_name = systemVer;
            os_version = systemType;
            string cpuName = Computer.GetCpuName();
            string language = System.Threading.Thread.CurrentThread.CurrentCulture.Name + " " +
                System.Globalization.CultureInfo.InstalledUICulture.NativeName;
            string account = Computer.GetSystemType("Name");
            //string boardType = Computer.GetBoardType();
            string boardType = Computer.GetVieoController();
            string biosVer = Computer.GetBios();

            config.OSVer = systemVer;
            config.OSType = systemType;
            config.Processor = cpuName;
            config.Language = language;
            config.VideoController = boardType;
            config.BIOS = biosVer;

            this.Dispatcher.Invoke((Action)delegate ()
            {
                os.Text = systemVer;
                os_type.Text = systemType;
                cpu.Text = cpuName;
                os_language.Text = language;
                video.Text = boardType;
                bios.Text = biosVer;
                os.Foreground = Brushes.Black;
                os_type.Foreground = Brushes.Black;
                cpu.Foreground = Brushes.Black;
                os_language.Foreground = Brushes.Black;
                video.Foreground = Brushes.Black;
                bios.Foreground = Brushes.Black;
                if(boardType.Contains("Microsoft"))
                {
                    video.FontWeight = FontWeights.Bold;
                    os.Foreground = Brushes.Red;
                }
                if (compared && old_config != null)
                {
                    try
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
                        if (!old_config.VideoController.Equals(boardType))
                        {
                            video.Foreground = Brushes.Red;
                        }
                        if (!old_config.BIOS.Equals(biosVer))
                        {
                            bios.Foreground = Brushes.Red;
                        }
                    }
                    catch (Exception)
                    {

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
                    //string[] old_mem = old_config.Memory;
                    List<String> old_mem = old_config.Memory.ToList();
                    int j = 0;
                    foreach (BaseData data in memList)
                    {
                        if (old_mem.Count != memList.Count)
                        {
                            data.Check = "error";
                        }
                        else
                        {
                            //内存就算有多条，一般也是同型号的，这里不能用Indexof
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
            //USB信息获取
            List<BaseData> usbList = Computer.GetUSBInfo();
            //if (usbList != null && usbList.Count > 0)
            //{
                this.Dispatcher.Invoke((Action)delegate ()
                {
                    usb_list.ItemsSource = usbList;
                });
            //}

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
                    List<String> old_storage = old_config.Storage.ToList();
                    int j = 0;
                    foreach (BaseData data in diskList)
                    {
                        if (old_storage.Count != diskList.Count)
                        {
                            data.Check = "error";
                        }
                        else
                        {
                            //有时候可能读取出的排序不同，这里改用indexOf
                            if (old_storage.IndexOf(disk[j]) < 0)
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
                    List<String> old_network = old_config.Network.ToList();
                    foreach (NetWorkData data in netList)
                    {
                        if (old_network.Count != netList.Count)
                        {
                            data.Check = "error";
                        }
                        else
                        {
                            if (old_network.IndexOf(data.Adapter) < 0)
                            {
                                data.Check = "error";
                            }
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
                status_info.Text = "保存当前配置成功!";
            }
            catch (Exception)
            {
                status_info.Text = "保存当前配置失败!";
                status_info.Foreground = Brushes.Red;
            }
        }

        private void DeleteConfigClick(object sender, RoutedEventArgs e)
        {
            //如果存在配置
            if (System.IO.File.Exists(configPath))
            {
                System.IO.File.Delete(configPath);
                status_info.Text = "配置已删除!";
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
            reload.BorderBrush = Brushes.Gray;
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
            Console.WriteLine("Reload_Click..............");
            //清空配置，重新读取
            status_info.Text = "读取中...";
            old_config = null;
            string compareValue = Computer.Readini("Compare", "Compare", "1", iniPath);
            compared = Convert.ToInt16(compareValue) == 0 ? false : true;
            ReadConfig();
        }

        private void OpenAudioClick(object sender, RoutedEventArgs e)
        {
            Process.Start("mmsys.cpl");
        }

        private void OpenSysteminfoClick(object sender, RoutedEventArgs e)
        {
            Process.Start("msinfo32.exe");
        }

        private void OpenSerialPort(object sender, RoutedEventArgs e)
        {
            comWindow = new ComWindow(portName.Text, baudRate.Text, dataBits.Text,
                stopBits.Text, parity.Text, handShake.Text)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
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
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = 10,
                Top = 10
            };
            w1.Closed += new EventHandler(ComWindowClosed);
            open_com.IsEnabled = false;
            open_all_com.IsEnabled = false;
            w1.Show();
        }

        private void ComWindowClosed(object sender, EventArgs e)
        {
            if (SerialPort.GetPortNames().Length >= 1)
            {
                open_com.IsEnabled = true;
                open_all_com.IsEnabled = true;
            }
        }

        private void Audio_start_Click(object sender, RoutedEventArgs e)
        {
            if(audio_start.IsChecked == false)
            {
                Computer.Writeini("Audio", "startup", "0", iniPath);
            }
            else
            {
                Computer.Writeini("Audio", "startup", "1", iniPath);
            }
        }

        private void Com_start_Click(object sender, RoutedEventArgs e)
        {
            if (com_start.IsChecked == false)
            {
                Computer.Writeini("COM", "startup", "0", iniPath);
            }
            else
            {
                Computer.Writeini("COM", "startup", "1", iniPath);
            }
        }

        private void CompareClick(object sender, RoutedEventArgs e)
        {
            if(check.IsChecked == false)
            {
                Computer.Writeini("Compare", "Compare", "0", iniPath);
            }
            else
            {
                Computer.Writeini("Compare", "Compare", "1", iniPath);
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aw = new AboutWindow(os_name, os_version)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            aw.ShowDialog();
        }

        private void Font_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Font_Click ... font_family:{0} font_size:{1}", 
                font_family.SelectedItem.ToString(), font_size.SelectedItem.ToString());

            //foreach (var children in info_title.Children)
            //{
            //    (children as TextBlock).FontFamily = new FontFamily(font_family.SelectedItem.ToString());
            //    (children as TextBlock).FontSize = Convert.ToDouble(font_size.SelectedItem.ToString());
            //}

            foreach (var children in os_info.Children)
            {
                (children as TextBlock).FontFamily = new FontFamily(font_family.SelectedItem.ToString());
                (children as TextBlock).FontSize = Convert.ToDouble(font_size.SelectedItem.ToString());
            }

            temperatureList.FontFamily = new FontFamily(font_family.SelectedItem.ToString());
            temperatureList.FontSize = Convert.ToDouble(font_size.SelectedItem.ToString());

            mem_List.FontFamily = new FontFamily(font_family.SelectedItem.ToString());
            mem_List.FontSize = Convert.ToDouble(font_size.SelectedItem.ToString());

            disk_List.FontFamily = new FontFamily(font_family.SelectedItem.ToString());
            disk_List.FontSize = Convert.ToDouble(font_size.SelectedItem.ToString());

            usb_list.FontFamily = new FontFamily(font_family.SelectedItem.ToString());
            usb_list.FontSize = Convert.ToDouble(font_size.SelectedItem.ToString());

            net_List.FontFamily = new FontFamily(font_family.SelectedItem.ToString());
            net_List.FontSize = Convert.ToDouble(font_size.SelectedItem.ToString());
        }
    }
}
