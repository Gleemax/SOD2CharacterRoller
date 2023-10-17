using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Runtime.InteropServices;
using OpenCvSharp.Flann;

namespace SoD2CharacterRoller
{
    public partial class CharacterRoller : Form
    {
        private double DpiScale = 1.0f;

        private static String XmlPath = @".\CharacterRoller.xml";
        private static String XmlBackupPath = @".\CharacterRoller_Backup.xml";

        private String Lang = "eng";

        private List<CharacterAttributes> Attributes = null;

        private Dictionary<int, CharacterRect> CharacterRects = null;

        private Thread OperateThread = null;
        private Thread[] ExecuteThreads = null;

        public CharacterRoller()
        {
            InitializeComponent();

            Initialize();
        }

        ~CharacterRoller()
        {
            UnregistKey();
            _running = false;
        }

        protected void Initialize()
        {
            RegistKey();

            Attributes = new List<CharacterAttributes>();
            CharacterRects = new Dictionary<int, CharacterRect>();
            ExecuteThreads = new Thread[3];

            LoadXmlFile(XmlPath, ref Attributes, ref CharacterRects);

            if (Attributes.Count == 0)
            {
                Attributes.Add(new CharacterAttributes("eng", true));
            }

            foreach (CharacterAttributes c in Attributes)
            {
                cmbLang.Items.Add(c.Lang);
                if (c.Using)
                {
                    cmbLang.SelectedItem = c.Lang;
                }
            }

            BindingSource bs = new BindingSource();
            bs.DataSource = CharacterRects.Values;
            dgvCharacterRects.DataSource = bs;
            dgvCharacterRects.Columns[0].Width = 65;
            dgvCharacterRects.Columns[1].Width = 65;
            dgvCharacterRects.Columns[2].Width = 65;
            dgvCharacterRects.Columns[3].Width = 65;
            dgvCharacterRects.Columns[4].Width = 70;
            dgvCharacterRects.Columns[5].Width = 70;

            var hdc = WinApi.GetDC(WinApi.GetDesktopWindow());
            int nWidth = WinApi.GetDeviceCaps(hdc, WinApi.DESKTOPHORZRES);
            WinApi.ReleaseDC(IntPtr.Zero, hdc);
            DpiScale = (float)nWidth / (float)Screen.PrimaryScreen.Bounds.Width;

            OcrUtil.InitTesseract(0, "eng");
            OcrUtil.InitTesseract(1, "eng");
            OcrUtil.InitTesseract(2, "eng");

            for (int i = 0; i < 3; i++)
            {
                ExecuteThreads[i] = new Thread(new ParameterizedThreadStart(Thread_Identify));
                ExecuteThreads[i].Start(i);
            }

            OperateThread = new Thread(new ThreadStart(Thread_Execute));
            OperateThread.Start();
        }

        protected void LoadXmlFile(String path, ref List<CharacterAttributes> a, ref Dictionary<int, CharacterRect> b)
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
                        String comment = xe.GetAttribute("Comment");

                        c.Attributes.Add(new CharacterAttribute(name, weight, comment));
                    }
                    c.Attributes.Add(new CharacterAttribute("", 0, ""));

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
                    if (delay != 0) txbOpearDelay.Text = delay.ToString();
                    if (weight != 0) txbTargetWeight.Text = weight.ToString();
                }
            }
            else
            {
                b.Add(0, new CharacterRect(470, 390, 790, 570, 450, 1075));
                b.Add(1, new CharacterRect(1265, 390, 1605, 570, 1275, 1075));
                b.Add(2, new CharacterRect(2095, 390, 2435, 570, 2095, 1075));
            }
        }

        protected void SaveXmlFile(String path, String backup, List<CharacterAttributes> a, ref Dictionary<int, CharacterRect> b)
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
                        ele.SetAttribute("Comment", v.Comment.ToString());
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
            xmlPara.SetAttribute("delay", txbOpearDelay.Text);
            xmlPara.SetAttribute("weight", txbTargetWeight.Text);
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

        protected void RegistKey()
        {
            HotKeys.RegisterHotKey(this.Handle, 100, HotKeys.KeyModifiers.None, Keys.NumPad0);
            HotKeys.RegisterHotKey(this.Handle, 101, HotKeys.KeyModifiers.None, Keys.NumPad1);
            HotKeys.RegisterHotKey(this.Handle, 102, HotKeys.KeyModifiers.None, Keys.NumPad2);
            HotKeys.RegisterHotKey(this.Handle, 103, HotKeys.KeyModifiers.None, Keys.NumPad3);
            HotKeys.RegisterHotKey(this.Handle, 104, HotKeys.KeyModifiers.None, Keys.NumPad4);
            HotKeys.RegisterHotKey(this.Handle, 105, HotKeys.KeyModifiers.None, Keys.NumPad5);
            HotKeys.RegisterHotKey(this.Handle, 106, HotKeys.KeyModifiers.None, Keys.NumPad6);
            HotKeys.RegisterHotKey(this.Handle, 255, HotKeys.KeyModifiers.None, Keys.Delete);
        }

        protected void UnregistKey()
        {
            HotKeys.UnregisterHotKey(this.Handle, 100);
            HotKeys.UnregisterHotKey(this.Handle, 101);
            HotKeys.UnregisterHotKey(this.Handle, 102);
            HotKeys.UnregisterHotKey(this.Handle, 103);
            HotKeys.UnregisterHotKey(this.Handle, 104);
            HotKeys.UnregisterHotKey(this.Handle, 105);
            HotKeys.UnregisterHotKey(this.Handle, 106);
            HotKeys.UnregisterHotKey(this.Handle, 255);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            //按快捷键
            switch (m.Msg)
            {
                case WM_HOTKEY:
                    switch (m.WParam.ToInt32())
                    {
                        case 100:
                            OnHotkey((int)RollType.SlotA | (int)RollType.SlotB | (int)RollType.SlotC);
                            break;
                        case 101:
                            OnHotkey((int)RollType.SlotA);
                            break;
                        case 102:
                            OnHotkey((int)RollType.SlotB);
                            break;
                        case 103:
                            OnHotkey((int)RollType.SlotC);
                            break;
                        case 104:
                            OnHotkey((int)RollType.SlotA | (int)RollType.SlotB);
                            break;
                        case 105:
                            OnHotkey((int)RollType.SlotA | (int)RollType.SlotC);
                            break;
                        case 106:
                            OnHotkey((int)RollType.SlotB | (int)RollType.SlotC);
                            break;
                        case 254:
                        case 255:
                            if (_executing)
                            {
                                OnHotkey((int)RollType.SlotA | (int)RollType.SlotB | (int)RollType.SlotC);
                            }
                            break;
                    }
                    break;
            }
            base.WndProc(ref m);
        }

        protected void OnHotkey(int type)
        {
            if (!_executing && cbTestMode.Checked)
            {
                UpdateDataDev(type);
            }
            else
            {
                if (_executing)
                {
                    _abort = true;
                }
                else
                {
                    if (!int.TryParse(txbTargetWeight.Text, out int weight))
                    {
                        weight = 6;
                    }
                    if (!int.TryParse(txbOpearDelay.Text, out int delay))
                    {
                        delay = 20;
                    }
                    _type = type;
                    _weight = weight;
                    _count = 0;
                    _attrs.Clear();
                    _abort = false;
                    _executing = true;
                    _time = System.Environment.TickCount;
                    _delay = delay;
                    _repeat = 0;

                    _slotrun[0] = (_type & (int)RollType.SlotA) == (int)RollType.SlotA;
                    _slotrun[1] = (_type & (int)RollType.SlotB) == (int)RollType.SlotB;
                    _slotrun[2] = (_type & (int)RollType.SlotC) == (int)RollType.SlotC;
                    _slotsucc[0] = false;
                    _slotsucc[1] = false;
                    _slotsucc[2] = false;
                    _operlock[0] = true;
                    _operlock[1] = true;
                    _operlock[2] = true;
                    _scale[0] = (int)Math.Ceiling((double)500 / (double)(CharacterRects[0].Right - CharacterRects[0].Left));
                    _scale[1] = (int)Math.Ceiling((double)500 / (double)(CharacterRects[1].Right - CharacterRects[1].Left));
                    _scale[2] = (int)Math.Ceiling((double)500 / (double)(CharacterRects[2].Right - CharacterRects[2].Left));
                }
            }
        }

        protected bool _running = true;
        protected bool _executing = false;
        protected bool _abort = false;
        protected int _type = 0;
        protected int _weight = 5;
        protected int _count = 0;
        protected int _time = 0;
        protected int _delay = 0;
        protected int _repeat = 0;

        protected bool[] _slotrun = new bool[3];
        protected bool[] _slotsucc = new bool[3];
        protected bool[] _operlock = new bool[3];
        protected int[] _opertime = new int[3];
        protected int[] _scale = new int[3];
        protected String[] _laststr = new String[3];

        protected Dictionary<String, int> _attrs = new Dictionary<string, int>();

        protected void Thread_Execute()
        {
            while (_running)
            {
                if (_executing)
                {
                    if (!_abort && (_slotrun[0] || _slotrun[1] || _slotrun[2]) && _repeat < 10)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            if (_slotrun[i] && !_operlock[i])
                            {
                                ForceSlotRoll(i);
                                _operlock[i] = true;
                            }
                            if (_slotrun[i] && _slotsucc[i])
                            {
                                if (_operlock[i])
                                {
                                    Thread.Sleep(100);
                                    ForceSlotRoll(i, false);
                                    Thread.Sleep(100);
                                }
                                _slotrun[i] = false;
                            }
                        }
                    }
                    else
                    {
                        _time = System.Environment.TickCount - _time;
                        WriteDataLog(_abort, _type, _weight, _time, _count, _attrs);
                        _executing = false;
                        _abort = false;
                    }
                }
                Thread.Sleep(1);
            }
        }
        protected void Thread_Identify(object para)
        {
            int index = (int)para;

            while (_running)
            {
                if (_executing)
                {
                    if (!_abort && _slotrun[index] && !_slotsucc[index] && _operlock[index])
                    {
                        int time = System.Environment.TickCount - _opertime[index];
                        if (time > 60)
                        {
                            Bitmap bitmap = GetSlotRect(index);
                            _operlock[index] = false;

                            String orc = GetOcrText(bitmap, index, _scale[index]);
                            int weight = GetOcrWeight(orc);
                            if (weight >= _weight)
                            {
                                _slotsucc[index] = true;
                            }
                            if (orc.Equals(_laststr[index]))
                            {
                                _repeat++;
                            }
                            else
                            {
                                _laststr[index] = orc;
                                _repeat = 0;
                            }
                        }
                    }
                    Thread.Sleep(1);
                }
            }
        }

        protected bool ForceSlotRoll(int index, bool forward = true)
        {
            bool ret = false;

            IntPtr hwnd = WinApi.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "UnrealWindow", "StateOfDecay2 ");
            if (hwnd != IntPtr.Zero)
            {
                WinApi.SetForegroundWindow(hwnd);
                WinApi.SetCursorPos(
                    (int)(CharacterRects[index].ButtonX / DpiScale),
                    (int)(CharacterRects[index].ButtonY / DpiScale));
                Thread.Sleep(_delay);
                WinApi.keybd_event((Byte)(forward ? Keys.T : Keys.R), 0, 0, 0);
                WinApi.keybd_event((Byte)(forward ? Keys.T : Keys.R), 0, 2, 0);
                Thread.Sleep(_delay);

                _opertime[index] = Environment.TickCount;
                _count++;
                ret = true;
            }

            return ret;
        }

        protected int GetOcrWeight(String ocr)
        {
            int weight = 0;
            if (ocr != null && ocr != "")
            {
                foreach (var attr in ((SortableBindingList<CharacterAttribute>)dgvAttributes.DataSource))
                {
                    if (ocr.ToLower().Contains(attr.Name.ToLower()))
                    {
                        weight += attr.Weight;
                    }
                }
                String[] ocrs = ocr.Split(';');
                foreach (String s in ocrs)
                {
                    if (s.Length > 0)
                    {
                        if (_attrs.ContainsKey(s))
                        {
                            _attrs[s]++;
                        }
                        else
                        {
                            _attrs[s] = 1;
                        }
                    }
                }
            }
            return weight;
        }

        protected Bitmap GetSlotRect(int index)
        {
            return OcrUtil.GetScreenRect(new Rectangle(
                CharacterRects[index].Left,
                CharacterRects[index].Top,
                CharacterRects[index].Right - CharacterRects[index].Left,
                CharacterRects[index].Bottom - CharacterRects[index].Top
                ));
        }

        protected String GetOcrText(Bitmap bitmap, int index, int scale)
        {
            String text = OcrUtil.GetStringFromImage(bitmap, index, scale);
            return text != null ? text.Replace('\n', ';') : "";
        }

        protected void WriteDataLog(bool Abort, int Type, int Weight, int Time, int Count, Dictionary<String, int> Attrs)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("--------------- Roll Completed ---------------");
            sb.AppendLine("Roll complete  = " + (!Abort).ToString());
            sb.AppendLine("Slot type      = " + Type.ToString());
            sb.AppendLine("Target weight  = " + Weight.ToString());
            sb.AppendLine("Total roll     = " + Count.ToString());
            sb.AppendLine("Time consumed  = " + Time.ToString());
            sb.AppendLine("------- Attributes Appeared as Follow --------");
            foreach (var kvp in Attrs)
            {
                sb.AppendLine(kvp.Key + " = " + kvp.Value.ToString());
            }
            sb.AppendLine("----------------------------------------------");
            LogHelper.WriteLog(sb.ToString());
        }

        protected void UpdateDataDev(int type)
        {
            if (type > 0)
            {
                _scale[0] = (int)Math.Ceiling((double)500 / (double)(CharacterRects[0].Right - CharacterRects[0].Left));
                _scale[1] = (int)Math.Ceiling((double)500 / (double)(CharacterRects[1].Right - CharacterRects[1].Left));
                _scale[2] = (int)Math.Ceiling((double)500 / (double)(CharacterRects[2].Right - CharacterRects[2].Left));

                Bitmap bitmap = null;
                if ((type & (int)RollType.SlotA) == (int)RollType.SlotA)
                {
                    bitmap = GetSlotRect(0);
                    pbDisplayOcrA.Image = bitmap;
                    txbDisplayOcrA.Text = GetOcrText(bitmap, 0, _scale[0]);
                }
                if ((type & (int)RollType.SlotB) == (int)RollType.SlotB)
                {
                    bitmap = GetSlotRect(1);
                    pbDisplayOcrB.Image = bitmap;
                    txbDisplayOcrB.Text = GetOcrText(bitmap, 1, _scale[1]);
                }
                if ((type & (int)RollType.SlotC) == (int)RollType.SlotC)
                {
                    bitmap = GetSlotRect(2);
                    pbDisplayOcrC.Image = bitmap;
                    txbDisplayOcrC.Text = GetOcrText(bitmap, 2, _scale[2]);
                }
            }
        }

        private void dgvAttributes_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == dgvAttributes.Rows.Count - 1)
            {
                if (dgvAttributes.Rows[e.RowIndex].Cells[0].Value != null &&
                    (String)dgvAttributes.Rows[e.RowIndex].Cells[0].Value != "")
                {
                    ((SortableBindingList<CharacterAttribute>)dgvAttributes.DataSource).Add(new CharacterAttribute("", 0, ""));
                }
            }
            else
            {
                if (dgvAttributes.Rows[e.RowIndex].Cells[0].Value == null ||
                    (String)dgvAttributes.Rows[e.RowIndex].Cells[0].Value == "")
                {
                    ((SortableBindingList<CharacterAttribute>)dgvAttributes.DataSource).RemoveAt(e.RowIndex);
                }
            }
        }

        private void btnSaveConfig_Click(object sender, EventArgs e)
        {
            SaveXmlFile(XmlPath, XmlBackupPath, Attributes, ref CharacterRects);
        }

        private void btnAutoCali_Click(object sender, EventArgs e)
        {
            IntPtr hwnd = WinApi.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "UnrealWindow", "StateOfDecay2 ");
            if (hwnd != IntPtr.Zero)
            {
                WinApi.RECT windowsrect = new WinApi.RECT();
                WinApi.RECT clientrect = new WinApi.RECT();
                if (WinApi.GetWindowRect(hwnd, out windowsrect) && WinApi.GetClientRect(hwnd, ref clientrect))
                {
                    int clientwidth = (int)((clientrect.right - clientrect.left) * DpiScale);
                    int clientheight = (int)((clientrect.bottom - clientrect.top) * DpiScale);
                    int titleheight = (int)((windowsrect.bottom - windowsrect.top) * DpiScale) - clientheight;
                    int left = (int)(windowsrect.left * DpiScale);
                    int top = (int)(windowsrect.top * DpiScale) + titleheight;
                    CharacterRects[0].Left = left + (int)(clientwidth * 0.1849);
                    CharacterRects[0].Top = top + (int)(clientheight * 0.3264);
                    CharacterRects[0].Right = left + (int)(clientwidth * 0.3496);
                    CharacterRects[0].Bottom = top + (int)(clientheight * 0.4036);
                    CharacterRects[0].ButtonX = left + (int)(clientwidth * 0.2643);
                    CharacterRects[0].ButtonY = top + (int)(clientheight * 0.3819);
                    CharacterRects[1].Left = left + (int)(clientwidth * 0.4167);
                    CharacterRects[1].Top = top + (int)(clientheight * 0.3264);
                    CharacterRects[1].Right = left + (int)(clientwidth * 0.5807);
                    CharacterRects[1].Bottom = top + (int)(clientheight * 0.4036);
                    CharacterRects[1].ButtonX = left + (int)(clientwidth * 0.4961);
                    CharacterRects[1].ButtonY = top + (int)(clientheight * 0.3819);
                    CharacterRects[2].Left = left + (int)(clientwidth * 0.6432);
                    CharacterRects[2].Top = top + (int)(clientheight * 0.3264);
                    CharacterRects[2].Right = left + (int)(clientwidth * 0.8073);
                    CharacterRects[2].Bottom = top + (int)(clientheight * 0.4036);
                    CharacterRects[2].ButtonX = left + (int)(clientwidth * 0.7227);
                    CharacterRects[2].ButtonY = top + (int)(clientheight * 0.3819);
                    dgvCharacterRects.Invalidate();
                }
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            OnHotkey((int)RollType.SlotA | (int)RollType.SlotB | (int)RollType.SlotC);
        }

        private void cmbLang_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (CharacterAttributes c in Attributes)
            {
                if (c.Lang == Lang)
                {
                    c.Using = false;
                }
                if (c.Lang == cmbLang.SelectedItem.ToString())
                {
                    if (c.Attributes != null)
                    {
                        dgvAttributes.DataSource = c.Attributes;
                        dgvAttributes.Columns[0].Width = 200;
                        dgvAttributes.Columns[1].Width = 75;
                        dgvAttributes.Columns[2].Width = 175;
                    }
                    c.Using = true;
                }
            }
            Lang = cmbLang.SelectedItem.ToString();
        }

        private void CharacterRoller_FormClosed(object sender, FormClosedEventArgs e)
        {
            System.Environment.Exit(0);
        }

        private void linkJoinGroup_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://qm.qq.com/q/kT9NANpY5i");
        }

        private void cbProMode_CheckedChanged(object sender, EventArgs e)
        {
            if (cbProMode.Checked)
            {
                dgvAttributes.ReadOnly = false;
                dgvCharacterRects.ReadOnly = false;
                txbOpearDelay.Enabled = true;
            }
            else
            {
                dgvAttributes.ReadOnly = true;
                dgvCharacterRects.ReadOnly = true;
                txbOpearDelay.Enabled = false;
            }
        }

        private void cbTestMode_CheckedChanged(object sender, EventArgs e)
        {
            if (cbTestMode.Checked)
            {
                tableLayoutPanelSet.Controls.Remove(tableLayoutPanelWeight);
                tableLayoutPanelSet.Controls.Add(tableLayoutPanelTest, 0, 0);
            }
            else
            {
                tableLayoutPanelSet.Controls.Remove(tableLayoutPanelTest);
                tableLayoutPanelSet.Controls.Add(tableLayoutPanelWeight, 0, 0);
            }

        }

        private void linkWiki_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://wiki.biligame.com/stateofdecay2/%E7%89%B9%E8%B4%A8");
        }
    }
}
