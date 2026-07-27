using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.IO.Compression;
using DbdModManager.Controls;

namespace DbdModManager
{
    public class Preset
    {
        public string Name { get; set; } = "";
        public List<string> SelectedModNames { get; set; } = new();
    }

    public partial class MainWindow : Window
    {
        private ObservableCollection<ModItem> _mods = new();
        private List<ModItem> _allMods = new();
        private ObservableCollection<string> _presets = new();
        private string _configPath = "config.json";
        private string _presetsPath = "presets.json";

        public MainWindow()
        {
            InitializeComponent();
            ModsItemsControl.ItemsSource = _mods;
            PresetCombo.ItemsSource = _presets;
            LoadConfig();
            LoadPresets();
            RefreshModList();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) ToggleMaximize();
            else if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void TitleText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            
            try {
                ZoePopup.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/zoe.png"));
            } catch { }

            var sb = new System.Windows.Media.Animation.Storyboard();
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromSeconds(0.2) };
            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation { From = 1, To = 0, Duration = TimeSpan.FromSeconds(0.8), BeginTime = TimeSpan.FromSeconds(1.5) };
            
            System.Windows.Media.Animation.Storyboard.SetTarget(fadeIn, ZoePopup);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
            System.Windows.Media.Animation.Storyboard.SetTarget(fadeOut, ZoePopup);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));
            
            sb.Children.Add(fadeIn);
            sb.Children.Add(fadeOut);
            sb.Begin();
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e) => System.Windows.Application.Current.Shutdown();
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void MaximizeButton_Click(object sender, RoutedEventArgs e) => ToggleMaximize();

        private void ToggleMaximize()
        {
            if (WindowState == WindowState.Maximized) WindowState = WindowState.Normal;
            else WindowState = WindowState.Maximized;
        }

        private void LoadConfig()
        {
            if (File.Exists(_configPath))
            {
                try {
                    var json = File.ReadAllText(_configPath);
                    var cfg = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (cfg != null) {
                        if (cfg.ContainsKey("steam_path")) SteamPathTxt.Text = cfg["steam_path"];
                        if (cfg.ContainsKey("epic_path")) EpicPathTxt.Text = cfg["epic_path"];
                        if (cfg.ContainsKey("xbox_path")) XboxPathTxt.Text = cfg["xbox_path"];
                        if (cfg.ContainsKey("mod_repo")) RepoTxt.Text = cfg["mod_repo"];
                        
                        if (cfg.ContainsKey("active_platform")) {
                            string plat = cfg["active_platform"];
                            if (plat == "Steam") SteamRb.IsChecked = true;
                            else if (plat == "Epic") EpicRb.IsChecked = true;
                            else if (plat == "Xbox") XboxRb.IsChecked = true;
                        }
                        if (cfg.ContainsKey("last_theme")) {
                            string loadedTheme = cfg["last_theme"];
                            if (loadedTheme == "Purple") loadedTheme = "Eclipsed Mode";
                            if (loadedTheme == "Blue") loadedTheme = "Isolumia Mode";
                            if (loadedTheme == "Girl") loadedTheme = "Girl Mode";
                            
                            foreach (ComboBoxItem item in ThemeCombo.Items) {
                                if (item.Content.ToString() == loadedTheme) {
                                    ThemeCombo.SelectedItem = item;
                                    break;
                                }
                            }
                        } else {
                            ThemeCombo.SelectedIndex = 0;
                        }
                    }
                } catch { }
            }
        }

        private void SaveConfig()
        {
            string plat = SteamRb.IsChecked == true ? "Steam" : (EpicRb.IsChecked == true ? "Epic" : "Xbox");
            var cfg = new Dictionary<string, string> {
                { "steam_path", SteamPathTxt.Text },
                { "epic_path", EpicPathTxt.Text },
                { "xbox_path", XboxPathTxt.Text },
                { "mod_repo", RepoTxt.Text },
                { "active_platform", plat },
                { "last_theme", _currentTheme }
            };
            File.WriteAllText(_configPath, JsonSerializer.Serialize(cfg));
        }

        private void LoadPresets()
        {
            _presets.Clear();
            if (File.Exists(_presetsPath))
            {
                try {
                    var json = File.ReadAllText(_presetsPath);
                    var data = JsonSerializer.Deserialize<List<Preset>>(json);
                    if (data != null) {
                        foreach (var p in data) _presets.Add(p.Name);
                    }
                } catch { }
            }
        }

        private void PresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PresetCombo.SelectedItem is string name) {
                ApplyPreset(name);
            }
        }

        private void ApplyPreset(string name)
        {
            if (!File.Exists(_presetsPath)) return;
            try {
                var json = File.ReadAllText(_presetsPath);
                var data = JsonSerializer.Deserialize<List<Preset>>(json);
                var preset = data?.FirstOrDefault(p => p.Name == name);
                if (preset != null) {
                    foreach (var mod in _allMods) {
                        mod.IsSelected = preset.SelectedModNames.Contains(mod.Name);
                    }
                }
            } catch { }
        }

        private void AddPreset_Click(object sender, RoutedEventArgs e)
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("Enter preset name:", "New Preset", "My Profile");
            if (string.IsNullOrWhiteSpace(name)) return;

            var preset = new Preset {
                Name = name,
                SelectedModNames = _allMods.Where(m => m.IsSelected).Select(m => m.Name).ToList()
            };

            List<Preset> all = new();
            if (File.Exists(_presetsPath)) {
                try { all = JsonSerializer.Deserialize<List<Preset>>(File.ReadAllText(_presetsPath)) ?? new(); } catch { }
            }
            all.RemoveAll(p => p.Name == name);
            all.Add(preset);
            File.WriteAllText(_presetsPath, JsonSerializer.Serialize(all));
            LoadPresets();
            PresetCombo.SelectedItem = name;
        }

        private void DeletePreset_Click(object sender, RoutedEventArgs e)
        {
            if (PresetCombo.SelectedItem is string name) {
                List<Preset> all = new();
                if (File.Exists(_presetsPath)) {
                    try { all = JsonSerializer.Deserialize<List<Preset>>(File.ReadAllText(_presetsPath)) ?? new(); } catch { }
                }
                all.RemoveAll(p => p.Name == name);
                File.WriteAllText(_presetsPath, JsonSerializer.Serialize(all));
                LoadPresets();
            }
        }

        private void SavePreset_Click(object sender, RoutedEventArgs e)
        {
            if (PresetCombo.SelectedItem is string name) {
                List<Preset> all = new();
                if (File.Exists(_presetsPath)) {
                    try { all = JsonSerializer.Deserialize<List<Preset>>(File.ReadAllText(_presetsPath)) ?? new(); } catch { }
                }
                
                var preset = all.FirstOrDefault(p => p.Name == name);
                if (preset != null) {
                    preset.SelectedModNames = _allMods.Where(m => m.IsSelected).Select(m => m.Name).ToList();
                    File.WriteAllText(_presetsPath, JsonSerializer.Serialize(all));
                    Toasts.Show($"Preset '{name}' saved", ToastKind.Success);
                }
            }
        }

        private string _currentTheme = "Eclipsed Mode";
        private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeCombo.SelectedItem is ComboBoxItem item && item.Content is string theme && _currentTheme != theme) {
                SetTheme(theme);
                SaveConfig();
            }
        }

        private void SetTheme(string theme)
        {
            _currentTheme = theme;
            System.Windows.Media.Color accent = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#8A2BE2");
            System.Windows.Media.Color glow = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6a0dad");
            System.Windows.Media.Brush accentBrush = new System.Windows.Media.SolidColorBrush(accent);

            System.Windows.Media.Brush bgBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0a0a0a"));
            System.Windows.Media.Brush secBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#121212"));
            System.Windows.Media.Brush cardBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#151515"));
            System.Windows.Media.Brush hoverCardBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1d1d1d"));
            System.Windows.Media.Brush selectedCardBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#151515"));
            System.Windows.Media.Brush activeHoverCardBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1d1d1d"));
            System.Windows.Media.Brush textBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ffffff"));
            System.Windows.Media.Brush mutedTextBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#888888"));

            if (theme == "Isolumia Mode") {
                accent = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00aacc");
                glow = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#006688");
                accentBrush = new System.Windows.Media.SolidColorBrush(accent);
                cardBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#101010"));
                hoverCardBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#181818"));
                selectedCardBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#101010"));
                activeHoverCardBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#181818"));
            } else if (theme == "Girl Mode") {
                accent = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ff6699"); 
                glow = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ff3377");   
                accentBrush = new System.Windows.Media.SolidColorBrush(accent);
                
                var lightGrad = new System.Windows.Media.LinearGradientBrush();
                lightGrad.StartPoint = new System.Windows.Point(0, 0.5);
                lightGrad.EndPoint = new System.Windows.Point(1, 0.5);
                lightGrad.GradientStops.Add(new System.Windows.Media.GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#fca7c2"), 0.0)); 
                lightGrad.GradientStops.Add(new System.Windows.Media.GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ffeaf0"), 0.5)); 
                lightGrad.GradientStops.Add(new System.Windows.Media.GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#fca7c2"), 1.0));
                
                var panAnim = new System.Windows.Media.Animation.PointAnimation {
                    From = new System.Windows.Point(0, 0.5),
                    To = new System.Windows.Point(1, 0.5),
                    Duration = new System.Windows.Duration(TimeSpan.FromSeconds(60)),
                    AutoReverse = true,
                    RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
                };
                
                var panAnimEnd = new System.Windows.Media.Animation.PointAnimation {
                    From = new System.Windows.Point(1, 0.5),
                    To = new System.Windows.Point(2, 0.5), 
                    Duration = new System.Windows.Duration(TimeSpan.FromSeconds(60)),
                    AutoReverse = true, 
                    RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
                };

                lightGrad.BeginAnimation(System.Windows.Media.LinearGradientBrush.StartPointProperty, panAnim);
                lightGrad.BeginAnimation(System.Windows.Media.LinearGradientBrush.EndPointProperty, panAnimEnd);

                bgBrush = lightGrad; 
                secBrush = lightGrad;
                cardBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#151515"));
                hoverCardBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1d1d1d"));
                selectedCardBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ffcce0"));
                activeHoverCardBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ffb3d1"));
                textBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ff1493"));
                mutedTextBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#cc5288"));
                
                accentBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ff1493"));
            }

            System.Windows.Application.Current.Resources["AccentColor"] = accent;
            System.Windows.Application.Current.Resources["GlowColor"] = glow;
            System.Windows.Application.Current.Resources["AccentBrush"] = accentBrush;
            
            System.Windows.Application.Current.Resources["BackgroundBrush"] = bgBrush;
            System.Windows.Application.Current.Resources["SecondaryBrush"] = secBrush;
            System.Windows.Application.Current.Resources["CardBrush"] = cardBrush;
            System.Windows.Application.Current.Resources["HoverCardBrush"] = hoverCardBrush;
            System.Windows.Application.Current.Resources["SelectedCardBrush"] = selectedCardBrush;
            System.Windows.Application.Current.Resources["ActiveHoverCardBrush"] = activeHoverCardBrush;
            System.Windows.Application.Current.Resources["TextBrush"] = textBrush;
            System.Windows.Application.Current.Resources["MutedTextBrush"] = mutedTextBrush;
            
            System.Windows.Application.Current.Resources["PurpleGlowEffect"] = new System.Windows.Media.Effects.DropShadowEffect { Color = glow, BlurRadius = 15, ShadowDepth = 0, Opacity = 0.4 };
            System.Windows.Application.Current.Resources["StrongGlowEffect"] = new System.Windows.Media.Effects.DropShadowEffect { Color = glow, BlurRadius = 25, ShadowDepth = 0, Opacity = 0.6 };

            if (_mods != null) {
                foreach (var m in _mods) m.RefreshThemeBrushes();
            }
        }

        private string BrowseFolder()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog();
            return dialog.ShowDialog() == true ? dialog.FolderName : "";
        }

        private void BrowseSteam_Click(object sender, RoutedEventArgs e) { string p = BrowseFolder(); if (p!="") { SteamPathTxt.Text = p; SaveConfig(); } }
        private void BrowseEpic_Click(object sender, RoutedEventArgs e) { string p = BrowseFolder(); if (p!="") { EpicPathTxt.Text = p; SaveConfig(); } }
        private void BrowseXbox_Click(object sender, RoutedEventArgs e) { string p = BrowseFolder(); if (p!="") { XboxPathTxt.Text = p; SaveConfig(); } }
        private void BrowseRepo_Click(object sender, RoutedEventArgs e) { string p = BrowseFolder(); if (p!="") { RepoTxt.Text = p; SaveConfig(); RefreshModList(); } }

        private void RefreshModList_Click(object sender, RoutedEventArgs e)
        {
            RefreshModList();
        }

        private void RefreshModList()
        {
            string repo = RepoTxt.Text;
            
            var previouslySelected = _allMods.Where(m => m.IsSelected).Select(m => m.Path).ToHashSet();
            
            if (string.IsNullOrEmpty(repo) || !Directory.Exists(repo)) {
                _allMods.Clear();
                FilterMods();
                return;
            }

            _allMods.Clear();
            var enumOptions = new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true };
            foreach (var dir in Directory.GetDirectories(repo))
            {
                string name = System.IO.Path.GetFileName(dir);
                if (Directory.EnumerateFiles(dir, "*.pak", enumOptions).Any())
                    _allMods.Add(new ModItem { Name = name, Path = dir });
            }
            foreach (var zip in Directory.GetFiles(repo, "*.zip"))
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(zip);
                _allMods.Add(new ModItem { Name = name, Path = zip });
            }
            foreach (var mmpack in Directory.GetFiles(repo, "*.mmpackage"))
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(mmpack);
                _allMods.Add(new ModItem { Name = name, Path = mmpack });
            }
            foreach (var rar in Directory.GetFiles(repo, "*.rar"))
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(rar);
                _allMods.Add(new ModItem { Name = name, Path = rar });
            }
            
            foreach (var mod in _allMods) {
                if (previouslySelected.Contains(mod.Path)) {
                    mod.IsSelected = true;
                }
            }
            
            FilterMods();
        }

        private void FilterMods()
        {
            string search = SearchTxt.Text.ToLower();
            _mods.Clear();
            foreach (var mod in _allMods.Where(m => m.Name.ToLower().Contains(search)))
                _mods.Add(mod);
        }

        private void SearchTxt_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => FilterMods();

        private void Card_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ModItem mod)
            {
                mod.IsSelected = !mod.IsSelected;
            }
        }

        private void Card_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ModItem mod)
            {
                var result = System.Windows.MessageBox.Show($"Are you sure you want to permanently delete the mod '{mod.Name}' from your PC?\n\nPath: {mod.Path}", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try {
                        if (File.Exists(mod.Path)) {
                            File.Delete(mod.Path);
                        } else if (Directory.Exists(mod.Path)) {
                            Directory.Delete(mod.Path, true);
                        }
                        RefreshModList();
                    } catch (Exception ex) {
                        Toasts.Show($"Failed to delete mod: {ex.Message}", ToastKind.Error);
                    }
                }
            }
        }

        private void EnableAllMods_Click(object sender, RoutedEventArgs e)
        {
            if (_mods == null) return;
            foreach (var mod in _mods) mod.IsSelected = true;
        }

        private void DisableAllMods_Click(object sender, RoutedEventArgs e)
        {
            if (_mods == null) return;
            foreach (var mod in _mods) mod.IsSelected = false;
        }

        private void Clean_Click(object sender, RoutedEventArgs e)
        {
            try {
                int count = RunCleaner();
                Toasts.Show($"Cleanup complete! Removed {count} mod-related files.", ToastKind.Success);
            } catch (Exception ex) {
                Toasts.Show($"Cleanup failed: {ex.Message}", ToastKind.Error);
            }
        }

        private int RunCleaner()
        {
            string activePath = "";
            if (SteamRb.IsChecked == true) activePath = SteamPathTxt.Text;
            else if (EpicRb.IsChecked == true) activePath = EpicPathTxt.Text;
            else if (XboxRb.IsChecked == true) activePath = XboxPathTxt.Text;

            if (string.IsNullOrEmpty(activePath) || !Directory.Exists(activePath)) return 0;

            int count = 0;
            var files = Directory.EnumerateFiles(activePath, "*.*", SearchOption.TopDirectoryOnly)
                                 .Where(f => {
                                     string ext = System.IO.Path.GetExtension(f).ToLower();
                                     return ext == ".pak" || ext == ".sig" || ext == ".utoc" || ext == ".ucas";
                                 });

            foreach (var file in files) {
                string name = System.IO.Path.GetFileNameWithoutExtension(file);
                
                var match = Regex.Match(name, @"pakchunk(\d+)", RegexOptions.IgnoreCase);
                if (match.Success) {
                    if (int.TryParse(match.Groups[1].Value, out int chunkId)) {
                        if (chunkId > 6000) {
                            try { File.Delete(file); count++; } catch { }
                        }
                    }
                }
            }
            return count;
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            string activePath = "";
            string suffix = "";
            
            if (SteamRb.IsChecked == true) { activePath = SteamPathTxt.Text; suffix = "-Windows"; }
            else if (EpicRb.IsChecked == true) { activePath = EpicPathTxt.Text; suffix = "-EGS"; }
            else if (XboxRb.IsChecked == true) { activePath = XboxPathTxt.Text; suffix = "-WinGDK"; }

            if (string.IsNullOrEmpty(activePath) || !Directory.Exists(activePath)) {
                Toasts.Show("Selected path is invalid!", ToastKind.Warning);
                return;
            }

            try {
                if (CleanChk.IsChecked == true) RunCleaner();

                string sigTemplate = System.IO.Path.Combine(activePath, $"pakchunk0{suffix}.sig");
                if (!File.Exists(sigTemplate)) {
                    sigTemplate = Directory.GetFiles(activePath, $"*{suffix}.sig").FirstOrDefault();
                }

                int appliedCount = 0;
                string[] extensions = { ".pak", ".sig", ".ucas", ".utoc" };
                
                var enumOptions = new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true };

                foreach (var mod in _allMods.Where(m => m.IsSelected)) {
                    if (Directory.Exists(mod.Path)) {
                        foreach (var pakPath in Directory.EnumerateFiles(mod.Path, "*.pak", enumOptions)) {
                            string modFolder = System.IO.Path.GetDirectoryName(pakPath);
                            string originalBaseName = System.IO.Path.GetFileNameWithoutExtension(pakPath);
                            
                            var match = Regex.Match(originalBaseName, @"(pakchunk\d+)", RegexOptions.IgnoreCase);
                            string targetChunkBase = match.Success ? match.Groups[1].Value : originalBaseName;
                            string targetFinalBase = $"{targetChunkBase}{suffix}";

                            foreach (string ext in extensions) {
                                string sourceFile = System.IO.Path.Combine(modFolder, originalBaseName + ext);
                                string targetFile = System.IO.Path.Combine(activePath, targetFinalBase + ext);

                                if (File.Exists(sourceFile)) {
                                    File.Copy(sourceFile, targetFile, true);
                                    if (ext == ".pak") appliedCount++;
                                } 
                                else if (ext == ".sig" && !File.Exists(targetFile)) {
                                    if (File.Exists(sigTemplate)) File.Copy(sigTemplate, targetFile, true);
                                    else File.WriteAllBytes(targetFile, Array.Empty<byte>());
                                }
                            }
                        }
                    }
                    else if (File.Exists(mod.Path) && (mod.Path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || mod.Path.EndsWith(".mmpackage", StringComparison.OrdinalIgnoreCase))) {
                        using var archive = System.IO.Compression.ZipFile.OpenRead(mod.Path);
                        var pakEntries = archive.Entries.Where(e => e.FullName.EndsWith(".pak", StringComparison.OrdinalIgnoreCase) && !e.FullName.EndsWith("/"));
                        foreach (var pakEntry in pakEntries) {
                            string originalBaseName = System.IO.Path.GetFileNameWithoutExtension(pakEntry.FullName);
                            var match = Regex.Match(originalBaseName, @"(pakchunk\d+)", RegexOptions.IgnoreCase);
                            string targetChunkBase = match.Success ? match.Groups[1].Value : originalBaseName;
                            string targetFinalBase = $"{targetChunkBase}{suffix}";

                            foreach (string ext in extensions) {
                                var entry = archive.Entries.FirstOrDefault(e => 
                                    System.IO.Path.GetFileName(e.FullName).Equals(originalBaseName + ext, StringComparison.OrdinalIgnoreCase)
                                );
                                
                                string targetFile = System.IO.Path.Combine(activePath, targetFinalBase + ext);

                                if (entry != null) {
                                    entry.ExtractToFile(targetFile, true);
                                    if (ext == ".pak") appliedCount++;
                                } 
                                else if (ext == ".sig" && !File.Exists(targetFile)) {
                                    if (File.Exists(sigTemplate)) File.Copy(sigTemplate, targetFile, true);
                                    else File.WriteAllBytes(targetFile, Array.Empty<byte>());
                                }
                            }
                        }
                    }
                    else if (File.Exists(mod.Path) && mod.Path.EndsWith(".rar", StringComparison.OrdinalIgnoreCase)) {
                        using var archive = SharpCompress.Archives.ArchiveFactory.OpenArchive(mod.Path);
                        var pakEntries = archive.Entries.Where(e => !e.IsDirectory && e.Key.EndsWith(".pak", StringComparison.OrdinalIgnoreCase));
                        foreach (var pakEntry in pakEntries) {
                            string originalBaseName = System.IO.Path.GetFileNameWithoutExtension(pakEntry.Key);
                            var match = Regex.Match(originalBaseName, @"(pakchunk\d+)", RegexOptions.IgnoreCase);
                            string targetChunkBase = match.Success ? match.Groups[1].Value : originalBaseName;
                            string targetFinalBase = $"{targetChunkBase}{suffix}";

                            foreach (string ext in extensions) {
                                var entry = archive.Entries.FirstOrDefault(e => 
                                    !e.IsDirectory && System.IO.Path.GetFileName(e.Key).Equals(originalBaseName + ext, StringComparison.OrdinalIgnoreCase)
                                );
                                
                                string targetFile = System.IO.Path.Combine(activePath, targetFinalBase + ext);

                                if (entry != null) {
                                    using (var stream = entry.OpenEntryStream())
                                    using (var outputStream = File.OpenWrite(targetFile))
                                    {
                                        stream.CopyTo(outputStream);
                                    }
                                    if (ext == ".pak") appliedCount++;
                                } 
                                else if (ext == ".sig" && !File.Exists(targetFile)) {
                                    if (File.Exists(sigTemplate)) File.Copy(sigTemplate, targetFile, true);
                                    else File.WriteAllBytes(targetFile, Array.Empty<byte>());
                                }
                            }
                        }
                    }
                }
                Toasts.Show($"Applied {appliedCount} mod chunks successfully!", ToastKind.Success);
            } catch (Exception ex) {
                Toasts.Show($"Application failed: {ex.Message}", ToastKind.Error);
            }
        }
    }
}