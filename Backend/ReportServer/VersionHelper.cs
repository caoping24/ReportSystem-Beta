//using System.Reflection;
//using System.Diagnostics;

//namespace ReportServer.Helpers
//{
//    public static class VersionHelper
//    {
//        // 获取程序集版本（包含 Git 提交次数）
//        public static string GetAssemblyVersion()
//        {
//            var assembly = Assembly.GetExecutingAssembly();
//            return assembly.GetName().Version?.ToString() ?? "未知版本";
//        }

//        // 获取包含 Git 分支的产品版本
//        public static string GetProductVersion()
//        {
//            var assembly = Assembly.GetExecutingAssembly();
//            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
//            return fileVersionInfo.ProductVersion ?? GetAssemblyVersion();
//        }

//        // 获取完整版本信息
//        public static string GetFullVersion()
//        {
//            return $"程序集版本：{GetAssemblyVersion()}\n产品版本：{GetProductVersion()}";
//        }
//    }
//}