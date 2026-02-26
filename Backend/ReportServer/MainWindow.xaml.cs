using Microsoft.AspNetCore.Builder;
using Microsoft.Web.WebView2.Core;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Resources;
using System.Windows.Threading;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;

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
        private ToolStripMenuItem? _openMainWindow;
        // 使用与 Kestrel / Program.cs 中一致的主机地址（保持与 CORS 白名单一致）
        private const string HomePageUrl = "http://localhost:5260/user/login";        // 主页地址（常量，便于修改）
        private readonly object _apiLock = new();
        private Icon? _iconRunning; // 服务运行时图标（图标A）
        private Icon? _iconStopped; // 服务停止时图标（图标B）

        // 虚拟主机名（用于本地静态资源回退），避免与 API 的 origin 混淆（不要使用 "localhost"）
        private const string FallbackVirtualHost = "appassets";

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
            InitializeWebView2();
        }

        private async void InitializeWebView2()
        {
            try
            {
                // 初始化 WebView2 环境
                var env = await CoreWebView2Environment.CreateAsync();
                await webView.EnsureCoreWebView2Async(env);

                // 计算 dist 目录与 index.html 的路径（ReportServer 输出目录中的副本，用作回退）
                string distDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dist");
                string vueIndexPath = Path.Combine(distDir, "index.html");

                // 如果文件或目录不存在，提示并继续（但不阻止后续导航到嵌入式 API）
                if (!Directory.Exists(distDir) || !File.Exists(vueIndexPath))
                {
                    System.Windows.MessageBox.Show($"找不到 Vue 静态资源：{vueIndexPath}\n请确认 dist 已复制到输出目录。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    // 将本地文件夹映射到一个自定义虚拟主机名，用作未启动 API 时的静态回退页面
                    webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                              FallbackVirtualHost,
                              distDir,
                              CoreWebView2HostResourceAccessKind.Allow);

                    // 使用映射后的 HTTPS URL 加载回退页面（仅在 API 未启动前展示）
                    webView.Source = new Uri($"http://{FallbackVirtualHost}/index.html");
                }

                // 可选：启用开发者工具（F12）
                webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载Vue页面失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //this.Hide();            // 隐藏窗口
            this.ShowInTaskbar = false;
            InitializeTray();// 初始化托盘图标与菜单
            await StartEmbeddedApiAsync();// 启动后端并在启动成功后导航到 API 提供的页面（统一 origin）
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

            _openMainWindow = new ToolStripMenuItem("系统信息");
            _openMainWindow.Click += (_, __) => Dispatcher.Invoke(ShowAndActivateWindow);
            menu.Items.Add(_openMainWindow);

            var exitMenu = new ToolStripMenuItem("退出");
            exitMenu.Click += async (_, __) => await ExitApplicationAsync();
            menu.Items.Add(exitMenu);

            // 1. 加载图标
            _iconRunning = LoadIconFromResource("pack://application:,,,/AppIco/SL_Icon_Green.ico");
            _iconStopped = LoadIconFromResource("pack://application:,,,/AppIco/SL_Icon_Gray.ico");
            // 2. 异常回退：使用系统图标兜底
            if (_iconRunning == null) _iconRunning = SystemIcons.Shield;
            if (_iconStopped == null) _iconStopped = SystemIcons.Application;
            _notifyIcon = new NotifyIcon
            {
                Icon = _iconStopped!, // 初始状态：服务未启动
                Text = "ReportServer RT",
                ContextMenuStrip = menu,
                Visible = true
            };

            _notifyIcon.DoubleClick += (_, __) => Dispatcher.Invoke(OpenBrowserToHomePage);// 双击托盘打开浏览器访问主页（外部浏览器）
            UpdateMenuState();
        }

        private Icon? LoadIconFromResource(string packUri)// 从资源加载图标
        {
            try
            {
                Uri uri = new Uri(packUri, UriKind.Absolute);
                StreamResourceInfo resourceInfo = System.Windows.Application.GetResourceStream(uri);
                if (resourceInfo?.Stream != null)
                {
                    return new Icon(resourceInfo.Stream, 32, 32); // 固定32x32适配托盘
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    System.Windows.MessageBox.Show($"加载图标失败：{ex.Message}", "警告", MessageBoxButton.OK, MessageBoxImage.Warning));
            }
            return null;
        }

        private void ShowAndActivateWindow()
        {
            if (this.IsVisible == false)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.ShowInTaskbar = true;
            }
            this.Activate();
        }

        // 封装：打开系统默认浏览器并访问主页
        private void OpenBrowserToHomePage()
        {
            if (string.IsNullOrEmpty(HomePageUrl))
            {
                Dispatcher.Invoke(() =>
                    System.Windows.MessageBox.Show("主页地址未配置！", "错误", MessageBoxButton.OK, MessageBoxImage.Error));
                return;
            }
            try
            {
                Process.Start(new ProcessStartInfo(HomePageUrl)// 调用系统默认浏览器打开URL
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>// 异常处理：提示用户手动访问
                    System.Windows.MessageBox.Show(
                        $"打开浏览器失败：{ex.Message}\n请手动访问主页：{HomePageUrl}",
                        "访问失败",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    )
                );
            }
        }

        private async void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
            ShowInTaskbar = false;
            Topmost = false; // 隐藏时重置置顶状态
        }

        private void Window_GotFocus(object sender, RoutedEventArgs e)// 获得焦点时置顶
        {
            Topmost = true;
        }
        private void Window_LostFocus(object sender, RoutedEventArgs e)//失去焦点时取消置顶
        {
            if (IsVisible)
            {
                Topmost = false;
            }
        }
        private void UpdateMenuState()
        {
            if (!Dispatcher.CheckAccess())// 必须在 UI 线程执行（服务启动/停止是异步操作，可能触发非 UI 线程调用）
            {
                Dispatcher.Invoke(UpdateMenuState);
                return;
            }

            bool isServiceRunning = _apiApp != null;
            if (_startMenuItem != null) _startMenuItem.Enabled = !isServiceRunning;
            if (_stopMenuItem != null) _stopMenuItem.Enabled = isServiceRunning;

            if (_notifyIcon != null)// 根据服务状态切换托盘图标
            {
                _notifyIcon.Icon = isServiceRunning ? _iconRunning! : _iconStopped!;
                _notifyIcon.Text = isServiceRunning ? "ReportServer（服务运行中）" : "ReportServer（服务已停止）";
            }
        }

        private async Task StartEmbeddedApiAsync()
        {
            lock (_apiLock)
            {
                if (_apiApp != null) return; // 已经启动
            }

            try
            {
                // 直接用程序集目录（确保 CenterBackend 的 wwwroot/dist 已复制到输出目录或 contentRootPath 指向包含 wwwroot 的目录）
                string webApiProjectDir = Path.GetDirectoryName(typeof(CenterBackend.Program).Assembly.Location) ?? AppContext.BaseDirectory;
                string contentRootPath = Path.GetFullPath(webApiProjectDir);
                int port = 5260;

                // 构建并启动 WebApplication（调用方负责 StopAsync/DisposeAsync）
                var app = CenterBackend.Program.BuildWebApplication(Array.Empty<string>(), contentRootPath, port);
                await app.StartAsync();

                lock (_apiLock)
                {
                    _apiApp = app;
                }

                // 等待 API 对外可连通（StartAsync 通常已足够，但额外轮询可避免极短时间窗口导致导航失败）
                var httpClient = new HttpClient();
                bool apiReady = false;
                for (int i = 0; i < 20; i++) // 最多等待约 4 秒（20 * 200ms）
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(1000);
                        var resp = await httpClient.GetAsync(HomePageUrl, cts.Token);
                        if (resp.IsSuccessStatusCode)
                        {
                            apiReady = true;
                            break;
                        }
                    }
                    catch
                    {
                        // 忽略，继续重试
                    }
                    await Task.Delay(200);
                }

                // 在 UI 线程更新菜单并导航嵌入式 WebView 到 API 提供的页面（统一 origin，避免 CORS/同源问题）
                Dispatcher.Invoke(() =>
                {
                    UpdateMenuState();

                    try
                    {
                        if (apiReady)
                        {
                            if (webView?.CoreWebView2 != null)
                            {
                                webView.CoreWebView2.Navigate(HomePageUrl); // 确保是http://localhost:5260
                            }
                            else
                            {
                                webView.Source = new Uri(HomePageUrl);
                            }
                        }
                        else
                        {
                            // 如果 API 未能在短时间内响应，仍尝试导航（可能会在稍后成功）
                            if (webView?.CoreWebView2 != null)
                            {
                                webView.CoreWebView2.Navigate(HomePageUrl);
                            }
                            else
                            {
                                webView.Source = new Uri(HomePageUrl);
                            }
                            System.Windows.MessageBox.Show("后端已启动，但短时间内未响应。若页面仍无法正常调用接口，请检查防火墙或端口占用。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception navEx)
                    {
                        System.Windows.MessageBox.Show($"导航到内嵌 API 页面失败：{navEx.Message}", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                });
            }
            catch (Exception ex)
            {
                // 输出详细异常信息（包含内部异常）
                string errorMsg = $"启动服务失败：{ex.Message}\n" +
                                 $"内部异常：{ex.InnerException?.Message}\n";
                Dispatcher.Invoke(() =>
                    System.Windows.MessageBox.Show(errorMsg, "错误", MessageBoxButton.OK, MessageBoxImage.Error));
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
                    System.Windows.MessageBox.Show($"退出应用失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            Dispatcher.Invoke(() =>
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false; // 先隐藏
                    _notifyIcon.Dispose();      // 释放资源
                    _notifyIcon = null;
                }
            });
            //手动释放图标资源
            _iconStopped?.Dispose();
            _iconRunning?.Dispose();

            await Task.Delay(200);// 延迟一小段时间再关闭，给系统处理图标移除的时间
            System.Windows.Application.Current.Shutdown();
        }
    }
}