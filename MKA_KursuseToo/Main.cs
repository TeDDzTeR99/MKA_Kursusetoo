using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MKA_KursuseToo.Properties;

namespace MKA_KursuseToo
{
    public partial class Main : Form
    {
        // One kind of item on the home page (Weapons, Talismans, ...). To add a new category
        // (e.g. armor), add one entry to the list in the constructor, or fill in its placeholder.
        private sealed class Category
        {
            public string Kind;             // identifies the category in code, e.g. "weapon"
            public string TitleKey;         // resource name of the title, e.g. "CategoryWeapons"
            public string CountSql;         // number of items, shown on the home page button
            public string SearchSql;        // must return Id and Name; @search is the LIKE pattern
            public Action Open;             // what clicking the home page button shows
            public Action<int> OpenItem;    // opens the details window for one item

            // A category without data yet: a greyed-out "Coming soon" button that can't be clicked
            // and isn't searched
            public bool IsPlaceholder => Open == null;

            // Shown on the home page button and as the search result heading. Looked up each time,
            // so it follows the language switch.
            public string Title => Resources.ResourceManager.GetString(TitleKey, Resources.Culture);
        }

        private static Category Placeholder(string titleKey) => new Category { TitleKey = titleKey };

        private readonly List<Category> _categories;

        // Pages visited before the current one, for the Back button. A page is the method that draws it.
        private readonly Stack<Action> _history = new Stack<Action>();
        private Action _currentPage;

        public Main()
        {
            UseSavedLanguage();
            InitializeComponent();

            // In the order the game's inventory lists them
            _categories = new List<Category>
            {
                new Category
                {
                    Kind = "weapon",
                    TitleKey = nameof(Resources.CategoryWeapons),
                    CountSql = "SELECT COUNT(*) FROM weapons;",
                    SearchSql = @"SELECT id AS Id, name AS Name FROM weapons
                                  WHERE name LIKE @search ESCAPE '\' ORDER BY name;",
                    Open = () => NavigateTo(ShowWeaponTypes),
                    OpenItem = OpenWeapon
                },
                Placeholder(nameof(Resources.CategoryShields)),
                Placeholder(nameof(Resources.CategoryArrowsAndBolts)),
                Placeholder(nameof(Resources.CategoryArmor)),
                new Category
                {
                    Kind = "talisman",
                    TitleKey = nameof(Resources.CategoryTalismans),
                    CountSql = "SELECT COUNT(*) FROM talismans;",
                    SearchSql = @"SELECT id AS Id, name AS Name FROM talismans
                                  WHERE name LIKE @search ESCAPE '\' ORDER BY name;",
                    Open = () => NavigateTo(ShowTalismans),
                    OpenItem = OpenTalisman
                },
                Placeholder(nameof(Resources.CategorySorceries)),
                Placeholder(nameof(Resources.CategoryIncantations)),
                Placeholder(nameof(Resources.CategoryAshesOfWar)),
                Placeholder(nameof(Resources.CategorySpiritAshes)),
                Placeholder(nameof(Resources.CategoryConsumables)),
                Placeholder(nameof(Resources.CategoryCraftingMaterials)),
                new Category
                {
                    Kind = "upgrade_material",
                    TitleKey = nameof(Resources.CategoryUpgradeMaterials),
                    CountSql = "SELECT COUNT(*) FROM upgrade_materials;",
                    SearchSql = @"SELECT id AS Id, name AS Name FROM upgrade_materials
                                  WHERE name LIKE @search ESCAPE '' ORDER BY name;",
                    Open = () => NavigateTo(ShowUpgradeMaterialCategories),
                    OpenItem = OpenUpgradeMaterial
                },
                Placeholder(nameof(Resources.CategoryKeyItems))
            };
        }

        private Category WeaponCategory => _categories.First(c => c.Kind == "weapon");
        private Category TalismanCategory => _categories.First(c => c.Kind == "talisman");
        private Category UpgradeMaterialCategory => _categories.First(c => c.Kind == "upgrade_material");

        private void Main_Load(object sender, EventArgs e)
        {
            ApplyTexts();

            Theme.Apply(this);
            lblTitle.Font = Theme.TitleFont;
            lblTitle.ForeColor = Theme.Accent;
            lblPage.ForeColor = Theme.TextMuted;
            pnlTiles.BackColor = Theme.Background;
            btnLanguage.Font = Theme.BodyFont;
            btnLanguage.BringToFront();   // it sits on top of the title, in the top right corner

            if (!WeaponDatabase.Exists)
            {
                MessageBox.Show(
                    string.Format(Resources.DatabaseNotFound, WeaponDatabase.DbPath),
                    Resources.ErrorTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                // Nothing can be loaded without the database, so don't let the user try
                pnlToolbar.Enabled = false;
                return;
            }

            ShowPage(ShowHome);
            ActiveControl = SearchBox;   // so the user can start typing a search right away
        }

        // ---------- Language ----------

        private const string English = "en";
        private const string Estonian = "et";

        // Texts come from Properties/Resources (English) or Resources.et (Estonian)
        private void ApplyTexts()
        {
            Text = Resources.AppWindowTitle;
            lblTitle.Text = Resources.AppTitle;
            btnHome.Text = Resources.HomeButton;
            btnBack.Text = Resources.BackButton;
            Search.Text = Resources.SearchButton;
            btnLanguage.Text = Resources.LanguageSwitch;   // the name of the *other* language, e.g. "Eesti"
        }

        private static bool IsEstonian =>
            (Resources.Culture ?? CultureInfo.CurrentUICulture).TwoLetterISOLanguageName == Estonian;

        private static void SetLanguage(string code)
        {
            var culture = new CultureInfo(code);
            Resources.Culture = culture;                        // texts from Resources.*
            Thread.CurrentThread.CurrentUICulture = culture;    // anything else .NET translates
        }

        // The language the user picked last time. If they never picked one, the app follows the
        // Windows language (Estonian Windows -> Estonian, anything else -> English).
        private static void UseSavedLanguage()
        {
            var saved = Settings.Default.Language;
            if (saved == English || saved == Estonian)
                SetLanguage(saved);
        }

        // Switches between English and Estonian, remembers the choice and redraws the current page
        private void btnLanguage_Click(object sender, EventArgs e)
        {
            var next = IsEstonian ? English : Estonian;
            SetLanguage(next);
            Settings.Default.Language = next;
            Settings.Default.Save();

            ApplyTexts();
            if (_currentPage != null)
                ShowPage(_currentPage);
        }

        // ---------- Navigation ----------

        // Goes to a new page and remembers the current one for Back
        private void NavigateTo(Action page)
        {
            if (_currentPage != null)
                _history.Push(_currentPage);
            ShowPage(page);
        }

        private void ShowPage(Action page)
        {
            try
            {
                page();
                _currentPage = page;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // Grey out Back when there is nowhere to go. Not with Enabled = false: Windows then draws
            // the text of a flat button in its own dark grey, which is almost invisible on the dark theme.
            var canGoBack = _history.Count > 0;
            btnBack.ForeColor = canGoBack ? Theme.Accent : Theme.Border;
            btnBack.FlatAppearance.BorderColor = canGoBack ? Theme.Accent : Theme.Border;
            btnBack.Cursor = canGoBack ? Cursors.Hand : Cursors.Default;
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            _history.Clear();
            ShowPage(ShowHome);
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (_history.Count > 0)
                ShowPage(_history.Pop());
        }

        // ---------- Pages ----------

        // Home page: one big button per category, with the number of items in it
        // ("Coming soon" for categories that have no data yet)
        private void ShowHome()
        {
            ShowTiles(Resources.HomeHeading, Theme.TileFont, new Size(184, 92),
                _categories.Select(c => c.IsPlaceholder
                    ? ($"{c.Title}\n{Resources.ComingSoon}", (Action)null)
                    : (TileText(c.Title, Count(c.CountSql)), c.Open)));
        }

        // Weapons page: "All Weapons" plus one button per weapon type that has weapons
        private void ShowWeaponTypes()
        {
            var types = WeaponDatabase.Query(
                @"SELECT wt.id, wt.name, COUNT(w.id) AS weapons
                  FROM weapon_types wt
                  JOIN weapons w ON w.weapon_type_id = wt.id
                  GROUP BY wt.id, wt.name
                  ORDER BY wt.name;");

            var tiles = new List<(string, Action)>
            {
                (TileText(Resources.AllWeapons, Count(WeaponCategory.CountSql)),
                 () => NavigateTo(() => ShowWeapons(null, Resources.AllWeapons)))
            };
            foreach (DataRow type in types.Rows)
            {
                var id = Convert.ToInt32(type["id"]);
                var name = type["name"].ToString();
                tiles.Add((TileText(name, Convert.ToInt32(type["weapons"])),
                           () => NavigateTo(() => ShowWeapons(id, name))));
            }

            ShowTiles(Resources.CategoryWeapons, Theme.BoldFont, new Size(180, 64), tiles);
        }

        // All weapons (typeId null) or the weapons of one type
        private void ShowWeapons(int? typeId, string heading)
        {
            var weapons = WeaponDatabase.Query(
                @"SELECT id AS Id, name AS Name FROM weapons
                  WHERE @typeid IS NULL OR weapon_type_id = @typeid
                  ORDER BY name;",
                new Dictionary<string, object> { { "@typeid", (object)typeId ?? DBNull.Value } });

            ShowItems($"{Resources.CategoryWeapons} › {heading}", new[] { (WeaponCategory, weapons) }, false);
        }

        private void ShowTalismans()
        {
            var talismans = WeaponDatabase.Query(
                "SELECT id AS Id, name AS Name FROM talismans ORDER BY name;");

            ShowItems(Resources.CategoryTalismans, new[] { (TalismanCategory, talismans) }, false,
                      Resources.NoTalismansImported);
        }

        // Upgrade materials page: "All Upgrade Materials" plus one button per kind (Smithing Stones, ...),
        // in the game's inventory order
        private void ShowUpgradeMaterialCategories()
        {
            var categories = WeaponDatabase.Query(
                @"SELECT category, COUNT(*) AS materials
                  FROM upgrade_materials
                  GROUP BY category
                  ORDER BY MIN(sort_id);");

            var tiles = new List<(string, Action)>
            {
                (TileText(Resources.AllUpgradeMaterials, Count(UpgradeMaterialCategory.CountSql)),
                 () => NavigateTo(() => ShowUpgradeMaterials(null, Resources.AllUpgradeMaterials)))
            };
            foreach (DataRow category in categories.Rows)
            {
                var key = category["category"].ToString();
                var name = Resources.ResourceManager.GetString("UpgradeCategory_" + key, Resources.Culture) ?? key;
                tiles.Add((TileText(name, Convert.ToInt32(category["materials"])),
                           () => NavigateTo(() => ShowUpgradeMaterials(key, name))));
            }

            ShowTiles(Resources.CategoryUpgradeMaterials, Theme.BoldFont, new Size(180, 64), tiles);
        }

        // All upgrade materials (category null) or those of one kind, in the game's inventory order
        // so e.g. Smithing Stone [1] to [8] stay in sequence
        private void ShowUpgradeMaterials(string category, string heading)
        {
            var materials = WeaponDatabase.Query(
                @"SELECT id AS Id, name AS Name FROM upgrade_materials
                  WHERE @category IS NULL OR category = @category
                  ORDER BY sort_id, name;",
                new Dictionary<string, object> { { "@category", (object)category ?? DBNull.Value } });

            ShowItems($"{Resources.CategoryUpgradeMaterials} › {heading}", new[] { (UpgradeMaterialCategory, materials) },
                      false, Resources.NoUpgradeMaterialsImported);
        }

        // Search results, grouped by category: "Weapons (10)" with its matches, then "Talismans (1)", ...
        private void ShowSearchResults(string searchText)
        {
            var parameters = new Dictionary<string, object> { { "@search", "%" + EscapeLike(searchText) + "%" } };
            var groups = _categories
                .Where(c => !c.IsPlaceholder)
                .Select(c => (c, WeaponDatabase.Query(c.SearchSql, parameters)))
                .Where(g => g.Item2.Rows.Count > 0)
                .ToList();

            var heading = groups.Count > 0
                ? string.Format(Resources.SearchResultsHeading, searchText)
                : string.Format(Resources.NoSearchResults, searchText);
            ShowItems(heading, groups, true);
        }

        // ---------- Drawing the pages ----------

        // Every page is a panel of cards: categories, sub-categories (weapon types, ...) and items alike.
        // Item cards are all drawn by AddCard, so a picture can later be added there in one place.
        private static readonly Size ItemCardSize = new Size(180, 72);

        // A page of category / sub-category tiles. A tile with no Click action is drawn greyed out and does nothing.
        private void ShowTiles(string heading, Font font, Size size, IEnumerable<(string Text, Action Click)> tiles)
        {
            BeginCards(heading);
            foreach (var tile in tiles)
                AddCard(tile.Text, font, size, tile.Click);
            EndCards();
        }

        // A page of item cards; clicking one opens its details window.
        // With withHeadings, each group starts with a full-width heading such as "Weapons (10)".
        // If there are no items at all, emptyNote (when given) is shown instead.
        private void ShowItems(string heading, IEnumerable<(Category Category, DataTable Items)> groups,
                               bool withHeadings, string emptyNote = null)
        {
            BeginCards(heading);
            var any = false;
            foreach (var group in groups)
            {
                if (withHeadings)
                    AddCardHeading($"{group.Category.Title} ({group.Items.Rows.Count})", Theme.Accent, Theme.BoldFont);

                var open = group.Category.OpenItem;
                foreach (DataRow item in group.Items.Rows)
                {
                    var id = Convert.ToInt32(item["Id"]);
                    AddCard(item["Name"].ToString(), Theme.BodyFont, ItemCardSize, () => open(id));
                    any = true;
                }
            }

            if (!any && emptyNote != null)
                AddCardHeading(emptyNote, Theme.TextMuted, Theme.BodyFont);
            EndCards();
        }

        private void BeginCards(string heading)
        {
            lblPage.Text = heading;
            pnlTiles.SuspendLayout();
            // Controls.Clear() doesn't free the old controls, so dispose them
            foreach (var old in pnlTiles.Controls.Cast<Control>().ToList())
                old.Dispose();
        }

        private void EndCards()
        {
            pnlTiles.ResumeLayout();
            pnlTiles.AutoScrollPosition = Point.Empty;
        }

        private void AddCard(string text, Font font, Size size, Action click)
        {
            // UseMnemonic off, so "&" (e.g. "Arrows & Bolts") is shown instead of being read as a shortcut key
            var button = new Button { Text = text, Size = size, Margin = new Padding(8), UseMnemonic = false };
            Theme.StyleButton(button);
            button.Font = FontThatFits(text, font, size.Width - 16);   // 16 = button's inner padding

            if (click == null)
            {
                // Not Enabled = false: Windows would draw the text in a dark grey that can't be read
                // on the dark theme. Instead: muted colours, no hover effect, not reachable with Tab.
                button.ForeColor = Theme.TextMuted;
                button.BackColor = Theme.Surface;
                button.FlatAppearance.BorderColor = Theme.Border;
                button.FlatAppearance.MouseOverBackColor = Theme.Surface;
                button.FlatAppearance.MouseDownBackColor = Theme.Surface;
                button.Cursor = Cursors.Default;
                button.TabStop = false;
            }
            else
            {
                // Run the click after the button's own event has finished: the next page disposes this button
                button.Click += (s, e) => BeginInvoke(click);
            }

            pnlTiles.Controls.Add(button);
        }

        // Text on its own line between cards, e.g. a search result group heading
        private void AddCardHeading(string text, Color color, Font font)
        {
            // Start a new line before and after it
            if (pnlTiles.Controls.Count > 0)
                pnlTiles.SetFlowBreak(pnlTiles.Controls[pnlTiles.Controls.Count - 1], true);

            var label = new Label
            {
                Text = text, AutoSize = true, ForeColor = color, Font = font, UseMnemonic = false,
                Margin = new Padding(8, 12, 8, 0)
            };
            pnlTiles.Controls.Add(label);
            pnlTiles.SetFlowBreak(label, true);
        }

        // Smaller versions of the tile fonts, made once and reused (a Font holds a Windows resource)
        private static readonly Dictionary<(string, float, FontStyle), Font> SmallerFonts =
            new Dictionary<(string, float, FontStyle), Font>();

        // Buttons wrap text only between words, so a single long word (e.g. Estonian
        // "Meisterdamismaterjalid") would be cut in the middle. Shrink the font until every word fits.
        private static Font FontThatFits(string text, Font font, int width)
        {
            var words = text.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var result = font;
            while (result.Size > 8 && words.Any(w => TextRenderer.MeasureText(w, result).Width > width))
            {
                var key = (font.FontFamily.Name, result.Size - 1, font.Style);
                if (!SmallerFonts.TryGetValue(key, out var smaller))
                    SmallerFonts[key] = smaller = new Font(font.FontFamily, key.Item2, font.Style);
                result = smaller;
            }
            return result;
        }

        private static string TileText(string title, int count) => $"{title}\n{count}";

        private static int Count(string sql) => Convert.ToInt32(WeaponDatabase.Query(sql).Rows[0][0]);

        // ---------- Search and clicks ----------

        private void Search_Click(object sender, EventArgs e)
        {
            var searchValue = SearchBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(searchValue))
            {
                MessageBox.Show(Resources.EnterSearchText);
                return;
            }

            NavigateTo(() => ShowSearchResults(searchValue));
        }

        // In LIKE, % and _ are wildcards. Put a \ in front of them (and of \ itself) so they are
        // searched for as normal characters, e.g. searching "_" finds names containing "_".
        private static string EscapeLike(string text)
        {
            return text.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
        }

        private void OpenWeapon(int id)
        {
            using (var detailsForm = new DetailedView(id))
            {
                detailsForm.ShowDialog(this);
            }
        }

        private void OpenTalisman(int id)
        {
            using (var talismanForm = new TalismanView(id))
            {
                talismanForm.ShowDialog(this);
            }
        }

        private void OpenUpgradeMaterial(int id)
        {
            using (var materialForm = new UpgradeMaterialView(id))
            {
                materialForm.ShowDialog(this);
            }
        }
    }
}
