using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MinecraftLaunch.Base.Models.Game;
using MinecraftLaunch.Utilities;
using VibrantbitLauncher.Services;

namespace VibrantbitLauncher.Helpers
{
    /// <summary>
    /// Java 查找。
    ///
    /// 不使用 MinecraftLaunch 的 <c>JavaUtil.EnumerableJavaAsync()</c>：它在遇到空路径候选时会
    /// 直接抛 ArgumentException，导致整个枚举中断、结果为空 —— 这正是设置页和开机向导
    /// 「检测不到 Java」的原因（机器上其实装了多个 JDK）。
    /// 这里改为自己收集候选路径并逐个解析，单项失败不影响其它项。
    /// </summary>
    public static class JavaHelper
    {
        public static async Task<List<JavaEntry>> FindJavasAsync()
        {
            var result = new List<JavaEntry>();
            var tried = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var accepted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var acceptedPath = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var candidate in EnumerateCandidates())
            {
                if (string.IsNullOrWhiteSpace(candidate) || !tried.Add(candidate))
                    continue;

                try
                {
                    var info = await JavaUtil.GetJavaInfoAsync(candidate);
                    if (info == null || string.IsNullOrWhiteSpace(info.JavaPath))
                        continue;

                    // 同一份 Java 常常同时以 javaw.exe / java.exe、以及 Oracle 的 javapath shim 出现，
                    // 按「主版本 + 位数」去重，避免下拉框里出现一串重复项
                    var key = $"{ParseMajor(info.JavaVersion)}|{info.Is64bit}";
                    if (!accepted.Add(key))
                        continue;

                    // 另外按真实路径去重（符号链接如 ...\Java\latest 会指向同一个 java.exe）
                    if (!acceptedPath.Add(ResolveRealPath(info.JavaPath)))
                        continue;

                    result.Add(info);
                }
                catch
                {
                    // 单个候选不可用（损坏、版本过低、非 Java 可执行文件）时跳过
                }
            }

            // 主版本号高的排前面
            return result.OrderByDescending(j => ParseMajor(j.JavaVersion)).ToList();
        }

        /// <summary>只找第一个可用 Java 的路径（安装器等场景用）。</summary>
        public static async Task<string?> FindFirstJavaPathAsync()
        {
            var javas = await FindJavasAsync();
            return javas.FirstOrDefault()?.JavaPath;
        }

        private static string ResolveRealPath(string path)
        {
            try
            {
                return new FileInfo(path).ResolveLinkTarget(true)?.FullName ?? Path.GetFullPath(path);
            }
            catch
            {
                return path;
            }
        }

        private static int ParseMajor(string? version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return 0;

            var match = Regex.Match(version, @"\d+");
            if (!match.Success)
                return 0;

            var major = int.Parse(match.Value);
            return major == 1 ? 8 : major;   // "1.8.0_441" → 8
        }

        private static IEnumerable<string> EnumerateCandidates()
        {
            foreach (var exe in FromJavaHome()) yield return exe;
            foreach (var exe in FromPathVariable()) yield return exe;
            foreach (var exe in FromCommonFolders()) yield return exe;
            foreach (var exe in FromMinecraftRuntime()) yield return exe;
        }

        private static IEnumerable<string> FromJavaHome()
        {
            var home = Environment.GetEnvironmentVariable("JAVA_HOME")?.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(home))
                yield break;

            yield return Path.Combine(home, "bin", "javaw.exe");
            yield return Path.Combine(home, "bin", "java.exe");
        }

        private static IEnumerable<string> FromPathVariable()
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(path))
                yield break;

            foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = dir.Trim().Trim('"');
                if (trimmed.Length == 0)
                    continue;

                yield return Path.Combine(trimmed, "javaw.exe");
                yield return Path.Combine(trimmed, "java.exe");
            }
        }

        /// <summary>各家 JDK 的默认安装根目录：扫根目录本身 + 一层子目录。</summary>
        private static IEnumerable<string> FromCommonFolders()
        {
            var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");

            var roots = new List<string>
            {
                @"C:\Program Files\Java",
                @"C:\Program Files (x86)\Java",
                @"C:\Program Files\Eclipse Adoptium",
                @"C:\Program Files\Eclipse Foundation",
                @"C:\Program Files\Microsoft",
                @"C:\Program Files\Zulu",
                @"C:\Program Files\Amazon Corretto",
                @"C:\Program Files\BellSoft",
                @"C:\Program Files\Semeru",
                @"C:\Program Files\SapMachine",
                @"C:\Program Files\JetBrains",
            };

            if (!string.IsNullOrWhiteSpace(localAppData))
                roots.Add(Path.Combine(localAppData, "Programs", "Eclipse Adoptium"));

            foreach (var root in roots)
            {
                if (!Directory.Exists(root))
                    continue;

                yield return Path.Combine(root, "bin", "javaw.exe");
                yield return Path.Combine(root, "bin", "java.exe");

                foreach (var sub in SafeDirectories(root))
                {
                    yield return Path.Combine(sub, "bin", "javaw.exe");
                    yield return Path.Combine(sub, "bin", "java.exe");
                }
            }
        }

        /// <summary>.minecraft 下由启动器下载的 Java 运行时。</summary>
        private static IEnumerable<string> FromMinecraftRuntime()
        {
            string full;
            try
            {
                full = Path.GetFullPath(SettingsService.ResolveMinecraftFolder());
            }
            catch
            {
                yield break;
            }

            var runtimeDir = Path.Combine(full, "runtime");
            if (!Directory.Exists(runtimeDir))
                yield break;

            foreach (var runtime in SafeDirectories(runtimeDir))
                foreach (var exe in SafeFiles(runtime, "javaw.exe", 4))
                    yield return exe;
        }

        private static IEnumerable<string> SafeDirectories(string dir)
        {
            try { return Directory.GetDirectories(dir); }
            catch { return Array.Empty<string>(); }
        }

        private static IEnumerable<string> SafeFiles(string dir, string fileName, int maxDepth)
        {
            try
            {
                return Directory.EnumerateFiles(dir, fileName, new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    MaxRecursionDepth = maxDepth,
                    IgnoreInaccessible = true
                }).ToList();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}
