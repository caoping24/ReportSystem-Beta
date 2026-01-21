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
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

            //this.Hide();            // 隐藏窗口
            this.ShowInTaskbar = false;
            InitializeTray();// 初始化托盘图标与菜单
            await StartEmbeddedApiAsync();// 可选：启动时自动启动后端
        }

        private async void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            e.Cancel = true;
            HideMainWindow();
        }

        private void InitializeTray()
        {
            if (_notifyIcon != null) return;// 如果已初始化，跳过
            var menu = new ContextMenuStrip();// 创建托盘菜单

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


            string icoName = "128.ico";// 自定义图标
            string icoPath = Path.Combine(AppContext.BaseDirectory, "wwwroot\\Images", icoName);
            Icon iconToUse;
            if (File.Exists(icoPath))
            {
                iconToUse = new Icon(icoPath);
            }
            else
            {
                iconToUse = SystemIcons.Application; // 回退图标
            }
            _notifyIcon = new NotifyIcon// 创建 NotifyIcon
            {
                Icon = iconToUse,
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
                // 直接用程序集目录
                string webApiProjectDir = Path.GetDirectoryName(typeof(CenterBackend.Program).Assembly.Location) ?? AppContext.BaseDirectory;
                string contentRootPath = Path.GetFullPath(webApiProjectDir);
                int port = 5260;
                // 传入正确的 contentRootPath
                var app = CenterBackend.Program.BuildWebApplication(Array.Empty<string>(), contentRootPath, port);
                await app.StartAsync();
                lock (_apiLock)
                {
                    _apiApp = app;
                }
                Dispatcher.Invoke(UpdateMenuState);// 更新托盘菜单状态（在UI线程）
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>// 如果启动失败，提示用户（UI线程）
                    System.Windows.MessageBox.Show($"启动服务失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error));
                await ExitApplicationAsync();
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
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // StopAsync 需要 CancellationToken
                await appToStop!.StopAsync(cts.Token);
                await appToStop.DisposeAsync();
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    System.Windows.MessageBox.Show($"停止服务失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                Dispatcher.Invoke(UpdateMenuState);
            }
        }

        private async Task ExitApplicationAsync()
        {
            try
            {
                await StopEmbeddedApiAsync();
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    System.Windows.MessageBox.Show($"推出应用失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            if (_notifyIcon != null)// 移除托盘图标
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
            System.Windows.Application.Current.Shutdown();// 退出应用
        }
    }
}