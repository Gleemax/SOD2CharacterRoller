using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using Tesseract;
using System.Security.Cryptography;
using OpenCvSharp;

namespace SoD2CharacterRoller
{
    public class OcrUtil
    {
        static Dictionary<int, TesseractEngine> Engines = new Dictionary<int, TesseractEngine>();

        public static void InitTesseract(int index, String lang)
        {
            Engines.Add(index, new TesseractEngine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata"), lang, EngineMode.TesseractOnly));
        }

        public static String GetStringFromImage(Bitmap image, int index, int scale)
        {
            String r = null;

            Pix pix = null;
            if (scale > 1)
            {
                OpenCvSharp.Mat mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(image);
                Cv2.Resize(mat, mat, new OpenCvSharp.Size(mat.Cols * scale, mat.Rows * scale));
                pix = PixConverter.ToPix(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat));
            }
            else
            {
                pix = PixConverter.ToPix(image);
            }

            if (pix != null)
            {
                Tesseract.Page page = null;
                if (Engines.ContainsKey(index))
                {
                    page = Engines[index].Process(pix);
                }
                if (page != null)
                {
                    string text = page.GetText();
                    if (!string.IsNullOrEmpty(text))
                    {
                        r = text;
                    }
                    page.Dispose();
                }
            }

            return r;
        }

        public static Bitmap GetScreenRect(Rectangle ScreenArea)
        {
            Bitmap bmp = new Bitmap(ScreenArea.Width, ScreenArea.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(
                    ScreenArea.Left, 
                    ScreenArea.Top, 
                    0, 
                    0, 
                    new System.Drawing.Size(ScreenArea.Width, ScreenArea.Height)
                    );
            }
            return bmp;
        }

    }
}
