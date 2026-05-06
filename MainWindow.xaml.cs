using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;

namespace Windows_Simple_DashBoard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private System.Windows.Threading.DispatcherTimer? _updateTimer;
        private readonly DateTime _startTime = DateTime.Now - TimeSpan.FromHours(4); // 예시 업타임
        private PerformanceCounter? _cpuCounter;
        private PerformanceCounter? _ramCounter;
        private double _totalRamGb;
        private int _refreshCount = 0;

        public MainWindow()
        {
            InitializeComponent();

            // 카운터 초기화 메서드를 반드시 호출해야 합니다!
            InitializePerformanceCounters();

            // DriveItem 클래스 안에 있는 static 메서드이므로 클래스명을 붙여줍니다.
            DriveItemsControl.ItemsSource = DriveItem.GetDriveStatus();

            bool isLight = ThemeHelper.IsLightTheme();
            ThemeSwitch.IsChecked = !isLight;
            ThemeHelper.UpdateAppTheme(isLight); // 초기 실행 시 테마 적용

            // 창이 완전히 로드된 후 실행될 이벤트를 연결합니다.
            this.Loaded += MainWindow_Loaded;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // 브라우저로 해당 URL 열기
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // UI가 준비된 상태에서 데이터를 바인딩합니다. (여기서 에러가 났던 겁니다!)
            RefreshDriveInfo();

            // 타이머 시작
            _updateTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            // 기존 _startTime 방식 대신 시스템 틱을 직접 사용
            TimeSpan uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);

            // 일(days)이 있을 경우를 대비해 포맷 변경
            string uptimeText = uptime.Days > 0
                ? $"{uptime.Days}d {uptime.Hours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}"
                : $"{uptime.Hours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}";

            UptimeTextBlock.Text = $"Uptime: {uptimeText}";

            // 2. CPU 점유율 갱신
            if (_cpuCounter != null)
            {
                // 변수 할당 없이 바로 출력 (IDE0059 해결)
                CpuTextBlock.Text = $"{_cpuCounter.NextValue():F1}%";
            }

            // UpdateTimer_Tick 내 RAM 로직 수정
            if (_ramCounter != null)
            {
                float availableMb = _ramCounter.NextValue();
                double availableGb = availableMb / 1024.0;
                double usedGb = _totalRamGb - availableGb;
                double usagePercent = (usedGb / _totalRamGb) * 100;

                // "75.2% (4.2GB Free)" 형식으로 표시
                RamTextBlock.Text = $"{usagePercent:F1}% ({availableGb:F1}GB Free)";

               // RAM 사용량이 90% 이상일때 빨간색으로 표시 
                if (usagePercent >= 90)
                {
                    RamTextBlock.Foreground = Brushes.Red;
                    RamTextBlock.FontWeight = FontWeights.Bold;
                }
                else
                {
                    // 90% 미만일 때 CPU 라벨과 같은 색으로 표시 
                 
                    RamTextBlock.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x88, 0x88, 0x88));
                    RamTextBlock.FontWeight = FontWeights.Normal;
                }
            }
            // 5초마다 드라이브 정보 갱신
            _refreshCount++;
            if (_refreshCount >= 5)
            {
                RefreshDriveInfo();
                _refreshCount = 0;
            }


        }
        private void RefreshDriveInfo()
        {
            // 에러 방지를 위해 ItemsSource를 넣기 전 확인
            if (DriveItemsControl != null)
            {
                DriveItemsControl.ItemsSource = DriveItem.GetDriveStatus();
            }
        }

        // 드라이브 카드 클릭 시 탐색기 열기 이벤트
        private void DriveCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is DriveItem item)
            {
                // item.Name에서 경로(예: C:\)를 추출하여 탐색기 실행
                string path = item.Name?.Split('(').Last().TrimEnd(')') ?? "";
                if (!string.IsNullOrEmpty(path))
                {
                    Process.Start("explorer.exe", path);
                }
            }
        }

        private void InitializePerformanceCounters()
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            // 전체 물리 메모리 용량 가져오기 (GB 단위)
            var gcStatus = GC.GetGCMemoryInfo();
            _totalRamGb = gcStatus.TotalAvailableMemoryBytes / 1024.0 / 1024.0 / 1024.0;
        }

        private void ThemeSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                // 체크되면 다크 모드(라이트 모드 false), 해제되면 라이트 모드(true)
                bool isDarkMode = cb.IsChecked ?? false;
                //ThemeHelper.SetWindowsTheme(!isDarkMode);
                ThemeHelper.UpdateAppTheme(!isDarkMode); // 앱 UI 색상도 즉시 변경

                // 알림 메시지 (선택 사항)
                // MessageBox.Show(isDarkMode ? "다크 모드로 전환되었습니다." : "라이트 모드로 전환되었습니다.");
            }
        }
    }

    public class DriveItem
    {
        public string? Name { get; set; }           // 드라이브 이름 (예: C:)
        public double UsagePercentage { get; set; } // 사용량 (0~100)
        public string? UsageText { get; set; }       // "65% Used"
        public string? SpaceText { get; set; }       // "120GB free of 500GB"

        public Brush UsageColor
        {
            get
            {
                // 90% 이상이면 빨간색, 아니면 테마의 강조색 사용
                if (UsagePercentage >= 90)
                    return Brushes.Red;

                return (Brush)Application.Current.Resources["PrimaryAccent"];
            }
        }

        public static List<DriveItem> GetDriveStatus()
        {
            List<DriveItem> driveList = [];
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            foreach (DriveInfo d in allDrives)
            {
                if (d.IsReady && d.DriveType == DriveType.Fixed) // 고정 드라이브만
                {
                    double usedSpace = d.TotalSize - d.AvailableFreeSpace;
                    double usagePercent = (usedSpace / d.TotalSize) * 100;

                    driveList.Add(new DriveItem
                    {
                        Name = $"{d.VolumeLabel} ({d.Name})",
                        UsagePercentage = usagePercent,
                        UsageText = $"{Math.Round(usagePercent, 1)}% Used",
                        SpaceText = $"{FormatBytes(d.AvailableFreeSpace)} free of {FormatBytes(d.TotalSize)}"
                    });
                }
            }
            return driveList;
        }

        // 용량 단위 변환 함수 (GB 단위 위주)
        private static string FormatBytes(long bytes)
        {
            return $"{Math.Round(bytes / 1024.0 / 1024.0 / 1024.0, 1)}GB";
        }
    }
    public static class ThemeHelper
    {
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string RegistryValueName = "AppsUseLightTheme";

        public static bool IsLightTheme()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            object? registryValueObject = key?.GetValue(RegistryValueName);
            if (registryValueObject == null) return true;
            return (int)registryValueObject > 0;
        }

        public static void SetWindowsTheme(bool isLight)
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
            {
                key?.SetValue(RegistryValueName, isLight ? 1 : 0, RegistryValueKind.DWord);
            }

            // 시스템에 테마 변경을 통지합니다.
            SendMessage(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "ImmersiveColorSet");
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);

        private const uint WM_SETTINGCHANGE = 0x001A;
        private static readonly IntPtr HWND_BROADCAST = new(0xffff);

        public static void UpdateAppTheme(bool isLight)
        {
            var app = (App)Application.Current;
            var res = app.Resources;

            if (isLight)
            {
                res["WindowBackground"] = new SolidColorBrush(Color.FromRgb(243, 243, 243));
                res["CardBackground"] = Brushes.White;
                res["PrimaryText"] = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                res["PrimaryAccent"] = new SolidColorBrush(Color.FromRgb(0, 120, 212)); // 이것도 추가
                res["SecondaryText"] = new SolidColorBrush(Color.FromRgb(102, 102, 102)); // #666
            }
            else
            {
                res["WindowBackground"] = new SolidColorBrush(Color.FromRgb(32, 32, 32));
                res["CardBackground"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                res["PrimaryText"] = Brushes.White;
                res["PrimaryAccent"] = new SolidColorBrush(Color.FromRgb(55, 158, 255)); // 다크모드용 연파랑
                res["SecondaryText"] = new SolidColorBrush(Color.FromRgb(170, 170, 170)); // 연회색
            }
        }
    }

}