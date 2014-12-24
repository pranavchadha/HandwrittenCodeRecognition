using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;

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

            LineSegment2D[][] hough = thresh.HoughLines(180, 120, 1, Math.PI / 180, 20, 50, 10);
            //LineSegment2D[][] hough = thresh.HoughLines(180, 120, Math.PI / 36, 1, 20, 30, 10);
            double angle, angle1 = 0;
            double angle2 = 0;
            int ct = 0;
            int ct2 = 0;
            for (int i = 0; i < hough[0].Length; i++)
            {
                houghlines.Draw(hough[0][i], new Gray(255), 2);
                double theta = Math.Atan2(hough[0][i].P2.Y - hough[0][i].P1.Y, hough[0][i].P2.X - hough[0][i].P1.X) * 180 / Math.PI;

                //Console.Out.Write("theta: ");
                //Console.Out.WriteLine(theta);

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

            //imageBox1.Image = thresh.Copy();
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
            imageBox1.Image = first;
            imageBox2.Image = thresh.Copy();
            
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
            //imageBox2.Image = thresh;
            //imageBox1.Image = second;

            File.WriteAllText("beforepost.txt", text);
            text = postprocess(text);
            Console.Out.WriteLine(text);
            File.WriteAllText("trial.c", text);
            File.WriteAllText("tray.txt", text);

            //String useless = "#include<stdio.h>void main(){printf('Acquiring image...\n'); printf('Pre-Processing... \n'); printf('Running OCR... \n'); printf('Post Processing... \n'); printf('Compiling... \n'); printf('Compiled Successfully \n'); printf('Output: \n');}";
            //File.WriteAllText("op.c", useless);


            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            startInfo.FileName = "cmd.exe";

            startInfo.Arguments = "/k gcc trial.c -o b & gcc op.c & a.exe & b.exe";
            process.StartInfo = startInfo;
            process.Start();

            

        }

        public String postprocess(String a)
        {
            String[] freq = new String[] { "#include<stdio.h>", "#include<string.h>", "#include<stdlib.h>", "#include<stdlib.h>", "#include", "<stdio.h>", "printf", "scanf", "void", "main", "char", "string", "%d\\n"};
            a = ambigs(a);
            a = ambigs(a);

            a = a.Trim();
            if (a[a.Length-1]=='3')
            {
                a = a.TrimEnd('3');
                a += '}';
            }
            if (a[a.Length - 1] != '}')
            {
                a += '}';
            }
            

            //Console.Out.WriteLine(code);

            var reader = new StringReader(a);
            String line;
            int min;
            String str = "";
            String newcode = "";
            while ((line = reader.ReadLine()) != null)
            {
                String[] words = line.Split(new char[] { ',', ';', ' ', '"', '{', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                Console.Out.WriteLine("original line: " + line);
                foreach (String word in words)
                {

                    min = 1000;
                    foreach (String item in freq)
                    {

                        int lev = levenshtein(word, item);
                        if (lev < min)
                        {
                            min = lev;
                            str = item;
                        }
                    }
                    if (min < 3)
                    {
                        line = line.Replace(word, str);
                    }

                    
                }
                newcode += line;
                newcode += "\n";
                Console.Out.WriteLine("final: " + line);
            }

            //String trr = line.Trim();
        


            String code = null, final = null; 
            int flag = 1;
            for (int i = 0; i < newcode.Length; i++)
            {
                code += newcode[i];    
                if(newcode[i] == ')' && flag == 1 )
                    {
                        if(newcode[i+1] != '{')
                            code += "\n";
                        int p = i + 1;
                        while (newcode[p] != '{')
                        {
                            p++;
                            i++;
                        }
                        flag = 0;   
                    }
                else if (newcode[i] == 'i' && newcode[i + 1] == '.')
                    i++;
            }

            Console.Out.WriteLine(newcode);

            var read = new StringReader(code);
            while ((line = read.ReadLine()) != null)
            {
                String x = @"\(\""[^\""]+?\)";
                MatchCollection mc = Regex.Matches(line, x);
                String s = null, p = null;
                foreach (Match m in mc)
                {
                    s = m.ToString();
                    p = null;
                    for (int i = 0; i < s.Length - 1; i++)
                    {

                        p += s[i];
                    }
                    p += '"';
                    p += ')';
                    line = line.Replace(s, p);
                    Console.Out.WriteLine("s -> " + s);
                    Console.Out.WriteLine("p -> " + p);
                    Console.Out.WriteLine("final" + line);
                }

                if (!line.Contains("for") && !line.Contains("while") && !line.Contains("if") && !line.Contains("main") && !line.Contains("include"))
                {
                    String trimmed = line;
                    int l = trimmed.Length - 1;
                    if (l >= 0)
                    {
                        if (trimmed[l] != ';' && trimmed[l] != '{' && trimmed[l] != '}')
                        {
                            p = null;
                            for (int i = 0; i < trimmed.Length; i++)
                            {

                                p += trimmed[i];
                            }
                            p += ';';
                            line = p;
                        }
                    }

                }
    

                //String y = @"\([^\)]+?\n";
                //String temp = line + "\\n";
                //Console.Out.WriteLine(temp);
                //MatchCollection mc1 = Regex.Matches(line, y, RegexOptions.Singleline);
                String s1 = null, p1 = null;
                if (line.Contains('('))
                {
                    String m = line.Split('(')[1];
                    if (!m.Contains(')'))
                    {
                        p1 = null;
                        s1 = m.ToString();

                        for (int i = 0; i < s1.Length - 1; i++)
                        {
                            p1 += s1[i];
                        }

                        if (line.Contains("for") || line.Contains("while") || line.Contains("if"))
                        {
                            if (s1[s1.Length - 1] == ';')
                            {
                                p1 += ')';
                            }
                            else
                            {
                                p1 += s1[s1.Length - 1];
                                p1 += ')';
                            }
                        }
                        else
                        {

                            p1 += ')';
                            p1 += s1[s1.Length - 1];
                        }

                        line = line.Replace(s1, p1);
                    }
                }
                
                
                //foreach (Match m in mc1)
                //{
                //    p1 = null;
                //    s1 = m.ToString();
                //    //s1 = s1.TrimEnd('n');
                //    //s1 = s1.TrimEnd('\\');
                    
                //    for (int i = 0; i < s1.Length - 1; i++)
                //    {
                //        p1 += s1[i];
                //    }

                //    if (line.Contains("for") && line.Contains("while") && line.Contains("if"))
                //    {
                //        if (s1[s1.Length - 1] == ';')
                //        {
                //            p1 += ')';
                //        }
                //        else
                //        {
                //            p1 += s1[s1.Length - 1];
                //            p1 += ')';
                //        }
                //    }
                //    else
                //    {
                        
                //        p1 += ')';
                //        p1 += s1[s1.Length - 1];
                //    }

                    
                //    line = line.Replace(s1, p1);
                //    Console.Out.WriteLine("s -> " + s1);
                //    Console.Out.WriteLine("p -> " + p1);
                //    Console.Out.WriteLine("final" + line);
                //}


                final += line;
                final += "\n";
            }

            return final;
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

        public String ambigs(String a)
        {
            String code = null;
            for (int i = 0; i < a.Length; i++)
            {
                //Console.Out.WriteLine(a[i]);
                if ((a[i] == '.' && a[i + 1] == 'l') || (a[i] == '.' && a[i + 1] == '1') || (a[i] == '.' && a[i + 1] == '"'))
                {
                    //Console.Out.WriteLine("in");
                    code += "i";

                    i++;
                }
                else if (a[i] == '.' && a[i + 1] == ',')
                {
                    code += ";";
                    i++;
                }

                else if ((a[i] == '.' && a[i + 1] == '*' && a[i + 2] == '.') || (a[i] == '.' && a[i + 1] == '/' && a[i + 2] == '.') || (a[i] == '.' && a[i + 1] == '2' && a[i + 2] == '.'))
                {
                    code += "%";
                    i += 2;
                }

                else if ((a[i] == ';' && a[i + 1] == 'n' && a[i + 2] == 't'))
                {
                    code += "int";
                    i += 2;
                }

                else if ((a[i] == ';' && a[i + 1] == 'n' && a[i + 2] == 'c'))
                {
                    code += "inc";
                    i += 2;
                }
                else if (a[i] == '“')
                    code += '"';
                
                else
                {
                    code += a[i];
                }
            }

            return code;
        }
        
    }
}
