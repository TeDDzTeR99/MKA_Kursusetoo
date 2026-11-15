using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MKA_KursuseToo
{
    // Dark theme with gold accents, used by every window so they all look the same
    internal static class Theme
    {
        // Colours
        public static readonly Color Background   = Color.FromArgb(18, 18, 18);
        public static readonly Color Surface      = Color.FromArgb(30, 30, 30);
        public static readonly Color SurfaceAlt   = Color.FromArgb(38, 38, 38);
        public static readonly Color HeaderBack   = Color.FromArgb(42, 36, 24);
        public static readonly Color Border       = Color.FromArgb(58, 58, 58);
        public static readonly Color Text         = Color.FromArgb(224, 220, 207);
        public static readonly Color TextMuted    = Color.FromArgb(160, 154, 138);
        public static readonly Color Accent       = Color.FromArgb(201, 168, 106);
        public static readonly Color AccentHover  = Color.FromArgb(232, 202, 140);
        public static readonly Color Selection    = Color.FromArgb(68, 56, 34);

        // Fonts
        public static readonly Font BodyFont    = new Font("Segoe UI", 10.5F);
        public static readonly Font BoldFont    = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        public static readonly Font TitleFont   = new Font("Georgia", 20F, FontStyle.Bold);

        // Applies the theme to a window and everything inside it
        public static void Apply(Form form)
        {
            form.BackColor = Background;
            form.ForeColor = Text;
            UseDarkTitleBar(form);
            ApplyToChildren(form);
        }

        private static void ApplyToChildren(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                switch (control)
                {
                    case Button button:
                        StyleButton(button);
                        break;
                    case TextBox textBox:
                        textBox.BackColor = SurfaceAlt;
                        textBox.ForeColor = Text;
                        textBox.BorderStyle = BorderStyle.FixedSingle;
                        break;
                    case ComboBox comboBox:
                        comboBox.FlatStyle = FlatStyle.Flat;
                        comboBox.BackColor = SurfaceAlt;
                        comboBox.ForeColor = Text;
                        break;
                    case DataGridView grid:
                        StyleGrid(grid);
                        break;
                    case Label label:
                        label.ForeColor = Text;
                        break;
                }

                ApplyToChildren(control);
            }
        }

        public static void StyleButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Accent;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = HeaderBack;
            button.FlatAppearance.MouseDownBackColor = Selection;
            button.BackColor = SurfaceAlt;
            button.ForeColor = Accent;
            button.Font = BoldFont;
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
        }

        public static void StyleGrid(DataGridView grid)
        {
            grid.BackgroundColor = Surface;
            grid.BorderStyle = BorderStyle.None;
            grid.GridColor = Border;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.EnableHeadersVisualStyles = false;   // otherwise Windows ignores our header colours

            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderBack;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Accent;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = HeaderBack;
            grid.ColumnHeadersDefaultCellStyle.Font = BoldFont;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 6, 6, 6);

            grid.DefaultCellStyle.BackColor = Surface;
            grid.DefaultCellStyle.ForeColor = Text;
            grid.DefaultCellStyle.SelectionBackColor = Selection;
            grid.DefaultCellStyle.SelectionForeColor = Text;
            grid.DefaultCellStyle.Font = BodyFont;
            grid.DefaultCellStyle.Padding = new Padding(6, 3, 6, 3);
            grid.AlternatingRowsDefaultCellStyle.BackColor = SurfaceAlt;

            grid.RowTemplate.Height = 30;
        }

        // Clickable link columns in gold, underlined on hover
        public static void StyleLinkColumn(DataGridViewLinkColumn column)
        {
            column.LinkColor = Accent;
            column.ActiveLinkColor = AccentHover;
            column.VisitedLinkColor = Accent;
            column.LinkBehavior = LinkBehavior.HoverUnderline;
        }

        // Windows 10 (1809+) and 11 can draw the title bar dark. Older Windows just ignores this.
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        private const int DwmUseImmersiveDarkMode = 20;

        private static void UseDarkTitleBar(Form form)
        {
            try
            {
                int enabled = 1;
                DwmSetWindowAttribute(form.Handle, DwmUseImmersiveDarkMode, ref enabled, sizeof(int));
            }
            catch (Exception)
            {
                // Not supported on this Windows version, keep the normal title bar
            }
        }
    }
}
