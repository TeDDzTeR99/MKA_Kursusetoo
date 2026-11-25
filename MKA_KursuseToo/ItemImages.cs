using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace MKA_KursuseToo
{
    // Item pictures: Images\<kind>\<id>.png next to the .exe (copied there on build from the project's
    // Images folder), e.g. Images\talisman\1000.png. <kind> is the category's Kind ("weapon", "talisman",
    // "upgrade_material", ...) and <id> the item's id in the database. Items without a file get an empty frame.
    internal static class ItemImages
    {
        public const int CardSize = 96;      // picture on an item card
        public const int DetailsSize = 128;  // picture on a details page

        // Card pictures, made once per item and kept: a list page is shown again on every Back.
        // Small (96 px), so even a few thousand items stay well under 100 MB.
        private static readonly Dictionary<(string, int), Image> CardImages = new Dictionary<(string, int), Image>();
        private static Image _cardPlaceholder;

        private static string PathOf(string kind, int id) =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", kind, id + ".png");

        // The picture for a card, or the empty frame. Shared: don't dispose it.
        public static Image ForCard(string kind, int id)
        {
            if (!CardImages.TryGetValue((kind, id), out var image))
                CardImages[(kind, id)] = image = Load(kind, id, CardSize);
            return image ?? (_cardPlaceholder ?? (_cardPlaceholder = Placeholder(CardSize)));
        }

        // A picture slot for the top of a details page, left-aligned, sized DetailsSize
        public static Control DetailsSlot(string kind, int id)
        {
            var picture = new PictureBox
            {
                Dock = DockStyle.Left,
                Width = DetailsSize,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = Load(kind, id, DetailsSize) ?? Placeholder(DetailsSize)
            };
            picture.Disposed += (s, e) => picture.Image?.Dispose();   // this page's own copy

            var slot = new Panel { Dock = DockStyle.Top, Height = DetailsSize + 12, Padding = new Padding(12, 12, 12, 0) };
            slot.Controls.Add(picture);
            return slot;
        }

        // The file scaled to fit size x size, or null if there is no file or it can't be read
        private static Image Load(string kind, int id, int size)
        {
            var path = PathOf(kind, id);
            if (!File.Exists(path))
                return null;

            try
            {
                // Read through a stream and copy, so the file isn't kept locked while the app runs
                using (var stream = File.OpenRead(path))
                using (var original = Image.FromStream(stream))
                {
                    var scale = Math.Min(1.0, Math.Min((double)size / original.Width, (double)size / original.Height));
                    var width = Math.Max(1, (int)(original.Width * scale));
                    var height = Math.Max(1, (int)(original.Height * scale));

                    var result = new Bitmap(width, height);
                    using (var g = Graphics.FromImage(result))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(original, 0, 0, width, height);
                    }
                    return result;
                }
            }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is OutOfMemoryException)
            {
                // Not a valid picture (GDI+ reports that as OutOfMemory): show the empty frame instead
                return null;
            }
        }

        // An empty frame in the theme's border colour
        private static Image Placeholder(int size)
        {
            var image = new Bitmap(size, size);
            using (var g = Graphics.FromImage(image))
            using (var pen = new Pen(Theme.Border) { DashStyle = DashStyle.Dash })
            {
                g.DrawRectangle(pen, 0, 0, size - 1, size - 1);
            }
            return image;
        }
    }
}
