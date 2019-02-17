using AForge.Imaging;
using AForge.Imaging.Filters;
using PoE_Trade_Bot.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoE_Trade_Bot
{
    public class AforgeService
    {
        //найденные совпадения
        private TemplateMatch[] _matchings;

        /// <summary>
        /// Количество найденных совпадений
        /// </summary>
        public int CountMatchings
        {
            get => _matchings != null ? _matchings.Length : 0;
        }


        //ctor
        public AforgeService()
        {

        }

        /// <summary>
        /// Содержит ли исходное изображение представленый образец
        /// </summary>
        /// <param name="pathOriginalImage">путь к файлу исходного изображения</param>
        /// <param name="pathSampleImage">путь к файлу образца</param>
        /// <returns>true если содержит</returns>
        public async Task<bool> IsContains(string pathOriginalImage, string pathSampleImage)
        {
            if (String.IsNullOrEmpty(pathOriginalImage)) throw new ArgumentNullException(nameof(pathOriginalImage));
            if (String.IsNullOrEmpty(pathSampleImage)) throw new ArgumentNullException(nameof(pathSampleImage));

            var sample = new Bitmap(pathSampleImage);
            var orig = new Bitmap(pathOriginalImage);

            ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching(0.91f);
            _matchings = await Task.Run(() => tm.ProcessImage(orig, sample));

            return _matchings.Any();
        }

        public async Task<bool> IsContains(System.Drawing.Image originalImage, string pathSampleImage)
        {
            if (originalImage == null) throw new ArgumentNullException(nameof(originalImage));
            if (String.IsNullOrEmpty(pathSampleImage)) throw new ArgumentNullException(nameof(pathSampleImage));

            var sample = new Bitmap(pathSampleImage);
            var orig = ConvertToFormat(originalImage, PixelFormat.Format24bppRgb);

            ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching(0.9f);
            _matchings = await Task.Run(() => tm.ProcessImage(orig, sample));

            return _matchings.Any();
        }


        /// <summary>
        /// Получение коллекции найденных мест где находится образец
        /// </summary>
        /// <returns>коллекция найденных мест</returns>
        public List<FoundPlace> GetPlaces()
        {
            List<FoundPlace> result = new List<FoundPlace>();
            if (CountMatchings == 0) return result;

            int id = 0;
            foreach (var match in _matchings)
            {
                FoundPlace place = new FoundPlace
                {
                    Id = ++id,
                    Similarity = match.Similarity,
                    Top = match.Rectangle.Top,
                    Left = match.Rectangle.Left,
                    Height = match.Rectangle.Height,
                    Width = match.Rectangle.Width
                };

                result.Add(place);
            }

            return result;
        }

        public static Bitmap ConvertToFormat(System.Drawing.Image image, PixelFormat format)
        {
            Bitmap copy = new Bitmap(image.Width, image.Height, format);
            using (Graphics gr = Graphics.FromImage(copy))
            {
                gr.DrawImage(image, new Rectangle(0, 0, copy.Width, copy.Height));
            }
            return copy;
        }
    }
}
