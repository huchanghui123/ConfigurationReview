using QotomReview.model;
using System;
using System.Xml;

namespace QotomReview.Tools
{
    public class SaveXml
    {
        public static void SaveConfigurationToXml(ConfigData data, string path)
        {
            //创建XML文档对象
            XmlDocument doc = new XmlDocument();
            //创建第一个行描述信息，并且添加到doc文档中
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(dec);
            //创建根节点
            XmlElement config = doc.CreateElement("Config");
            doc.AppendChild(config);
            //创建OSVer子节点
            XmlElement os_ver = doc.CreateElement("OSVer");
            os_ver.InnerText = data.OSVer;
            config.AppendChild(os_ver);

            XmlElement os_type = doc.CreateElement("OSType");
            os_type.InnerText = data.OSType;
            config.AppendChild(os_type);

            XmlElement language = doc.CreateElement("Language");
            language.InnerText = data.Language;
            config.AppendChild(language);

            XmlElement processor = doc.CreateElement("Processor");
            processor.InnerText = data.Processor;
            config.AppendChild(processor);

            XmlElement mb = doc.CreateElement("Motherboard");
            mb.InnerText = data.Motherboard;
            config.AppendChild(mb);

            XmlElement bios_ver = doc.CreateElement("BIOS");
            bios_ver.InnerText = data.BIOS;
            config.AppendChild(bios_ver);

            XmlElement memory = doc.CreateElement("Memory");
            config.AppendChild(memory);
            if(data.Memory != null && data.Memory.Length >= 1)
            {
                for (int i = 0; i < data.Memory.Length; i++)
                {
                    XmlElement mem = doc.CreateElement("Value");
                    mem.InnerText = data.Memory[i];
                    memory.AppendChild(mem);
                }
            }
            else
            {
                XmlElement mem = doc.CreateElement("Value");
                mem.InnerText = "unknown";
                memory.AppendChild(mem);
            }
            

            XmlElement storage = doc.CreateElement("Storage");
            config.AppendChild(storage);
            if (data.Storage != null && data.Storage.Length >= 1)
            {
                for (int i = 0; i < data.Storage.Length; i++)
                {
                    XmlElement disk = doc.CreateElement("Value");
                    disk.InnerText = data.Storage[i];
                    storage.AppendChild(disk);
                }
            }
            else
            {
                XmlElement disk = doc.CreateElement("Value");
                disk.InnerText = "unknown";
                storage.AppendChild(disk);
            }

                

            XmlElement network = doc.CreateElement("Network");
            config.AppendChild(network);
            if(data.Network != null && data.Network.Length >= 1)
            {
                for (int i = 0; i < data.Network.Length; i++)
                {
                    XmlElement net = doc.CreateElement("Adapter");
                    net.InnerText = data.Network[i];
                    network.AppendChild(net);
                }
            }
            else
            {
                XmlElement net = doc.CreateElement("Adapter");
                net.InnerText = "unknown";
                network.AppendChild(net);
            }
            

            doc.Save(path);
        }

        public static ConfigData ReadConfigurationFromXml(string path)
        {
            ConfigData config = new ConfigData();
            XmlDocument xmlDoc = new XmlDocument();
            XmlReaderSettings settings = new XmlReaderSettings
            {
                IgnoreComments = true//忽略文档里面的注释
            };
            XmlReader reader = XmlReader.Create(path, settings);
            try
            {
                xmlDoc.Load(reader);
                // 得到根节点Config
                XmlNode xn = xmlDoc.SelectSingleNode("Config");
                // 得到根节点的所有子节点
                XmlNodeList xnl = xn.ChildNodes;

                foreach (XmlNode xn1 in xnl)
                {
                    XmlElement xe = (XmlElement)xn1;
                    int count = xe.ChildNodes.Count;
                    Console.WriteLine(xe.Name + " " + count + " ");

                    if (xe.Name.Equals("OSVer"))
                    {
                        config.OSVer = xe.InnerText;
                    }
                    else if(xe.Name.Equals("OSType"))
                    {
                        config.OSType = xe.InnerText;
                    }
                    else if (xe.Name.Equals("Language"))
                    {
                        config.Language = xe.InnerText;
                    }
                    else if (xe.Name.Equals("Processor"))
                    {
                        config.Processor = xe.InnerText;
                    }
                    else if (xe.Name.Equals("Motherboard"))
                    {
                        config.Motherboard = xe.InnerText;
                    }
                    else if (xe.Name.Equals("BIOS"))
                    {
                        config.BIOS = xe.InnerText;
                    }
                    else
                    {
                        string[] temp = new string[count];
                        for (int i = 0; i < count; i++)
                        {
                            temp[i] = xe.ChildNodes.Item(i).InnerText;
                            Console.WriteLine(xe.Name + ":" + temp[i]);
                        }

                        if (xe.Name.Equals("Memory"))
                        {
                            config.Memory = temp;
                        }
                        else if (xe.Name.Equals("Storage"))
                        {
                            config.Storage = temp;
                        }
                        else if (xe.Name.Equals("Network"))
                        {
                            config.Network = temp;
                        }
                        }
                }
            }
            catch (Exception )
            {
                return null;
            }
            finally
            {
                reader.Close();
            }
            return config;
        }
    }
}
