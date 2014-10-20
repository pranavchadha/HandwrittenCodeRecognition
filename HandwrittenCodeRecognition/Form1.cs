using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;

namespace HandwrittenCodeRecognition
{
    public partial class Form1 : Form
    {
        Image<Gray, byte> first;
        
        public Form1()
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

        private void normalise_Click(object sender, EventArgs e)
        {
            if (first == null)
            {
                open();
            }

            float x = first.Height / first.Width;
            Image<Gray, byte> final = new Image<Gray, byte>(720, (int)(x*720));
            
            CvInvoke.cvResize(first, final, Emgu.CV.CvEnum.INTER.CV_INTER_AREA);
            final = final.Not();
            Image<Gray, byte> bg = new Image<Gray, byte>(final.Width, final.Height);
            IntPtr se = CvInvoke.cvCreateStructuringElementEx(9, 9, 5, 5, Emgu.CV.CvEnum.CV_ELEMENT_SHAPE.CV_SHAPE_ELLIPSE, IntPtr.Zero);
            CvInvoke.cvErode(final, bg, se, 1);

            Image<Gray, byte> foreground = final - bg;
            Image<Gray, byte> thresh = new Image<Gray,byte>(foreground.Width, foreground.Height);
            CvInvoke.cvThreshold(foreground, thresh , 0, 255, Emgu.CV.CvEnum.THRESH.CV_THRESH_OTSU);
            
            

            imageBox1.Image = final;
            imageBox2.Image = final-bg;
        }

        
    }
}
