using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace VibrantbitLauncher.Services
{
    /// <summary>
    /// Modrinth 搜索。
    ///
    /// 自己实现而不用 MinecraftLaunch 的 ModrinthProvider：后者的 SearchAsync 只有
    /// searchFilter / version / modLoader 三个参数，写死了 mod 类型，无法查整合包、
    /// 资源包和光影。这里直接走 Modrinth 的 search 接口，用 facets 指定 project_type。
    /// 接口文档：https://docs.modrinth.com/api/operations/searchprojects/
    /// </summary>
    public static class ModrinthSearchService
    {
        public const string TypeMod = "mod";
        public const string TypeModpack = "modpack";
        public const string TypeResourcePack = "resourcepack";
        public const string TypeShader = "shader";

        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            try { client.DefaultRequestHeaders.UserAgent.ParseAdd("VibrantbitLauncher/1.0"); } catch { }
            return client;
        }

        public sealed class Hit
        {
            [JsonPropertyName("project_id")] public string ProjectId { get; set; } = string.Empty;
            [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
            [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
            [JsonPropertyName("icon_url")] public string? IconUrl { get; set; }
            [JsonPropertyName("versions")] public List<string> Versions { get; set; } = new();
        }

        private sealed class SearchResponse
        {
            [JsonPropertyName("hits")] public List<Hit> Hits { get; set; } = new();
        }

        /// <summary>按项目类型搜索；gameVersion / loader 传 null 或 "全部" 表示不筛选。</summary>
        public static async Task<List<Hit>> SearchAsync(
            string projectType,
            string? query = null,
            string? gameVersion = null,
            string? loader = null,
            CancellationToken cancellationToken = default)
        {
            var facets = new List<string> { $"[\"project_type:{projectType}\"]" };

            if (!string.IsNullOrWhiteSpace(gameVersion) && gameVersion != "全部")
                facets.Add($"[\"versions:{gameVersion}\"]");

            if (!string.IsNullOrWhiteSpace(loader) && loader != "全部")
                facets.Add($"[\"categories:{loader}\"]");

            var facetsJson = Uri.EscapeDataString("[" + string.Join(",", facets) + "]");
            var url = $"https://api.modrinth.com/v2/search?limit=30&facets={facetsJson}";

            if (!string.IsNullOrWhiteSpace(query))
                url += $"&query={Uri.EscapeDataString(query)}";

            var json = await Http.GetStringAsync(url, cancellationToken);
            return JsonSerializer.Deserialize<SearchResponse>(json)?.Hits ?? new List<Hit>();
        }
    }
}