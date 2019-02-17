using Emgu.CV;
using Emgu.CV.Structure;
using PoE_Trade_Bot.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoE_Trade_Bot.Services
{
    class OpenCV_Service
    {
        public OpenCV_Service()
        {

        }

        public static List<Position> FindObjects(Bitmap source_img, string path_template)
        {
            List<Position> res_pos = new List<Position>();

            Image<Bgr, byte> source = new Image<Bgr, byte>(source_img); // Image B
            Image<Bgr, byte> template = new Image<Bgr, byte>(path_template); // Image A

            using (Image<Gray, float> result = source.MatchTemplate(template, Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed))
            {
                while (true)
                {
                    double[] minValues, maxValues;
                    Point[] minLocations, maxLocations;

                    result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                    // You can try different values of the threshold. I guess somewhere between 0.75 and 0.95 would be good.

                    if (maxValues[0] > 0.85)
                    {
                        // This is a match. Do something with it, for example draw a rectangle around it.
                        Rectangle match = new Rectangle(maxLocations[0], template.Size);
                        result.Draw(match, new Gray(), 3);

                        res_pos.Add(new Position
                        {
                            Left = maxLocations[0].X,
                            Top = maxLocations[0].Y,
                            Width = template.Size.Width,
                            Height = template.Size.Height
                        });
                    }
                    else
                        break;
                }
                result.Save(@"C:\Users\MrWaip\Desktop\tests\test" + DateTime.Now.ToShortDateString() + ".png");
            }

            return res_pos;
        }

        public static Position FindObject(Bitmap source_img, string path_template)
        {
            Position res = new Position();

            Image<Bgr, byte> source = new Image<Bgr, byte>(source_img); // Image B
            Image<Bgr, byte> template = new Image<Bgr, byte>(path_template); // Image A

            using (Image<Gray, float> result = source.MatchTemplate(template, Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed))
            {
                double[] minValues, maxValues;
                Point[] minLocations, maxLocations;

                result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                // You can try different values of the threshold. I guess somewhere between 0.75 and 0.95 would be good.

                if (maxValues[0] > 0.85)
                {
                    res = new Position
                    {
                        Left = maxLocations[0].X,
                        Top = maxLocations[0].Y,
                        Width = template.Size.Width,
                        Height = template.Size.Height
                    };
                }
                result.Save(@"C:\Users\MrWaip\Desktop\tests\test" + DateTime.Now.ToShortDateString() + ".png");

                source.Dispose();
                template.Dispose();
                result.Dispose();
            }

            return res;
        }

        public static Position FindObject(Bitmap source_img, string path_template, double trashholder)
        {
            Position res = new Position();

            Image<Bgr, byte> source = new Image<Bgr, byte>(source_img); // Image B
            Image<Bgr, byte> template = new Image<Bgr, byte>(path_template); // Image A

            using (Image<Gray, float> result = source.MatchTemplate(template, Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed))
            {
                double[] minValues, maxValues;
                Point[] minLocations, maxLocations;

                result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                // You can try different values of the threshold. I guess somewhere between 0.75 and 0.95 would be good.

                if (maxValues[0] > trashholder)
                {
                    res = new Position
                    {
                        Left = maxLocations[0].X,
                        Top = maxLocations[0].Y,
                        Width = template.Size.Width,
                        Height = template.Size.Height
                    };
                }
                result.Save(@"C:\Users\MrWaip\Desktop\tests\test" + DateTime.Now.ToShortDateString() + ".png");

                source.Dispose();
                template.Dispose();
                result.Dispose();
            }

            return res;
        }

        public static List<Position> FindObjects(Bitmap source_img, string path_template, double trashholder)
        {
            List<Position> res_pos = new List<Position>();

            Image<Bgr, byte> source = new Image<Bgr, byte>(source_img); // Image B
            Image<Bgr, byte> template = new Image<Bgr, byte>(path_template); // Image A

            using (Image<Gray, float> result = source.MatchTemplate(template, Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed))
            {
                while (true)
                {
                    double[] minValues, maxValues;
                    Point[] minLocations, maxLocations;

                    result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                    // You can try different values of the threshold. I guess somewhere between 0.75 and 0.95 would be good.

                    if (maxValues[0] > trashholder)
                    {
                        // This is a match. Do something with it, for example draw a rectangle around it.
                        Rectangle match = new Rectangle(maxLocations[0], template.Size);
                        result.Draw(match, new Gray(), 3);

                        res_pos.Add(new Position
                        {
                            Left = maxLocations[0].X,
                            Top = maxLocations[0].Y,
                            Width = template.Size.Width,
                            Height = template.Size.Height
                        });
                    }
                    else
                        break;
                }
                result.Save(@"C:\Users\MrWaip\Desktop\tests\test" + DateTime.Now.ToShortDateString() + ".png");
            }

            return res_pos;
        }

        internal static List<Position> FindCurrencies(Bitmap source_img, string path_template, double trashholder)
        {
            List<Position> res_pos = new List<Position>();

            Image<Bgr, byte> source = new Image<Bgr, byte>(source_img); // Image B
            Image<Bgr, byte> template = new Image<Bgr, byte>(path_template); // Image A

            template = template.Resize(33, 33, Emgu.CV.CvEnum.Inter.Cubic);
            template.ROI = new Rectangle(0, 11, 33, 24);

            using (Image<Gray, float> result = source.MatchTemplate(template, Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed))
            {
                while (true)
                {
                    double[] minValues, maxValues;
                    Point[] minLocations, maxLocations;

                    result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                    // You can try different values of the threshold. I guess somewhere between 0.75 and 0.95 would be good.

                    if (maxValues[0] > trashholder)
                    {
                        // This is a match. Do something with it, for example draw a rectangle around it.
                        Rectangle match = new Rectangle(maxLocations[0], template.Size);
                        result.Draw(match, new Gray(), 3);

                        res_pos.Add(new Position
                        {
                            Left = maxLocations[0].X,
                            Top = maxLocations[0].Y,
                            Width = template.Size.Width,
                            Height = template.Size.Height
                        });
                    }
                    else
                        break;
                }
                result.Save(@"C:\Users\MrWaip\Desktop\tests\test" + DateTime.Now.ToShortDateString() + ".png");
            }

            return res_pos;        
        }
    }
}
