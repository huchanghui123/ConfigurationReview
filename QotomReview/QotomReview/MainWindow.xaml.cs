using QotomReview.model;
using QotomReview.Tool;
using System;
using System.Collections.Generic;
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
        DispatcherTimer dispatcherTimer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();

            Thread t = new Thread(WorkThread)
            {
                IsBackground = true
            };
            t.Start();

            Thread t1 = new Thread(WorkThread2)
            {
                IsBackground = true
            };
            t1.Start();

            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke((Action)delegate ()
            {
                time.Text = DateTime.Now.ToString();
            });
        }

        void WorkThread()
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
                os_name.Text = account;
                mb.Text = boardType;
                bios.Text = biosName;
                
            });
        }

        void WorkThread2()
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

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Console.WriteLine("Window OnClosing!!!");
            if(dispatcherTimer.IsEnabled)
            {
                dispatcherTimer.Stop();
            }
        }
    }
}
