using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using System.Threading;

namespace PerformanceMonitoring
{
    public class PerfMonitor
    {
        private bool endCreateFile = false;

        public PerfMonitor()
        {

        }

        public bool EndCreateFile
        {
            get
            {
                return endCreateFile;
            }
        }

        public void MonitorsList()
        {
            endCreateFile = false;
            CreateXmlDocument();
            XmlDocument xmlDoc = new XmlDocument();
            FileStream file = new FileStream("MonitorsList.xml", FileMode.Open);
            xmlDoc.Load(file);

            PerformanceCounterCategory[] categories = PerformanceCounterCategory.GetCategories();
            string[] CategoryName = new string[categories.Length];
            for (int i = 0; i < categories.Length; i++)
                CategoryName[i] = categories[i].CategoryName;
            Array.Sort(CategoryName);

            for (int i = 0; i < CategoryName.Length; i++)
            {
                if (CategoryName[i] == "Wątek") // Kategoria "Wątek" musi zostać pominięta ponieważ jest zbyt dynamiczna, instancje co chwile się zmieniają
                    continue;
                XmlElement xmlCategory = xmlDoc.CreateElement("Category");
                xmlCategory.SetAttribute("Name", CategoryName[i]);

                PerformanceCounterCategory pccInstance = new PerformanceCounterCategory(CategoryName[i]);
                string[] instance = pccInstance.GetInstanceNames();

                if (instance.Length == 0)
                {
                    XmlElement xmlInstance = xmlDoc.CreateElement("Instance");
                    xmlInstance.SetAttribute("Name", "Non_Instance");

                    PerformanceCounterCategory pccCounters = new PerformanceCounterCategory(CategoryName[i]);
                    PerformanceCounter[] counters;
                    counters = pccCounters.GetCounters();
                    string[] countersList = new string[counters.Length];
                    for (int k = 0; k < counters.Length; k++)
                    {
                        countersList[k] = counters[k].CounterName;
                    }
                    Array.Sort(countersList);
                    for (int m = 0; m < countersList.Length; m++)
                    {
                        XmlElement xmlCounter = xmlDoc.CreateElement("Counter");
                        XmlText xmlCounterText = xmlDoc.CreateTextNode(countersList[m]);
                        xmlCounter.AppendChild(xmlCounterText);
                        xmlInstance.AppendChild(xmlCounter);
                    }
                    xmlCategory.AppendChild(xmlInstance);
                }
                else
                {
                    for (int j = 0; j < instance.Length; j++)
                    {
                        XmlElement xmlInstance = xmlDoc.CreateElement("Instance");
                        xmlInstance.SetAttribute("Name", instance[j]);

                        PerformanceCounterCategory pccCounters = new PerformanceCounterCategory(CategoryName[i]);
                        PerformanceCounter[] counters;
                        counters = pccCounters.GetCounters(instance[j]);
                        string[] countersList = new string[counters.Length];
                        for (int k = 0; k < counters.Length; k++)
                        {
                            countersList[k] = counters[k].CounterName;
                        }
                        Array.Sort(countersList);
                        for (int m = 0; m < countersList.Length; m++)
                        {
                            XmlElement xmlCounter = xmlDoc.CreateElement("Counter");
                            XmlText xmlCounterText = xmlDoc.CreateTextNode(countersList[m]);
                            xmlCounter.AppendChild(xmlCounterText);
                            xmlInstance.AppendChild(xmlCounter);
                        }
                        xmlCategory.AppendChild(xmlInstance);
                    }
                }
                xmlDoc.DocumentElement.AppendChild(xmlCategory);
            }
            file.Close();
            xmlDoc.Save("MonitorsList.xml");
            endCreateFile = true;
        }

        private void CreateXmlDocument()
        {
            File.Delete("MonitorsList.xml");
            XmlTextWriter xmlTW = new XmlTextWriter("MonitorsList.xml", Encoding.UTF8);
            xmlTW.WriteStartDocument();
            xmlTW.WriteStartElement("MonitorsList");
            xmlTW.WriteEndDocument();
            xmlTW.Close();
        }

        public string[] ReadAllCategory(string database)
        {
            string[] allCategory;
            XmlDocument xmlDoc = new XmlDocument();
            FileStream file = new FileStream(database, FileMode.Open);

            xmlDoc.Load(file);

            XmlNodeList listCategory = xmlDoc.GetElementsByTagName("Category");
            allCategory = new string[listCategory.Count];
            for (int i = 0; i < listCategory.Count; i++)
            {
                XmlElement element = (XmlElement)listCategory[i];
                allCategory[i] = element.GetAttribute("Name");
            }
            file.Close();
            return allCategory;
        }

        public string[] ReadAllInstance(string database, string category)
        {
            string[] allInstance;
            XmlDocument xmlDoc = new XmlDocument();
            FileStream file = new FileStream(database, FileMode.Open);

            xmlDoc.Load(file);

            XmlNodeList listCategory = xmlDoc.GetElementsByTagName("Category");
            int i;
            for (i = 0; i < listCategory.Count; i++)
            {
                XmlElement element = (XmlElement)listCategory[i];
                if (element.GetAttribute("Name") == category)
                    break;
            }

            XmlNodeList listInstance = listCategory[i].ChildNodes;
            allInstance = new string[listInstance.Count];

            for (int j = 0; j < listInstance.Count; j++)
            {
                XmlElement element = (XmlElement)listInstance[j];
                allInstance[j] = element.GetAttribute("Name");
            }

            file.Close();
            return allInstance;
        }

        public string[] ReadAllProperty(string database, string category, string instance)
        {
            string[] allProperty;
            XmlDocument xmlDoc = new XmlDocument();
            FileStream file = new FileStream(database, FileMode.Open);

            xmlDoc.Load(file);

            XmlNodeList listCategory = xmlDoc.GetElementsByTagName("Category");
            int i;
            for (i = 0; i < listCategory.Count; i++)
            {
                XmlElement element = (XmlElement)listCategory[i];
                if (element.GetAttribute("Name") == category)
                    break;
            }

            XmlNodeList listInstance = listCategory[i].ChildNodes;
            int j;
            for (j = 0; j < listInstance.Count; j++)
            {
                XmlElement element = (XmlElement)listInstance[j];
                if (element.GetAttribute("Name") == instance)
                    break;
            }

            XmlNodeList listProperty = listInstance[j].ChildNodes;
            allProperty = new string[listProperty.Count];

            for (int k = 0; k < listProperty.Count; k++)
            {
                XmlElement element = (XmlElement)listProperty[k];
                allProperty[k] = element.InnerXml;
            }

            file.Close();
            return allProperty;
        }

        public void StartPerfMonitor(object parameters)
        {
            string [] param = (string[])parameters;
            // Pierwszy parametr to plik bazodanowy, drugi parametr to licznik, trzeci parametr to interwał


            XmlDocument xmlDoc = new XmlDocument();

            PerformanceCounter myCounter;
            string[] counter = param[1].Split(';');
            if (counter[2] == "Non_Instance")
                counter[2] = "";
            myCounter = new PerformanceCounter(counter[0],counter[1],counter[2]);
            try
            {
                myCounter.RawValue = 19;
            }
            catch(Exception ex)
            {

            }
            while (true)
            {
                try
                {
                    FileStream file = new FileStream(param[0], FileMode.Open,FileAccess.ReadWrite, FileShare.Inheritable);
                    xmlDoc.Load(file);

                    XmlElement xmlCounter = xmlDoc.CreateElement("Counter");
                    xmlCounter.SetAttribute("Name", param[1]);

                    XmlElement xmlValue = xmlDoc.CreateElement("Value");
                    XmlText xmlValueText = xmlDoc.CreateTextNode(myCounter.NextValue().ToString());
                    xmlValue.AppendChild(xmlValueText);

                    XmlElement xmlTime = xmlDoc.CreateElement("Time");
                    XmlText xmlTimeText = xmlDoc.CreateTextNode(DateTime.Now.ToLocalTime().ToString());
                    xmlTime.AppendChild(xmlTimeText);

                    xmlCounter.AppendChild(xmlTime);
                    xmlCounter.AppendChild(xmlValue);
                    xmlDoc.DocumentElement.AppendChild(xmlCounter);

                    file.Close();
                    xmlDoc.Save(param[0]);
                }
                catch
                {

                }
                Thread.Sleep(Convert.ToInt32(param[2]) * 1000);
            }
        }

    }
}
