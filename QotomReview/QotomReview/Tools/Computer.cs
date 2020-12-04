using QotomReview.model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;

namespace QotomReview.Tool
{
    public class Computer
    {
        //系统版本
        public static string GetSystemVersion()
        {
            try
            {
                var os_version = string.Empty;
                var mos = new ManagementObjectSearcher("Select * from Win32_OperatingSystem");
                foreach (var o in mos.Get())
                {
                    var mo = (ManagementObject)o;
                    os_version += mo["Caption"].ToString() + " ";
                    os_version += mo["Version"].ToString();
                }
                mos.Dispose();
                return os_version;
            }
            catch (Exception)
            {
                return "unknown";
            }
        }

        //系统类型
        public static string GetSystemType(string value)
        {
            try
            {
                var st = string.Empty;
                var mos = new ManagementObjectSearcher("Select * from Win32_ComputerSystem");
                foreach (var o in mos.Get())
                {
                    var mo = (ManagementObject)o;
                    st = mo[value].ToString();

                }
                mos.Dispose();
                return st;
            }
            catch (Exception)
            {
                return "unknown";
            }
        }

        //CPU型号
        public static string GetCpuName()
        {
            try
            {
                var st = string.Empty;
                var mos = new ManagementObjectSearcher("Select * from Win32_Processor");
                foreach (var o in mos.Get())
                {
                    var mo = (ManagementObject)o;
                    st = mo["Name"].ToString();
                }
                mos.Dispose();
                return st;
            }
            catch (Exception)
            {
                return "unknown";
            }
        }

        //主板类型
        public static string GetBoardType()
        {
            try
            {
                var st = string.Empty;
                var mos = new ManagementObjectSearcher("Select * from Win32_BaseBoard");
                foreach (var o in mos.Get())
                {
                    var mo = (ManagementObject)o;
                    st = mo["Product"].ToString();
                }
                mos.Dispose();
                return st;
            }

            catch (Exception)
            {
                return "unknown";
            }
        }

        public static string GetBios()
        {
            try
            {
                ManagementClass mc = new ManagementClass("Win32_BIOS");
                ManagementObjectCollection moc = mc.GetInstances();
                string strID = null;
                foreach (ManagementObject mo in moc)
                {
                    strID += mo.Properties["Manufacturer"].Value.ToString() + " ";
                    //strID += mo.Properties["Version"].Value.ToString() + " ";
                    strID += mo.Properties["Name"].Value.ToString() + " ";
                    string data = mo.Properties["ReleaseDate"].Value.ToString().Substring(0, 8);
                    strID += DateTime.ParseExact(data, "yyyyMMdd", null).ToString("yyyy/MM/dd") + " ";
                }
                mc = null;
                moc.Dispose();
                return strID;
            }
            catch (Exception)
            {
                return "unknown";
            }
        }

        //内存信息
        public static List<BaseData> GetMemoryInfo()
        {
            List<BaseData> mem_list = new List<BaseData>();
            try
            {
                ManagementClass mc = new ManagementClass("Win32_PhysicalMemory");
                ManagementObjectCollection moc = mc.GetInstances();

                double capacity = 0;
                string size = "";
                string manufacturer = String.Empty;
                string type = String.Empty;
                string speed = String.Empty;
                int minVoltage = 0;
                foreach (ManagementObject m in moc)
                {
                    manufacturer = m.Properties["Manufacturer"].Value.ToString();
                    if (m.Properties["Manufacturer"].Value.Equals("0710") ||
                        m.Properties["Manufacturer"].Value.Equals("1310"))
                    {
                        manufacturer = "Kimtigo";
                    }
                    speed = m.Properties["Speed"].Value.ToString() + " MHz";
                    //type = m.Properties["MemoryType"].Value.ToString();
                    try
                    {
                        minVoltage = Convert.ToInt32(m.Properties["MinVoltage"].Value);
                    }
                    catch (Exception)
                    {
                    }
                    if (minVoltage >= 1350)
                        type = "DDR3";
                    else
                        type = "DDR4";
                    
                    capacity = Convert.ToDouble(m.Properties["Capacity"].Value);
                    size = (capacity / 1024 / 1024 / 1024).ToString("f1") + " GB";
                    BaseData bd = new BaseData(manufacturer, type, size, speed);
                    mem_list.Add(bd);
                    
                }
                mc = null;
                moc.Dispose();
                return mem_list;
            }
            catch (Exception)
            {
                return null;
            }
        }

        //磁盘信息
        public static List<BaseData> GetDiskInfo()
        {
            List<BaseData> disk_list = new List<BaseData>();
            try
            {
                ManagementClass mc = new ManagementClass("Win32_DiskDrive");
                ManagementObjectCollection moc = mc.GetInstances();

                double disksize = 0;
                string size = String.Empty;
                string name = String.Empty;
                string type = String.Empty;
                foreach (ManagementObject m in moc)
                {
                    if(m.Properties["InterfaceType"].Value.ToString().Equals("USB"))
                    {
                        continue;
                    }
                    else
                    {
                        if (m.Properties["Size"].Value != null)
                        {
                            name = m.Properties["Caption"].Value.ToString();
                            type = m.Properties["InterfaceType"].Value.ToString();
                            disksize = Convert.ToDouble(m.Properties["Size"].Value);
                            size = (disksize / 1024 / 1024 / 1024).ToString("f1") + " GB";
                        }
                        disk_list.Add(new BaseData(name, type, size));
                    }
                    
                }
                mc = null;
                moc.Dispose();
                return disk_list;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static List<NetWorkData> GetNetWorkAdpterInfo()
        {
            List<NetWorkData> net_list = new List<NetWorkData>();
            try
            {
                NetWorkData nwd = null;
                NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface adapter in nics)
                {
                    //太网连接
                    bool isEthernet = (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet);
                    //无线连接
                    bool isWireless = (adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211);
                    //Console.WriteLine("Name:" + adapter.Name + " Description:" + adapter.Description +
                    //    " isEthernet:" + isEthernet + " isWireless:" + isWireless + " OperationalStatus:" + adapter.OperationalStatus);
                    
                    string mac = string.Join("-", (from z in adapter.GetPhysicalAddress().GetAddressBytes() select z.ToString("X2")).ToArray());
                    //Console.WriteLine("mac:" + mac);
                    if(!isEthernet && !isWireless)
                    {
                        continue;
                    }
                    nwd = new NetWorkData
                    {
                        Adapter = adapter.Description,
                        Mac = mac,
                        Status = adapter.OperationalStatus.ToString()
                    };
                    if (isEthernet)
                    {
                        if (adapter.Description.ToLower().IndexOf("bluetooth") > -1)
                        {
                            nwd.Type = "Bluetooth";
                        }
                        else
                        {
                            nwd.Type = "Ethernet";
                        }
                    }
                    if (isWireless)
                    {
                        nwd.Type = "Wireless";
                    }
                    IPInterfaceProperties ip = adapter.GetIPProperties();
                    if (ip.UnicastAddresses.Count > 0)
                    {
                        foreach (var item in ip.UnicastAddresses)
                        {
                            if (item.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                nwd.Ip = item.Address.ToString();
                            }
                        }
                    }
                    net_list.Add(nwd);
                }

                return net_list;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static DateTime GetNetworkTime(string ntpAddress)
        {
            string NtpServer = ntpAddress;

            const int DaysTo1900 = 1900 * 365 + 95; // 95 = offset for leap-years etc.
            const long TicksPerSecond = 10000000L;
            const long TicksPerDay = 24 * 60 * 60 * TicksPerSecond;
            const long TicksTo1900 = DaysTo1900 * TicksPerDay;

            var ntpData = new byte[48];
            ntpData[0] = 0x1B; // LeapIndicator = 0 (no warning), VersionNum = 3 (IPv4 only), Mode = 3 (Client Mode)
            var addresses = Dns.GetHostEntry(NtpServer).AddressList;
            var ipEndPoint = new IPEndPoint(addresses[0], 123);
            long pingDuration = Stopwatch.GetTimestamp(); // temp access (JIT-Compiler need some time at first call)
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect(ipEndPoint);
                socket.ReceiveTimeout = 5000;
                socket.Send(ntpData);
                pingDuration = Stopwatch.GetTimestamp(); // after Send-Method to reduce WinSocket API-Call time

                socket.Receive(ntpData);
                pingDuration = Stopwatch.GetTimestamp() - pingDuration;
            }

            long pingTicks = pingDuration * TicksPerSecond / Stopwatch.Frequency;

            // optional: display response-time
            // Console.WriteLine("{0:N2} ms", new TimeSpan(pingTicks).TotalMilliseconds);

            long intPart = (long)ntpData[40] << 24 | (long)ntpData[41] << 16 | (long)ntpData[42] << 8 | ntpData[43];
            long fractPart = (long)ntpData[44] << 24 | (long)ntpData[45] << 16 | (long)ntpData[46] << 8 | ntpData[47];
            long netTicks = intPart * TicksPerSecond + (fractPart * TicksPerSecond >> 32);

            var networkDateTime = new DateTime(TicksTo1900 + netTicks + pingTicks / 2);

            return networkDateTime; // without ToLocalTime() = faster
            //return networkDateTime.ToLocalTime(); // without ToLocalTime() = faster
        }
    }
}
