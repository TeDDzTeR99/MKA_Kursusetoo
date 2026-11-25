using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
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

        private const int Margin = 12;   // space between the picture and the page edge / the text

        // Puts the item's picture at the right edge of a details page's grid, beside the first rows,
        // with the data "cut out" around it: the rows next to the picture end before it, rows below it use
        // the full width. The picture is drawn by the grid itself and scrolls with the rows, so scrolling
        // costs nothing extra; the cut-out is recalculated only when the data or the width changes.
        public static void AttachCutout(string kind, int id, DataGridView grid)
        {
            var image = Load(kind, id, DetailsSize) ?? Placeholder(DetailsSize);
            grid.Disposed += (s, e) => image.Dispose();   // this page's own copy

            var reserved = DetailsSize + 2 * Margin;   // width kept free on the right of the rows beside it

            // Buffered, so the rows and the picture drawn over them appear together without flicker
            typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(grid, true);

            var busy = false;   // padding changes resize rows, which must not start another pass
            void Relayout()
            {
                if (busy || grid.Columns["Value"] == null)
                    return;
                busy = true;
                try
                {
                    CutOutRows(grid, DetailsSize + Margin, reserved);
                    // Rows keep an old height after the columns or their padding change; measure them all again
                    grid.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
                    grid.Invalidate();
                }
                finally { busy = false; }
            }

            // Top level with the first row, right edge a margin inside the Value column (left of the scrollbar)
            grid.Paint += (s, e) =>
            {
                var value = grid.Columns["Value"];
                if (value == null || grid.Rows.Count == 0)
                    return;
                var right = grid.GetColumnDisplayRectangle(value.Index, false).Right;
                // 1 = the grid's border (column headers are hidden on details pages); negative once scrolled
                var top = 1 - grid.VerticalScrollingOffset;
                e.Graphics.DrawImage(image, right - DetailsSize - Margin, top, DetailsSize, DetailsSize);
            };

            // The rows next to the picture end before it: their cut-out part is painted as page background,
            // not as a row (otherwise the rows, with their lines, would run on underneath the picture)
            grid.CellPainting += (s, e) =>
            {
                if (e.RowIndex < 0 || grid.Columns["Value"]?.Index != e.ColumnIndex
                    || e.CellStyle.Padding.Right < reserved)
                    return;

                e.Paint(e.ClipBounds, DataGridViewPaintParts.All);
                using (var background = new SolidBrush(Theme.Background))
                    e.Graphics.FillRectangle(background, e.CellBounds.Right - reserved, e.CellBounds.Top,
                                             reserved, e.CellBounds.Height);
                e.Handled = true;
            };

            grid.DataBindingComplete += (s, e) => Relayout();
            grid.Resize += (s, e) => Relayout();   // a new width wraps the text differently
        }

        // Gives the Value cells of the rows that start above cutoutBottom (pixels below the top of the first row)
        // extra right padding; all other rows get the grid's normal padding. Row heights change with the
        // padding (text wraps sooner), so each row's top is found from the preferred heights of the rows above it.
        private static void CutOutRows(DataGridView grid, int cutoutBottom, int reserved)
        {
            var normal = grid.DefaultCellStyle.Padding;
            var cut = new Padding(normal.Left, normal.Top, normal.Right + reserved, normal.Bottom);
            var valueIndex = grid.Columns["Value"].Index;

            var top = 0;
            foreach (DataGridViewRow row in grid.Rows)
            {
                var cell = row.Cells[valueIndex];
                var padding = top < cutoutBottom ? cut : normal;
                if (cell.Style.Padding != padding)
                    cell.Style.Padding = padding;
                if (top < cutoutBottom)   // rows past the picture needn't be measured
                    top += row.GetPreferredHeight(row.Index, DataGridViewAutoSizeRowMode.AllCells, true);
            }
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
