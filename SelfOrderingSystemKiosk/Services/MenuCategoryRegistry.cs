using Microsoft.Extensions.Options;
using SelfOrderingSystemKiosk.Models;

namespace SelfOrderingSystemKiosk.Services
{
    /// <summary>Single source of truth for menu categories (from appsettings).</summary>
    public class MenuCategoryRegistry
    {
        private readonly IReadOnlyList<MenuCategoryOption> _all;

        public MenuCategoryRegistry(IOptions<MenuCategoriesSettings> options)
        {
            var raw = options.Value?.Categories?
                .Where(c => !string.IsNullOrWhiteSpace(c.Key))
                .ToList() ?? new List<MenuCategoryOption>();

            if (raw.Count == 0)
                raw = GetDefaultCategories();

            _all = raw.OrderBy(c => c.SortOrder).ThenBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public IReadOnlyList<MenuCategoryOption> All => _all;

        public IReadOnlyList<MenuCategoryOption> KioskTabs =>
            _all.Where(c => c.ShowInKiosk).OrderBy(c => c.SortOrder).ThenBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();

        public bool IsValidKey(string? key) =>
            !string.IsNullOrWhiteSpace(key) &&
            _all.Any(c => c.Key.Equals(key.Trim(), StringComparison.Ordinal));

        public string GetDefaultImage(string? categoryKey)
        {
            if (string.IsNullOrWhiteSpace(categoryKey))
                return "/images/wings.png";
            var c = _all.FirstOrDefault(x => x.Key.Equals(categoryKey.Trim(), StringComparison.Ordinal));
            return string.IsNullOrEmpty(c?.DefaultImage) ? "/images/wings.png" : c!.DefaultImage;
        }

        private static List<MenuCategoryOption> GetDefaultCategories() =>
            new()
            {
                new MenuCategoryOption
                {
                    Key = "Wings",
                    DisplayName = "Wings",
                    DefaultImage = "/images/wings.png",
                    ShowInKiosk = true,
                    SortOrder = 1,
                    TabImageUrl = "/images/wings.png"
                },
                new MenuCategoryOption
                {
                    Key = "Appetizer",
                    DisplayName = "Appetizer",
                    DefaultImage = "/images/appetize.png",
                    ShowInKiosk = true,
                    SortOrder = 2,
                    TabImageUrl = "/images/appetize.png"
                },
                new MenuCategoryOption
                {
                    Key = "Add Ons",
                    DisplayName = "Add-Ons",
                    DefaultImage = "/images/wings.png",
                    ShowInKiosk = true,
                    SortOrder = 3,
                    TabIconClass = "basket"
                },
                new MenuCategoryOption
                {
                    Key = "Unavailable",
                    DisplayName = "Unavailable (hidden from kiosk)",
                    DefaultImage = "/images/wings.png",
                    ShowInKiosk = false,
                    SortOrder = 99
                }
            };
    }
}
