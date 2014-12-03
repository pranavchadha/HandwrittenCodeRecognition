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
    public partial class Form2 : Form
    {
        Image<Gray, byte> first;
        private Tesseract _ocr;
        public Form2()
        {
            InitializeComponent();
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

        private void button1_Click(object sender, EventArgs e)
        {
            if (first == null)
            {
                open();
            }
            Image<Gray, byte> test = new Image<Gray,byte>(first.Size);
            first.CopyTo(test);
            //test = test.Not();

            //imageBox2.Image = test;
            Console.Out.WriteLine("Hello");
            
            Image<Gray, byte> final = new Image<Gray, byte>((int)(((float)first.Width / (float)first.Height)* 1080), 1080);
            CvInvoke.cvResize(test, final, Emgu.CV.CvEnum.INTER.CV_INTER_AREA);
            final = final.Not();
            //imageBox1.Image = final;

            Image<Gray, byte> bg = new Image<Gray, byte>(final.Width, final.Height);
            IntPtr se = CvInvoke.cvCreateStructuringElementEx(9, 9, 5, 5, Emgu.CV.CvEnum.CV_ELEMENT_SHAPE.CV_SHAPE_ELLIPSE, IntPtr.Zero);
            CvInvoke.cvErode(final, bg, se, 3);

            
            Image<Gray, byte> foreground = final - bg;
            Image<Gray, byte> thresh = new Image<Gray, byte>(foreground.Width, foreground.Height);
            CvInvoke.cvAdaptiveThreshold(foreground, thresh, 255, Emgu.CV.CvEnum.ADAPTIVE_THRESHOLD_TYPE.CV_ADAPTIVE_THRESH_MEAN_C, Emgu.CV.CvEnum.THRESH.CV_THRESH_BINARY, 41, -10);
            CvInvoke.cvSmooth(thresh, thresh, Emgu.CV.CvEnum.SMOOTH_TYPE.CV_MEDIAN, 3, 3, 0, 0);


            //imageBox1.Image = foreground;
            //imageBox2.Image = thresh;

            Image<Gray, byte> second = new Image<Gray, byte>(final.Size);
            //thresh.CopyTo(second);

            Contour<Point> cont = thresh.FindContours();
            int count = 0;
            int total = 0;

            for (; cont != null; cont = cont.HNext)
            {
                total++;
                //cont = cont.ApproxPoly(0.0025);
                //second.Draw(cont.BoundingRectangle, new Gray(255), 1);
                if (cont.Area < 16)
                {

                    count++;
                    CvInvoke.cvDrawContours(second, cont, new MCvScalar(255), new MCvScalar(255), 0, -1, Emgu.CV.CvEnum.LINE_TYPE.EIGHT_CONNECTED, new Point(0, 0));
                }
            }



            IntPtr str = CvInvoke.cvCreateStructuringElementEx(3, 3, 1, 1, Emgu.CV.CvEnum.CV_ELEMENT_SHAPE.CV_SHAPE_RECT, IntPtr.Zero);
            //CvInvoke.cvMorphologyEx(thresh, thresh, IntPtr.Zero, str, Emgu.CV.CvEnum.CV_MORPH_OP.CV_MOP_CLOSE, 1);
           


            thresh = thresh.Not();
            imageBox1.Image = thresh.Copy();
            Image<Gray, byte> houghlines = new Image<Gray, byte>(thresh.Size);

            LineSegment2D[][] hough = thresh.HoughLines(180, 120, 1, Math.PI/180, 20, 50, 10);
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

                if (Math.Abs(theta)>60)
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

            if (Math.Abs(angle)>=8)
            {
                thresh = thresh.Rotate(-angle, new Gray(255));
                CvInvoke.cvThreshold(thresh, thresh, 128, 255, Emgu.CV.CvEnum.THRESH.CV_THRESH_BINARY);
                CvInvoke.cvMorphologyEx(thresh.Not(), thresh, IntPtr.Zero, str, Emgu.CV.CvEnum.CV_MORPH_OP.CV_MOP_CLOSE, 1);

            }



            //thresh = thresh.ThresholdBinary(new Gray(128), new Gray(255));
            //CvInvoke.cvThreshold(thresh, thresh, 128, 255, Emgu.CV.CvEnum.THRESH.CV_THRESH_BINARY);

            
            imageBox2.Image = thresh;
            

            thresh.Save("thresh1.tif");
        }
    }
}
