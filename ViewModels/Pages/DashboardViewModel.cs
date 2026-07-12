using GalaSoft.MvvmLight;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;
using System.Management;
using System;

namespace VibrantbitLauncher.ViewModels.Pages
{
    public class DashboardPageViewModel : ViewModelBase
    {
        public DashboardPageViewModel()
        {
           Task.Run(() =>
           {
              GetCPUUsageAndMemoryUsage();
              GetHardwareInformation();
           });
        }
        private int _cpuUsage;
        public int CPUUsage
        {
            get => _cpuUsage;
            set => Set(ref _cpuUsage, value);
        }
        private int _memoryUsage;
        public int MemoryUsage
        {
            get => _memoryUsage;
            set => Set(ref _memoryUsage, value);
        }
        private string _hardwareInformation;

        
        public string HardwareInformation
        {
            get => _hardwareInformation;
            set => Set(ref _hardwareInformation, value);
        }

        private async void GetCPUUsageAndMemoryUsage()
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            var memCounter = new PerformanceCounter("Memory", "Available MBytes");
            cpuCounter.NextValue(); // 第一次调用丢弃
            Thread.Sleep(1000);
            cpuCounter.NextValue(); // 预热
            await Task.Delay(1000); // 等待PDH刷新

            var timer = new Timer(_ =>
            {
                float cpu = cpuCounter.NextValue();
                float mem = memCounter.NextValue();
                _cpuUsage = (int)Math.Round(cpu);
                _memoryUsage = (int)Math.Round(mem);
            }, null, 0, 500);
        }
        private void GetHardwareInformation()
        {
            var CPUName = "";
            var management = new ManagementObjectSearcher("Select * from Win32_Processor");
            foreach (var baseObject in management.Get())
            {
                var managementObject = (ManagementObject)baseObject;
                CPUName = managementObject["Name"].ToString();
            }
            float memoryCount = 0;
            var mc = new ManagementClass("Win32_ComputerSystem");
            var moc = mc.GetInstances();
            foreach (var o in moc)
            {
                var mo = (ManagementObject)o;
                string str1 = mo["TotalPhysicalMemory"].ToString();//单位为 B
                float a = long.Parse(str1);
                memoryCount = a / 1024 / 1024 / 1024;//单位换成GB
            }
            string DisplayName = "";
            ManagementClass m = new ManagementClass("Win32_VideoController");
            ManagementObjectCollection mn = m.GetInstances();
            DisplayName = "显卡数量：" + mn.Count.ToString() + "  " + "\n";
            ManagementObjectSearcher mos = new ManagementObjectSearcher("Select * from Win32_VideoController");//Win32_VideoController 显卡
            int count = 0;
            foreach (ManagementObject mo in mos.Get())
            {
                count++;
                DisplayName += "第" + count.ToString() + "张显卡名称：" + mo["Name"].ToString() + "   " + "\n";
            }
            mn.Dispose();
            m.Dispose();
            Process cmd = new Process();
            cmd.StartInfo.FileName = "ipconfig.exe";//设置程序名   
            cmd.StartInfo.Arguments = "/all";  //参数   

            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.CreateNoWindow = true;//不显示窗口（控制台程序是黑屏）   

            cmd.Start();
            string info = cmd.StandardOutput.ReadToEnd();
            string[] de = new string[info.Length];
            cmd.WaitForExit();
            cmd.Close();

            string patten = (
     @"((([0-9A-Fa-f]{1,4}:){7}([0-9A-Fa-f]{1,4}|:))|(([0-9A-Fa-f]{1,4}:)
     {6}(:[0-9A-Fa-f]{1,4}|((25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)(\\.(25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)){3})|:))|(([0-9A-Fa-f]{1,4}:){5}(((:[0-9A-Fa-f]{1,4}){1,2})|:
     ((25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)(\\.(25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)){3})|:))|(([0-9A-Fa-f]{1,4}:){4}(((:[0-9A-Fa-f]{1,4}){1,3})|((:[0-9A-Fa-f]{1,4})?:
     ((25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)(\\.(25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){3}(((:[0-9A-Fa-f]{1,4}){1,4})|((:[0-9A-Fa-f]{1,4}){0,2}:
     ((25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)(\\.(25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){2}(((:[0-9A-Fa-f]{1,4}){1,5})|((:[0-9A-Fa-f]{1,4}){0,3}:
     ((25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)(\\.(25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){1}(((:[0-9A-Fa-f]{1,4}){1,6})|((:[0-9A-Fa-f]{1,4}){0,4}:
     ((25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)(\\.(25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)){3}))|:))|(:(((:[0-9A-Fa-f]{1,4}){1,7})|((:[0-9A-Fa-f]{1,4}){0,5}:((25[0-5]|2[0-4]
     \\d|1\\d\\d|[1-9]?\\d)(\\.(25[0-5]|2[0-4]\\d|1\\d\\d|[1-9]?\\d)){3}))|:)))(%.+)?");

            Match m1 = Regex.Match(info, patten);
            var ipString = m1.ToString();
            string ip = " ";
            var v = ipString.Split('(');//获取到的IPV6地址后面会有一个（首选），在这里把他去掉
            string name = Dns.GetHostName();
            IPAddress[] ipadrlist = Dns.GetHostAddresses(name);
            foreach (IPAddress ipa in ipadrlist)
            {
                if (ipa.AddressFamily == AddressFamily.InterNetwork)
                {
                    ip = ipa.ToString();
                }
                else
                {
                    ip = "IPv4为空";
                }
            }

            //这种模式在插入一个U盘后可能会有不同的结果，如插入我的手机时
            var hDid = string.Empty;
            var mc1 = new ManagementClass("Win32_DiskDrive");
            var moc1 = mc.GetInstances();
            foreach (var o in moc1)
            {
                var mo1 = (ManagementObject)o;
                hDid = hDid + (string)mo1.Properties["Model"].Value + "\n";
            }

            HardwareInformation = "CPU: " + CPUName + "\n" + "内存: " + memoryCount + "GB" + "\n" + DisplayName + "\n" + "IPV4: " + ip + "\n" + "IPV6: " + v.First() + "\n" + "硬盘: " + hDid;
        }

    }
}
