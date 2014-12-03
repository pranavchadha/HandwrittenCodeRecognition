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
            _ocr = new Tesseract("tess", "pc", Tesseract.OcrEngineMode.OEM_TESSERACT_ONLY);
            _ocr.SetVariable("language_model_penalty_non_freq_dict_word", "1.0");
	        _ocr.SetVariable("language_model_penalty_non_dict_word", "1.0");
            _ocr.SetVariable("segment_penalty_dict_frequent_word", "0");
	        _ocr.SetVariable("segment_penalty_dict_nonword", "2");
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

            Image<Gray, byte> test = new Image<Gray, byte>(first.Size);
            first.CopyTo(test);
            //test = test.Not();

            //imageBox2.Image = test;
            Console.Out.WriteLine("Hello");

            Image<Gray, byte> final = new Image<Gray, byte>((int)(((float)first.Width / (float)first.Height) * 1080), 1080);
            CvInvoke.cvResize(test, final, Emgu.CV.CvEnum.INTER.CV_INTER_AREA);
            final = final.Not();

            Image<Gray, byte> bg = new Image<Gray, byte>(final.Width, final.Height);
            IntPtr se = CvInvoke.cvCreateStructuringElementEx(9, 9, 5, 5, Emgu.CV.CvEnum.CV_ELEMENT_SHAPE.CV_SHAPE_ELLIPSE, IntPtr.Zero);
            CvInvoke.cvErode(final, bg, se, 3);

            imageBox1.Image = final-bg;
            imageBox2.Image = bg;

            Image<Gray, byte> foreground = final - bg;
            Image<Gray, byte> thresh = new Image<Gray, byte>(foreground.Width, foreground.Height);
            //CvInvoke.cvAdaptiveThreshold(foreground, thresh, 255, Emgu.CV.CvEnum.ADAPTIVE_THRESHOLD_TYPE.CV_ADAPTIVE_THRESH_MEAN_C, Emgu.CV.CvEnum.THRESH.CV_THRESH_OTSU, 75, -10);
            thresh = foreground.ThresholdAdaptive(new Gray(255), Emgu.CV.CvEnum.ADAPTIVE_THRESHOLD_TYPE.CV_ADAPTIVE_THRESH_MEAN_C, Emgu.CV.CvEnum.THRESH.CV_THRESH_BINARY, 75, new Gray(-10));
            imageBox2.Image = thresh;


            CvInvoke.cvSmooth(thresh, thresh, Emgu.CV.CvEnum.SMOOTH_TYPE.CV_MEDIAN, 3, 3, 0, 0);

            Contour<Point> cont = thresh.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_NONE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_CCOMP);
            int count = 0;
            int total = 0;

            Image<Gray, byte> second = new Image<Gray, byte>(final.Size);

            for (; cont != null; cont = cont.HNext)
            {
                total++;
                //cont = cont.ApproxPoly(0.0025);
                //second.Draw(cont.BoundingRectangle, new Gray(255), 1);
                if (cont.Area < 25)
                {

                    count++;
                    CvInvoke.cvDrawContours(second, cont, new MCvScalar(255), new MCvScalar(255), 0, -1, Emgu.CV.CvEnum.LINE_TYPE.EIGHT_CONNECTED, new Point(0, 0));
                }
            }

            //imageBox1.Image = thresh - second;
            thresh = thresh - second;

            thresh = thresh.Not();
            imageBox1.Image = thresh.Copy();
            Image<Gray, byte> houghlines = new Image<Gray, byte>(thresh.Size);

            LineSegment2D[][] hough = thresh.HoughLines(180, 120, 1, Math.PI / 180, 20, 60, 10);
            //LineSegment2D[][] hough = thresh.HoughLines(180, 120, Math.PI / 36, 1, 20, 30, 10);
            double angle, angle1 = 0;
            double angle2 = 0;
            int ct = 0;
            int ct2 = 0;
            for (int i = 0; i < hough[0].Length; i++)
            {
                houghlines.Draw(hough[0][i], new Gray(255), 2);
                double theta = Math.Atan2(hough[0][i].P2.Y - hough[0][i].P1.Y, hough[0][i].P2.X - hough[0][i].P1.X) * 180 / Math.PI;

                Console.Out.Write("theta: ");
                Console.Out.WriteLine(theta);

                if (Math.Abs(theta) > 60)
                {
                    //Console.Out.Write("original: ");
                    //Console.Out.WriteLine(theta);
                    if (theta > 0)
                    {
                        theta = theta - 90;
                    }
                    else
                    {
                        theta = theta + 90;
                    }

                    //Console.Out.Write("corrected: ");
                    //Console.Out.WriteLine(theta);
                    angle1 += theta;
                    ct++;
                }
                else
                {
                    angle2 += theta;
                    ct2++;
                }


            }

            angle1 = (angle1 / ct);
            angle2 = angle2 / ct2;

            imageBox1.Image = thresh.Copy();
            Point center = new Point((int)((thresh.Width - 1) / 2), (int)((thresh.Height - 1) / 2));

            if (ct > ct2)
            {
                angle = angle1;
            }
            else
            {
                angle = angle2;
            }

            Console.Out.Write("angle: ");
            Console.Out.WriteLine(angle);

            if (Math.Abs(angle) >= 8)
            {
                thresh = thresh.Rotate(-angle, new Gray(255));
                CvInvoke.cvThreshold(thresh, thresh, 128, 255, Emgu.CV.CvEnum.THRESH.CV_THRESH_BINARY);
                IntPtr str = CvInvoke.cvCreateStructuringElementEx(3, 3, 1, 1, Emgu.CV.CvEnum.CV_ELEMENT_SHAPE.CV_SHAPE_RECT, IntPtr.Zero);
                CvInvoke.cvMorphologyEx(thresh.Not(), thresh, IntPtr.Zero, str, Emgu.CV.CvEnum.CV_MORPH_OP.CV_MOP_CLOSE, 1);

            }

            imageBox2.Image = thresh.Not();
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

            //double x = first.Height / first.Width;
            //Image<Gray, byte> final = new Image<Gray, byte>((int)(first.Width), (int)(first.Height));
            
            //CvInvoke.cvResize(first, final, Emgu.CV.CvEnum.INTER.CV_INTER_AREA);
            //final = final.Not();
            //Image<Gray, byte> bg = new Image<Gray, byte>(final.Width, final.Height);
            //IntPtr se = CvInvoke.cvCreateStructuringElementEx(11, 11, 6, 6, Emgu.CV.CvEnum.CV_ELEMENT_SHAPE.CV_SHAPE_ELLIPSE, IntPtr.Zero);
            //CvInvoke.cvErode(final, bg, se, 1);

            //Image<Gray, byte> foreground = final - bg;
            //Image<Gray, byte> thresh = new Image<Gray, byte>(foreground.Width, foreground.Height);
            //CvInvoke.cvThreshold(foreground, thresh, 100, 255, Emgu.CV.CvEnum.THRESH.CV_THRESH_OTSU);
            ////CvInvoke.cvAdaptiveThreshold(final, thresh, 255, Emgu.CV.CvEnum.ADAPTIVE_THRESHOLD_TYPE.CV_ADAPTIVE_THRESH_MEAN_C, Emgu.CV.CvEnum.THRESH.CV_THRESH_BINARY, 25, 10);
            ////thresh = final.ThresholdAdaptive(new Gray(255), Emgu.CV.CvEnum.ADAPTIVE_THRESHOLD_TYPE.CV_ADAPTIVE_THRESH_MEAN_C, Emgu.CV.CvEnum.THRESH.CV_THRESH_OTSU, 25, new Gray(10));



            ////imageBox1.Image = foreground;

            //Image<Gray, byte> second = new Image<Gray, byte>(final.Size);
            //thresh.CopyTo(second);

            //Contour<Point> cont = thresh.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_NONE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_CCOMP);
            //int count = 0;
            //int total = 0;

            //for (; cont != null;cont = cont.HNext)
            //{
            //    total++;
            //    //cont = cont.ApproxPoly(0.0025);
            //    second.Draw(cont.BoundingRectangle, new Gray(255), 1);
            //    if (cont.BoundingRectangle.Height * cont.BoundingRectangle.Width < 10)
            //    {
                    
            //        count++;
            //        CvInvoke.cvDrawContours(thresh, cont, new MCvScalar(0), new MCvScalar(0), 0, -1, Emgu.CV.CvEnum.LINE_TYPE.EIGHT_CONNECTED, new Point(0, 0));
            //    }
            //}

            //Console.Out.WriteLine(count);
            //Console.Out.WriteLine(total);

            //Image<Gray, byte> houghlines = new Image<Gray, byte>(thresh.Size);
            
            //thresh = thresh.Not();

            //LineSegment2D[][] hough = thresh.HoughLines(180, 120, 1, Math.PI / 180, 20, 70, 10);
            //double angle = 0;
            //for (int i = 0; i < hough[0].Length; i++)
            //{
            //    houghlines.Draw(hough[0][i], new Gray(255), 2);
            //    angle += Math.Atan2(hough[0][i].P2.Y - hough[0][i].P1.Y, hough[0][i].P2.X - hough[0][i].P1.X);
            //}

            ////Console.Out.WriteLine(hough[0][1].P1);
            ////Console.Out.WriteLine(hough[0][1].P2);
            //angle = (angle / hough[0].Length) * 180 / Math.PI;
            //Console.Out.WriteLine(angle);


            //Point center = new Point((int)((thresh.Width - 1)/2), (int)((thresh.Height - 1)/2));
            ////thresh = thresh.Rotate(-angle, new Gray(255));

            

            //_ocr.Recognize(thresh);
            //Tesseract.Charactor[] charactors = _ocr.GetCharactors();
            //foreach (Tesseract.Charactor c in charactors)
            //{
            //    thresh.Draw(c.Region, new Gray(100), 1);
            //}

            

            ////String text = String.Concat( Array.ConvertAll(charactors, delegate(Tesseract.Charactor t) { return t.Text; }) );
            //String text = _ocr.GetText();
            //Console.Out.WriteLine("------------------------");
            //Console.Out.WriteLine(text);
            //Console.Out.WriteLine("------------------------");
            //imageBox2.Image = thresh;
            //imageBox1.Image = second;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form2 form = new Form2();
            form.ShowDialog();

        }


        public Int32 levenshtein(String a, String b)
        {

            if (string.IsNullOrEmpty(a))
            {
                if (!string.IsNullOrEmpty(b))
                {
                    return b.Length;
                }
                return 0;
            }

            if (string.IsNullOrEmpty(b))
            {
                if (!string.IsNullOrEmpty(a))
                {
                    return a.Length;
                }
                return 0;
            }

            Int32 cost;
            Int32[,] d = new int[a.Length + 1, b.Length + 1];
            Int32 min1;
            Int32 min2;
            Int32 min3;

            for (Int32 i = 0; i <= d.GetUpperBound(0); i += 1)
            {
                d[i, 0] = i;
            }

            for (Int32 i = 0; i <= d.GetUpperBound(1); i += 1)
            {
                d[0, i] = i;
            }

            for (Int32 i = 1; i <= d.GetUpperBound(0); i += 1)
            {
                for (Int32 j = 1; j <= d.GetUpperBound(1); j += 1)
                {
                    cost = Convert.ToInt32(!(a[i - 1] == b[j - 1]));

                    min1 = d[i - 1, j] + 1;
                    min2 = d[i, j - 1] + 1;
                    min3 = d[i - 1, j - 1] + cost;
                    d[i, j] = Math.Min(Math.Min(min1, min2), min3);
                }
            }

            return d[d.GetUpperBound(0), d.GetUpperBound(1)];

        }

        
    }
}
