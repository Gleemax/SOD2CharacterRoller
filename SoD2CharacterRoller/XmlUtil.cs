using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.IO;

namespace SoD2CharacterRoller
{
    public class XmlUtil
    {
        public static void LoadXmlFile(String path, ref List<CharacterAttributes> a, ref Dictionary<int, CharacterRect> b, ref String d, ref String t)
        {
            if (File.Exists(path))
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(path);

                XmlNode xmlRoot = xml.SelectSingleNode("CharacterRoller");
                XmlNodeList nodes = null;

                foreach (XmlNode xmlAttr in xmlRoot.SelectNodes("Attributes"))
                {
                    String lang = ((XmlElement)xmlAttr).GetAttribute("Lang");
                    bool use = ((XmlElement)xmlAttr).GetAttribute("Using") == "1";

                    CharacterAttributes c = new CharacterAttributes(lang, use);

                    nodes = xmlAttr.SelectNodes("Atrribute");
                    foreach (XmlElement xe in nodes)
                    {
                        String name = xe.GetAttribute("Name");
                        int.TryParse(xe.GetAttribute("Weight"), out int weight);
                        String type = xe.GetAttribute("Type");
                        String comment = xe.GetAttribute("Comment");
                        c.Attributes.Add(new CharacterAttribute(name, weight, type, comment));
                    }
                    c.Attributes.Add(new CharacterAttribute("", 0, "", ""));

                    a.Add(c);
                }

                XmlNode xmlRect = xmlRoot.SelectSingleNode("CharacterRects");
                nodes = xmlRect.SelectNodes("CharacterRect");
                foreach (XmlElement xe in nodes)
                {
                    int.TryParse(xe.GetAttribute("id"), out int id);
                    int.TryParse(xe.GetAttribute("Left"), out int left);
                    int.TryParse(xe.GetAttribute("Top"), out int top);
                    int.TryParse(xe.GetAttribute("Right"), out int right);
                    int.TryParse(xe.GetAttribute("Bottom"), out int bottom);
                    int.TryParse(xe.GetAttribute("ButtonX"), out int buttonX);
                    int.TryParse(xe.GetAttribute("ButtonY"), out int buttonY);

                    if (!b.ContainsKey(id))
                    {
                        b.Add(id, new CharacterRect(left, top, right, bottom, buttonX, buttonY));
                    }
                }

                XmlElement xmlPara = (XmlElement)xmlRoot.SelectSingleNode("Parameter");
                {
                    int.TryParse(xmlPara.GetAttribute("delay"), out int delay);
                    int.TryParse(xmlPara.GetAttribute("weight"), out int weight);
                    if (delay != 0) d = delay.ToString();
                    if (weight != 0) t = weight.ToString();
                }
            }
            else
            {
                b.Add(0, new CharacterRect(470, 390, 790, 570, 450, 1075));
                b.Add(1, new CharacterRect(1265, 390, 1605, 570, 1275, 1075));
                b.Add(2, new CharacterRect(2095, 390, 2435, 570, 2095, 1075));
            }
        }

        public static void SaveXmlFile(
            String path, 
            String backup, 
            List<CharacterAttributes> a, 
            Dictionary<int, CharacterRect> b,
            String d,
            String t)
        {
            XmlDocument xml = new XmlDocument();

            if (File.Exists(path))
            {
                if (File.Exists(backup))
                {
                    File.Delete(backup);
                }
                File.Move(path, backup);
            }

            XmlElement xmlRoot = xml.CreateElement("CharacterRoller");
            xml.AppendChild(xmlRoot);

            foreach (CharacterAttributes p in a)
            {
                XmlElement xmlAttr = xml.CreateElement("Attributes");
                xmlAttr.SetAttribute("Lang", p.Lang);
                xmlAttr.SetAttribute("Using", p.Using ? "1" : "0");

                foreach (var v in p.Attributes)
                {
                    if (v.Name != null && v.Name != "")
                    {
                        XmlElement ele = xml.CreateElement("Atrribute");
                        ele.SetAttribute("Name", v.Name);
                        ele.SetAttribute("Weight", v.Weight.ToString());
                        ele.SetAttribute("Type", v.Type);
                        ele.SetAttribute("Comment", v.Comment);
                        xmlAttr.AppendChild(ele);
                    }
                }
                xmlRoot.AppendChild(xmlAttr);
            }

            XmlElement xmlRect = xml.CreateElement("CharacterRects");
            foreach (var v in b)
            {
                XmlElement ele = xml.CreateElement("CharacterRect");
                ele.SetAttribute("id", v.Key.ToString());
                ele.SetAttribute("Left", v.Value.Left.ToString());
                ele.SetAttribute("Top", v.Value.Top.ToString());
                ele.SetAttribute("Right", v.Value.Right.ToString());
                ele.SetAttribute("Bottom", v.Value.Bottom.ToString());
                ele.SetAttribute("ButtonX", v.Value.ButtonX.ToString());
                ele.SetAttribute("ButtonY", v.Value.ButtonY.ToString());
                xmlRect.AppendChild(ele);
            }
            xmlRoot.AppendChild(xmlRect);

            XmlElement xmlPara = xml.CreateElement("Parameter");
            xmlPara.SetAttribute("delay", d);
            xmlPara.SetAttribute("weight", t);
            xmlRoot.AppendChild(xmlPara);

            try
            {
                xml.Save(path);
            }
            catch (Exception)
            {
                MessageBox.Show("Save Failed!");
            }
        }
    }
}
