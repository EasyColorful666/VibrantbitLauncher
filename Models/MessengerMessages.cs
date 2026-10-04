namespace VibrantbitLauncher.Models
{
    /// <summary>
    /// 请求下载中心切换到指定 Tab（0 = 游戏版本，1 = 模组资源）。
    /// CommunityToolkit.Mvvm 的消息必须是引用类型，因此不再用裸 int 传递。
    /// </summary>
    public sealed record DownloadHubTabMessage(int Index);
}
