using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoD2CharacterRoller
{
    class LogHelper
    {
        public static bool WriteLog(String Msg, bool UseTime = false)
        {
            bool r = true;
            try
            {
                DateTime dt = DateTime.Now;
                String Path = AppDomain.CurrentDomain.BaseDirectory + @"\Log";
                if (!System.IO.Directory.Exists(Path))
                {
                    System.IO.Directory.CreateDirectory(Path);
                }

                Path = Path + @"\" + dt.ToString("yyyyMMddHHmmss") + @".txt";
                System.IO.StreamWriter s = new System.IO.StreamWriter(Path, true);
                s.WriteLine((UseTime ? dt.ToString("HH:mm:ss") : "") + Msg);
                s.Close();
            }
            catch (Exception)
            {
                r = false;
            }
            return r;
        }
    }
}
