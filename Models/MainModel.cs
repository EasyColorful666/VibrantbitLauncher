using MinecraftLaunch.Base.Models.Authentication;
using MinecraftLaunch.Base.Models.Game;
using Wpf.Ui;

namespace VibrantbitLauncher.Models
{
    public class MainModel : ObservableObject
    {
        public string? MinecraftFolder { get; set; } = @"./.minecraft";
        public string? JavaPath { get; set; }
        public JavaEntry? Java { get; set; }
        public string? McVersion { get; set; }
        public Account? Account { get; set; }
        public bool IsMicrosoftAccount { get; set; }
        public List<MinecraftEntry> minecrafts { get; set; } = new List<MinecraftEntry>();
        public List<string> minecraftVersions { get; set; } = new List<string>();
    }
}
