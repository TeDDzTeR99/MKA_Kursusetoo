using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
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

        // Puts the item's picture at the right edge of a details page, its top level with the first grid row,
        // with the data "cut out" around it: the rows next to the picture end before it, rows below it use
        // the full width. Recalculated when the data, the scroll position or the width changes.
        public static void AttachCutout(Control page, string kind, int id, DataGridView grid)
        {
            var picture = new PictureBox
            {
                Size = new Size(DetailsSize, DetailsSize),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Theme.Background,
                Image = Load(kind, id, DetailsSize) ?? Placeholder(DetailsSize)
            };
            picture.Disposed += (s, e) => picture.Image?.Dispose();   // this page's own copy
            page.Controls.Add(picture);
            picture.BringToFront();   // drawn over the grid, not docked, so it doesn't take a strip of its own

            var reserved = DetailsSize + 2 * Margin;   // width kept free on the right of the rows beside it

            var busy = false;   // padding changes resize rows, which must not start another pass
            void Relayout()
            {
                if (busy || grid.Columns["Value"] == null)
                    return;
                busy = true;
                try
                {
                    // Left of the grid's scrollbar, so it stays usable; top level with the first row
                    // (1 = the grid's border; column headers are hidden on details pages)
                    var scrollBar = grid.Controls.OfType<VScrollBar>().Any(b => b.Visible)
                        ? SystemInformation.VerticalScrollBarWidth : 0;
                    var gridTop = page.PointToClient(grid.PointToScreen(Point.Empty)).Y;
                    picture.Location = new Point(page.ClientSize.Width - DetailsSize - Margin - scrollBar, gridTop + 1);
                    CutOutRows(grid, 1 + DetailsSize + Margin, reserved);
                }
                finally { busy = false; }
            }

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
            grid.Scroll += (s, e) => Relayout();   // other rows are next to the picture now
            page.Resize += (s, e) => Relayout();
            page.Layout += (s, e) => Relayout();
        }

        // Gives the Value cells of the visible rows that start above cutoutBottom (in grid coordinates)
        // extra right padding; all other rows get the grid's normal padding. Row heights change with the
        // padding (text wraps sooner), so each row's top is found from the preferred heights of the rows
        // above it, counting from the first row scrolled into view.
        private static void CutOutRows(DataGridView grid, int cutoutBottom, int reserved)
        {
            var normal = grid.DefaultCellStyle.Padding;
            var cut = new Padding(normal.Left, normal.Top, normal.Right + reserved, normal.Bottom);
            var valueIndex = grid.Columns["Value"].Index;
            var first = Math.Max(0, grid.FirstDisplayedScrollingRowIndex);

            var top = 1;   // grid border; column headers are hidden on details pages
            foreach (DataGridViewRow row in grid.Rows)
            {
                var cell = row.Cells[valueIndex];
                var padding = row.Index >= first && top < cutoutBottom ? cut : normal;
                if (cell.Style.Padding != padding)
                    cell.Style.Padding = padding;
                if (row.Index >= first)
                    top += row.GetPreferredHeight(row.Index, DataGridViewAutoSizeRowMode.AllCells, true);
            }

            // Rows keep an old height after the columns or their padding change; measure them all again
            grid.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
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
