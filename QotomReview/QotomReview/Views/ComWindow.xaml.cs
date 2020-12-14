using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace QotomReview.Views
{
    /// <summary>
    /// ComWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ComWindow : Window
    {
        private string portName = "COM1";
        private int baudRate = 115200;
        private int dataBits = 8;
        private String stopBits = "One";
        private String parity = "None";
        private String handshake = "None";

        private string indata = "";
        private StringBuilder sb = new StringBuilder();
        private SerialPort serialPort;
        private DispatcherTimer timer;

        public ComWindow(string name, string rate, string dbits,
            String sbits, String sparity, String shandshake)
        {
            InitializeComponent();

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200),
                IsEnabled = false
            };
            timer.Tick += Timer_Tick;

            portName = name;
            baudRate = int.Parse(rate.ToUpperInvariant());
            dataBits = int.Parse(dbits.ToUpperInvariant());
            stopBits = sbits;
            parity = sparity;
            handshake = shandshake;

            this.Title = portName;

            string[] coms = SerialPort.GetPortNames();
            com.ItemsSource = coms;
            com.SelectedIndex = coms.ToList().IndexOf(portName);
            string[] baudDatas = { "9600", "19200", "38400", "56000", "57600", "115200", "128000" };
            baud_Rate.ItemsSource = baudDatas;
            baud_Rate.SelectedIndex = baudDatas.ToList().IndexOf(rate);
            data_Bits.Text = dbits;
            string[] stopBitData = { StopBits.One + "", StopBits.Two + "", StopBits.OnePointFive + "" };
            stop_Bits.ItemsSource = stopBitData;
            stop_Bits.SelectedIndex = stopBitData.ToList().IndexOf(stopBits);
            string[] parityData = { Parity.None + "", Parity.Odd + "", Parity.Even + "", Parity.Mark + "", Parity.Space + "" };
            _parity.ItemsSource = parityData;
            _parity.SelectedIndex = parityData.ToList().IndexOf(parity);
            string[] handShakeData = { Handshake.None + "", Handshake.XOnXOff + "", Handshake.RequestToSend + "", Handshake.RequestToSendXOnXOff + "" };
            hand_shake.ItemsSource = handShakeData;
            hand_shake.SelectedIndex = handShakeData.ToList().IndexOf(handshake);
            Console.WriteLine("init:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff:ffffff"));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("onloaded:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff:ffffff"));
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            //Send_Click(SendBtn, new RoutedEventArgs());
        }

        private void OpenSerialPort()
        {
            try
            {
                //serialPort.PortName = portName;
                //serialPort.BaudRate = baudRate;
                //serialPort.DataBits = dataBits;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void Send_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Closed(object sender, EventArgs e)
        {

        }
    }
}
