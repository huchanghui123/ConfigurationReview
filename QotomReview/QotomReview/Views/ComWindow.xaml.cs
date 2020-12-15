using System;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

            serialPort = new SerialPort
            {
                Encoding = System.Text.Encoding.GetEncoding("GB2312"),
                ReadTimeout = 500,
                WriteTimeout = 500,
                RtsEnable = true,
                DtrEnable = true
            };
            serialPort.PinChanged += new SerialPinChangedEventHandler(SerialPort_PinChange);
            serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);

            portName = name;
            baudRate = int.Parse(rate.ToUpperInvariant());
            dataBits = int.Parse(dbits.ToUpperInvariant());
            stopBits = sbits;
            parity = sparity;
            handshake = shandshake;

            this.Title = portName;
            send_box.Text = portName + "测试!";

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
            OpenSerialPort();
            Send_Click(send, new RoutedEventArgs());
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Send_Click(send, new RoutedEventArgs());
        }

        private void OpenSerialPort()
        {
            try
            {
                serialPort.PortName = com.Text;
                serialPort.BaudRate = int.Parse(baud_Rate.Text);
                serialPort.DataBits = int.Parse(data_Bits.Text);
                serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), stop_Bits.Text, true);
                serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), _parity.Text, true);
                serialPort.Handshake = (Handshake)Enum.Parse(typeof(Handshake), hand_shake.Text, true);

                serialPort.Open();

                if (serialPort.CtsHolding)
                {
                    ctsStatus.Background = Brushes.Green;
                }
                if (serialPort.DsrHolding)
                {
                    dsrStatus.Background = Brushes.Green;
                }
                if (serialPort.CDHolding)
                {
                    dcdStatus.Background = Brushes.Green;
                }

                com.IsEnabled = false;
                send.IsEnabled = false;
                stop.IsEnabled = true;
                close.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Send_Click:" + serialPort.IsOpen + " " + timer.IsEnabled);
            try
            {
                if (serialPort.IsOpen)
                {
                    string message = send_box.Text;
                    serialPort.WriteLine(message);

                    if (!timer.IsEnabled)
                    {
                        double value = Convert.ToDouble(delay.Text);
                        timer.Interval = TimeSpan.FromMilliseconds(value);
                        timer.Start();
                        //delay.IsEnabled = false;
                    }
                }
                else
                {
                    OpenSerialPort();
                    Send_Click(send, new RoutedEventArgs());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort serialPort1 = (SerialPort)sender;
            byte[] ReDatas = new byte[serialPort1.BytesToRead];
            try
            {
                indata = serialPort1.ReadLine();
            }
            catch (Exception ex)
            {
                this.Dispatcher.BeginInvoke((Action)delegate () {
                    receive_box.AppendText(ex.Message + " Received:" + ReDatas.Length);

                    Stop_Click(stop, new RoutedEventArgs());
                });
            }
            sb.Clear();
            sb.Append(indata);
            indata = "";
            try
            {
                this.Dispatcher.BeginInvoke((Action)delegate () {
                    receive_box.AppendText(sb.ToString());
                    receive_box.ScrollToEnd();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SerialPort_PinChange(object sender, SerialPinChangedEventArgs e)
        {
            SerialPort serialPort1 = sender as SerialPort;
            if (serialPort1.CtsHolding)
            {
                InvokeUpdateStatePanel(ctsStatus, Brushes.Green);
            }
            else
            {
                InvokeUpdateStatePanel(ctsStatus, Brushes.LightGray);
            }
            if (serialPort1.DsrHolding)
            {
                InvokeUpdateStatePanel(dsrStatus, Brushes.Green);
            }
            else
            {
                InvokeUpdateStatePanel(dsrStatus, Brushes.LightGray);
            }
            if (serialPort1.CDHolding)
            {
                InvokeUpdateStatePanel(dcdStatus, Brushes.Green);
            }
            else
            {
                InvokeUpdateStatePanel(dcdStatus, Brushes.LightGray);
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort.IsOpen && timer.IsEnabled)
            {
                timer.Stop();
                serialPort.Close();
                stop.IsEnabled = false;
                send.IsEnabled = true;
            }
            Console.WriteLine("Close_Click:" + serialPort.IsOpen + " " + timer.IsEnabled);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
            timer.Stop();
            stop.IsEnabled = false;
            close.IsEnabled = false;
            send.IsEnabled = true;
            com.IsEnabled = true;
            InvokeUpdateStatePanel(ctsStatus, Brushes.LightGray);
            InvokeUpdateStatePanel(dsrStatus, Brushes.LightGray);
            InvokeUpdateStatePanel(dcdStatus, Brushes.LightGray);
            Console.WriteLine("Close_Click:" + serialPort.IsOpen + " " + timer.IsEnabled);
        }

        private void InvokeUpdateStatePanel(TextBlock tb, SolidColorBrush color)
        {
            this.Dispatcher.Invoke((Action)delegate ()
            {
                tb.Background = color;
            });
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
            timer.Stop();
            timer.IsEnabled = false;
            Console.WriteLine("Window_Closed:" + serialPort.IsOpen + " " + timer.IsEnabled);
        }

        private void Com_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            send_box.Text = comboBox.SelectedItem.ToString() + "测试!";
        }
    }
}
