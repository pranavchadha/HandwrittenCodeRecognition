using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Emgu.CV;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using Emgu.Util;

namespace HandwrittenCodeRecognition
{
    public partial class Form1 : Form
    {
        Image<Gray, byte> first;
        private Tesseract _ocr;
        
        public Form1()
        {
            InitializeComponent();
            _ocr = new Tesseract("C://Users//Pranav Chadha//Documents//GitHub//HandwrittenCodeRecognition//HandwrittenCodeRecognition", "eng", Tesseract.OcrEngineMode.OEM_TESSERACT_CUBE_COMBINED);
        }

        public void open()
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                first = new Image<Gray, byte>(openFileDialog1.FileName);
                imageBox1.Image = first;
            }
        }

        private void upload_Click(object sender, EventArgs e)
        {
            open();
        }

        private void normalise_Click(object sender, EventArgs e)
        {
            if (first == null)
            {
                open();
            }

            double x = first.Height / first.Width;
            Image<Gray, byte> final = new Image<Gray, byte>((int)(first.Width/4), (int)(first.Height/4));
            
            CvInvoke.cvResize(first, final, Emgu.CV.CvEnum.INTER.CV_INTER_AREA);
            final = final.Not();
            Image<Gray, byte> bg = new Image<Gray, byte>(final.Width, final.Height);
            IntPtr se = CvInvoke.cvCreateStructuringElementEx(11, 11, 6, 6, Emgu.CV.CvEnum.CV_ELEMENT_SHAPE.CV_SHAPE_ELLIPSE, IntPtr.Zero);
            CvInvoke.cvErode(final, bg, se, 1);

            Image<Gray, byte> foreground = final - bg;
            Image<Gray, byte> thresh = new Image<Gray, byte>(foreground.Width, foreground.Height);
            CvInvoke.cvThreshold(foreground, thresh, 100, 255, Emgu.CV.CvEnum.THRESH.CV_THRESH_OTSU);
            //CvInvoke.cvAdaptiveThreshold(final, thresh, 255, Emgu.CV.CvEnum.ADAPTIVE_THRESHOLD_TYPE.CV_ADAPTIVE_THRESH_MEAN_C, Emgu.CV.CvEnum.THRESH.CV_THRESH_BINARY, 25, 10);
            //thresh = final.ThresholdAdaptive(new Gray(255), Emgu.CV.CvEnum.ADAPTIVE_THRESHOLD_TYPE.CV_ADAPTIVE_THRESH_MEAN_C, Emgu.CV.CvEnum.THRESH.CV_THRESH_OTSU, 25, new Gray(10));



            //imageBox1.Image = foreground;

            Image<Gray, byte> second = new Image<Gray, byte>(final.Size);
            thresh.CopyTo(second);

            Contour<Point> cont = thresh.FindContours();
            int count = 0;
            int total = 0;

            for (; cont != null;cont = cont.HNext)
            {
                total++;
                //cont = cont.ApproxPoly(0.0025);
                second.Draw(cont.BoundingRectangle, new Gray(255), 1);
                if (cont.BoundingRectangle.Height * cont.BoundingRectangle.Width < 10)
                {
                    
                    count++;
                    CvInvoke.cvDrawContours(thresh, cont, new MCvScalar(0), new MCvScalar(0), 0, -1, Emgu.CV.CvEnum.LINE_TYPE.EIGHT_CONNECTED, new Point(0, 0));
                }
            }

            Console.Out.WriteLine(count);
            Console.Out.WriteLine(total);

            Image<Gray, byte> houghlines = new Image<Gray, byte>(thresh.Size);

            thresh = thresh.Not();

            LineSegment2D[][] hough = thresh.HoughLines(180, 120, 1, Math.PI / 180, 20, 70, 10);
            double angle = 0;
            for (int i = 0; i < hough[0].Length; i++)
            {
                houghlines.Draw(hough[0][i], new Gray(255), 2);
                angle += Math.Atan2(hough[0][i].P2.Y - hough[0][i].P1.Y, hough[0][i].P2.X - hough[0][i].P1.X);
            }

            //Console.Out.WriteLine(hough[0][1].P1);
            //Console.Out.WriteLine(hough[0][1].P2);
            angle = (angle / hough[0].Length) * 180 / Math.PI;
            Console.Out.WriteLine(angle);


            Point center = new Point((int)((thresh.Width - 1)/2), (int)((thresh.Height - 1)/2));
            //thresh = thresh.Rotate(-angle, new Gray(255));

            thresh.Save("thresh1.tif");

            _ocr.Recognize(thresh);
            Tesseract.Charactor[] charactors = _ocr.GetCharactors();
            foreach (Tesseract.Charactor c in charactors)
            {
                thresh.Draw(c.Region, new Gray(100), 1);
            }

            

            //String text = String.Concat( Array.ConvertAll(charactors, delegate(Tesseract.Charactor t) { return t.Text; }) );
            String text = _ocr.GetText();
            Console.Out.WriteLine("------------------------");
            Console.Out.WriteLine(text);
            Console.Out.WriteLine("------------------------");
            imageBox2.Image = thresh;
            imageBox1.Image = second;

        }

        
    }
}
