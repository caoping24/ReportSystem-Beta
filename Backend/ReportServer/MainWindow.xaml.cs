using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.AspNetCore.Builder;
using System.Windows.Forms; // NotifyIcon, ContextMenuStrip, ToolStripItem
using System.Drawing; // SystemIcons

namespace ReportServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WebApplication? _apiApp;
        private NotifyIcon? _notifyIcon;
        private ToolStripMenuItem? _startMenuItem;
        private ToolStripMenuItem? _stopMenuItem;
        private readonly object _apiLock = new();

        public MainWindow()
        {
            InitializeComponent();

            // 窗口启动时不显示（只驻留托盘）
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 隐藏窗口并不显示任务栏图标
            this.Hide();
            this.ShowInTaskbar = false;

            // 初始化托盘图标与菜单
            InitializeTray();

            // 可选：启动时自动启动后端（如不希望自动启动，请注释掉）
            await StartEmbeddedApiAsync();
        }

        private async void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            // 用户关闭窗口时改为退出应用（托盘图标移除、停止后端）
            e.Cancel = true; // 先取消关闭，执行退出逻辑
            await ExitApplicationAsync();
        }

        private void InitializeTray()
        {
            // 如果已初始化，跳过
            if (_notifyIcon != null) return;

            // 创建托盘菜单
            var menu = new ContextMenuStrip();

            _startMenuItem = new ToolStripMenuItem("启动后端");
            _startMenuItem.Click += async (_, __) => await StartEmbeddedApiAsync();
            menu.Items.Add(_startMenuItem);

            _stopMenuItem = new ToolStripMenuItem("停止后端");
            _stopMenuItem.Click += async (_, __) => await StopEmbeddedApiAsync();
            menu.Items.Add(_stopMenuItem);

            menu.Items.Add(new ToolStripSeparator());

            var showMenu = new ToolStripMenuItem("打开窗口");
            showMenu.Click += (_, __) => Dispatcher.Invoke(ShowMainWindow);
            menu.Items.Add(showMenu);

            var exitMenu = new ToolStripMenuItem("退出");
            exitMenu.Click += async (_, __) => await ExitApplicationAsync();
            menu.Items.Add(exitMenu);

            // 创建 NotifyIcon
            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application, // 可替换为自定义图标
                Text = "ReportServer",
                ContextMenuStrip = menu,
                Visible = true
            };

            // 双击托盘显示窗口
            _notifyIcon.DoubleClick += (_, __) => Dispatcher.Invoke(ShowMainWindow);

            UpdateMenuState();
        }

        private void ShowMainWindow()
        {
            if (this.IsVisible == false)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
                this.ShowInTaskbar = true;
            }
        }

        private void HideMainWindow()
        {
            this.Hide();
            this.ShowInTaskbar = false;
        }

        private void UpdateMenuState()
        {
            bool running = _apiApp != null;
            if (_startMenuItem != null) _startMenuItem.Enabled = !running;
            if (_stopMenuItem != null) _stopMenuItem.Enabled = running;
        }

        private async Task StartEmbeddedApiAsync()
        {
            lock (_apiLock)
            {
                if (_apiApp != null) return; // 已经启动
            }

            try
            {
                // 【核心修正】手动指定 Web API 项目的 wwwroot 所在目录（根据实际路径调整）
                // 方式1：调试阶段 - 指向 Web API 项目的 wwwroot 目录（示例路径，需替换为你的实际路径）
                string webApiProjectDir = Path.Combine(AppContext.BaseDirectory, "D:\\Prj\\hbPrj1\\Backend\\CenterBackend"); // 相对路径，适配调试
                string contentRootPath = Path.GetFullPath(webApiProjectDir);

                // 方式2：发布阶段 - 确保 wwwroot 被复制到 WPF 输出目录，直接用程序集目录
                // string contentRootPath = Path.GetDirectoryName(typeof(CenterBackend.Program).Assembly.Location) ?? AppContext.BaseDirectory;

                int port = 5260;

                // 传入正确的 contentRootPath
                var app = CenterBackend.Program.BuildWebApplication(Array.Empty<string>(), contentRootPath, port);

                await app.StartAsync(); // 异步启动

                lock (_apiLock)
                {
                    _apiApp = app;
                }

                // 更新托盘菜单状态（在UI线程）
                Dispatcher.Invoke(UpdateMenuState);
            }
            catch (Exception ex)
            {
                // 如果启动失败，提示用户（UI线程）
                Dispatcher.Invoke(() =>
                    System.Windows.MessageBox.Show($"启动后端失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }
        private async Task StopEmbeddedApiAsync()
        {
            WebApplication? appToStop = null;
            lock (_apiLock)
            {
                if (_apiApp == null) return;
                appToStop = _apiApp;
                _apiApp = null;
            }

            try
            {
                // StopAsync 需要 CancellationToken
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await appToStop!.StopAsync(cts.Token);

                // 异步释放
                await appToStop.DisposeAsync();
            }
            catch
            {
                // 忽略停止异常，也可以改为日志记录
            }
            finally
            {
                // 更新托盘菜单状态（在UI线程）
                Dispatcher.Invoke(UpdateMenuState);
            }
        }

        private async Task ExitApplicationAsync()
        {
            // 停止后端（若在运行）
            try
            {
                await StopEmbeddedApiAsync();
            }
            catch { }

            // 移除托盘图标
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }

                // 真正退出应用（消除 System.Windows.Forms.Application 与 System.Windows.Application 的歧义）
                System.Windows.Application.Current.Shutdown();
        }
    }
}