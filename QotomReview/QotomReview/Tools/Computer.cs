using QotomReview.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;

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
                foreach (ManagementObject m in moc)
                {
                    manufacturer = m.Properties["Manufacturer"].Value.ToString();
                    if (m.Properties["Manufacturer"].Value.Equals("0710") ||
                        m.Properties["Manufacturer"].Value.Equals("1310"))
                    {
                        manufacturer = "Kimtigo";
                    }
                    speed = m.Properties["Speed"].Value.ToString() + " MHz";
                    type = m.Properties["MemoryType"].Value.ToString();
                    capacity = Convert.ToDouble(m.Properties["Capacity"].Value);
                    size = (capacity / 1024 / 1024 / 1024).ToString("f1") + " GB";
                    BaseData bd = new BaseData(manufacturer, type, size, speed);
                    bd.Check = "error";
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
                    if (m.Properties["Size"].Value != null)
                    {
                        name = m.Properties["Caption"].Value.ToString();
                        type = m.Properties["InterfaceType"].Value.ToString();
                        disksize = Convert.ToDouble(m.Properties["Size"].Value);
                        size = (disksize / 1024 / 1024 / 1024).ToString("f1") + " GB";
                    }
                    disk_list.Add(new BaseData(name, type, size));
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

    }
}
