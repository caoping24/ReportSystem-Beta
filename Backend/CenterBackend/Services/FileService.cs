using CenterBackend.IFileService;
using ICSharpCode.SharpZipLib.Zip;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System;
using System.IO;
using System.Text;
namespace CenterBackend.Services
{
    /// <summary>
    /// 文件操作服务实现类 - 实现所有文件相关的具体逻辑
    /// </summary>
    public class FileService : IFileServices
    {
        /// <summary>
        /// 创建文件夹【兼容单层/多层】，路径存在则跳过，无异常抛出
        /// </summary>
        /// <param name="folderPath">待创建的文件夹完整路径</param>
        public void CreateFolder(string folderPath)
        {
            if (!string.IsNullOrWhiteSpace(folderPath) && !Directory.Exists(folderPath))//判空+判存在
            {
                Directory.CreateDirectory(folderPath);//自动兼容多级创建
            }
        }

        /// <summary>
        /// 复制文件【核心封装】自动创建目标文件夹，解决原生File.Copy不建文件夹的问题
        /// </summary>
        /// <param name="sourceFilePath">源文件完整路径</param>
        /// <param name="targetFilePath">目标文件完整路径</param>
        /// <param name="overwrite">是否覆盖已存在的目标文件，默认:true</param>
        /// <returns>复制成功返回true，失败返回false</returns>
        public bool CopyFile(string sourceFilePath, string targetFilePath, bool overwrite = true)
        {
            try
            {
                if (!File.Exists(sourceFilePath)) throw new FileNotFoundException($"源文件不存在：{sourceFilePath}");//校验源文件

                string targetFolder = Path.GetDirectoryName(targetFilePath);//提取目标文件夹路径
                CreateFolder(targetFolder);//自动创建目标文件夹

                File.Copy(sourceFilePath, targetFilePath, overwrite);//执行文件复制
                return true;
            }
            catch
            {
                return false;//异常则返回失败
            }
        }

        /// <summary>
        /// 压缩指定文件夹内所有内容为Zip包【含子文件夹+保持原结构】
        /// </summary>
        /// <param name="sourceFolderPath">待压缩的源文件夹路径</param>
        /// <param name="zipSavePath">压缩包保存的完整路径(含文件名.zip)</param>
        /// <returns>压缩成功返回true，失败返回false</returns>
        public bool CompressFolderToZip(string sourceFolderPath, string zipSavePath)
        {
            try
            {
                if (!Directory.Exists(sourceFolderPath)) throw new DirectoryNotFoundException($"文件夹不存在：{sourceFolderPath}");//校验源文件夹

                string zipFolder = Path.GetDirectoryName(zipSavePath);//提取压缩包所在文件夹
                CreateFolder(zipFolder);//自动创建存储目录

                // 流式写入，UTF8解决中文乱码，无内存溢出风险
                using (var fs = new FileStream(zipSavePath, FileMode.Create, FileAccess.Write))
                using (var zipStream = new ZipOutputStream(fs) { IsStreamOwner = true })
                {
                    zipStream.SetLevel(6);//0-9，6=速度+压缩率最优平衡
                    CompressDirectory(sourceFolderPath, zipStream, sourceFolderPath);//递归压缩文件/子文件夹
                    zipStream.Finish();
                }
                return File.Exists(zipSavePath);//校验压缩包是否生成
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 压缩单个文件为独立Zip包
        /// </summary>
        /// <param name="sourceFilePath">待压缩的源文件完整路径</param>
        /// <param name="zipSavePath">压缩包保存的完整路径(含文件名.zip)</param>
        /// <returns>压缩成功返回true，失败返回false</returns>
        public bool CompressSingleFileToZip(string sourceFilePath, string zipSavePath)
        {
            try
            {
                if (!File.Exists(sourceFilePath)) throw new FileNotFoundException($"文件不存在：{sourceFilePath}");//校验源文件

                string zipFolder = Path.GetDirectoryName(zipSavePath);//提取压缩包所在文件夹
                CreateFolder(zipFolder);//自动创建存储目录

                // 流式写入，缓冲区读取，适配大文件
                using (var fs = new FileStream(zipSavePath, FileMode.Create, FileAccess.Write))
                using (var zipStream = new ZipOutputStream(fs) { IsStreamOwner = true })
                {
                    zipStream.SetLevel(6);//最优压缩级别
                    var fileInfo = new FileInfo(sourceFilePath);
                    var zipEntry = new ZipEntry(fileInfo.Name) { DateTime = DateTime.Now };//创建压缩项
                    zipStream.PutNextEntry(zipEntry);

                    byte[] buffer = new byte[4096];//4K缓冲区，平衡性能与内存
                    using (var fileStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
                    {
                        int bytesRead;
                        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            zipStream.Write(buffer, 0, bytesRead);
                        }
                    }
                    zipStream.CloseEntry();
                    zipStream.Finish();
                }
                return File.Exists(zipSavePath);//校验压缩包是否生成
            }
            catch
            {
                return false;
            }
        }

        #region 私有辅助方法
        /// <summary>
        /// 递归压缩文件夹内所有文件/子文件夹，保持原目录相对结构
        /// </summary>
        /// <param name="folderPath">当前遍历的文件夹路径</param>
        /// <param name="zipStream">压缩输出流</param>
        /// <param name="rootFolder">压缩根目录，用于生成相对路径</param>
        private void CompressDirectory(string folderPath, ZipOutputStream zipStream, string rootFolder)
        {
            //遍历当前文件夹所有文件
            foreach (string file in Directory.GetFiles(folderPath))
            {
                var fileInfo = new FileInfo(file);
                string entryName = file.Substring(rootFolder.Length + 1);//生成压缩包内相对路径
                var zipEntry = new ZipEntry(entryName) { DateTime = DateTime.Now };
                zipStream.PutNextEntry(zipEntry);

                byte[] buffer = new byte[4096];//4K缓冲区
                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    int bytesRead;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        zipStream.Write(buffer, 0, bytesRead);
                    }
                }
                zipStream.CloseEntry();
            }

            //递归遍历子文件夹
            foreach (string dir in Directory.GetDirectories(folderPath))
            {
                CompressDirectory(dir, zipStream, rootFolder);
            }
        }
        #endregion

        /// <summary>
        /// 重载：按日期计算层级路径，支持创建文件夹+指定返回路径层级
        /// </summary>
        /// <param name="rootPath">存储根路径</param>
        /// <param name="targetDate">目标日期</param>
        /// <param name="IsCreateFolder">是否自动创建文件夹，默认:false</param>
        /// <param name="Type">路径类型 1=年 2=月 3=周 0=空值，默认:0</param>
        /// <returns>指定层级的完整物理路径，非法Type返回空字符串</returns>
        public string GetDateFolderPath(string rootPath, DateTime targetDate, bool IsCreateFolder = false, int Type = 0)
        {
            var paths = GetDateFolderPath(rootPath, targetDate);//获取三级完整路径
            if (IsCreateFolder) CreateDateFolder(rootPath, targetDate);//需要则创建文件夹

            //按需返回对应路径
            if (Type == 1) return paths.yearPath;
            if (Type == 2) return paths.monthPath;
            if (Type == 3) return paths.weekPath;
            return string.Empty;
        }

        /// <summary>
        /// 核心：按日期计算年/月/周三级完整路径
        /// </summary>
        /// <param name="rootPath">存储根路径</param>
        /// <param name="targetDate">目标日期</param>
        /// <returns>元组(年路径,月路径,周路径)</returns>
        public (string yearPath, string monthPath, string weekPath) GetDateFolderPath(string rootPath, DateTime targetDate)
        {
            if (string.IsNullOrWhiteSpace(rootPath)) return (string.Empty, string.Empty, string.Empty);//根路径判空

            DateTime currentDate = targetDate.Date;//去时分秒
            DateTime weekFirstDay = GetWeekFirstDay(currentDate);//取本周一
            int weekNum = GetWeekNumberInMonth(new DateTime(currentDate.Year, currentDate.Month, 1), currentDate);//取当月周数
            string weekFolderName = $"{weekNum:00}周";//周文件夹名 01周格式

            string yearPath; string monthPath; string weekPath;
            //跨月判断：本周一在当月则用当前年月，否则用周一的年月
            if (weekFirstDay.Year == currentDate.Year && weekFirstDay.Month == currentDate.Month)
            {
                yearPath = Path.Combine(rootPath, currentDate.Year.ToString());
                monthPath = Path.Combine(yearPath, $"{currentDate.Month:00}月");
                weekPath = Path.Combine(monthPath, weekFolderName);
            }
            else
            {
                yearPath = Path.Combine(rootPath, weekFirstDay.Year.ToString());
                monthPath = Path.Combine(yearPath, $"{weekFirstDay.Month:00}月");
                weekPath = Path.Combine(monthPath, weekFolderName);
            }
            return (yearPath, monthPath, weekPath);//返回三级路径
        }

        /// <summary>
        /// 按日期创建年/月/周三级文件夹(存在则不创建)
        /// </summary>
        /// <param name="rootPath">存储根路径</param>
        /// <param name="targetDate">目标日期</param>
        public void CreateDateFolder(string rootPath, DateTime targetDate)
        {
            var paths = GetDateFolderPath(rootPath, targetDate);//获取三级路径
            CreateFolder(paths.yearPath);
            CreateFolder(paths.monthPath);
            CreateFolder(paths.weekPath);
        }

        #region 私有辅助方法
        /// <summary>
        /// 获取指定日期的本周第一天(周一为周首)
        /// </summary>
        /// <param name="dt">目标日期</param>
        /// <returns>本周一日期</returns>
        private DateTime GetWeekFirstDay(DateTime dt)
        {
            int diff = dt.DayOfWeek - DayOfWeek.Monday;//计算与周一的差值
            if (diff < 0) diff += 7;//周日处理为-1，补7天
            return dt.AddDays(-diff).Date;
        }

        /// <summary>
        /// 获取指定日期在当月的周序号
        /// </summary>
        /// <param name="monthFirstDay">当月第一天</param>
        /// <param name="currentDay">目标日期</param>
        /// <returns>当月周数(从1开始)</returns>
        private int GetWeekNumberInMonth(DateTime monthFirstDay, DateTime currentDay)
        {
            int firstWeekDay = (int)monthFirstDay.DayOfWeek;
            if (firstWeekDay == 0) firstWeekDay = 7;//周日转7
            int daysPast = (currentDay.Day - 1) + (firstWeekDay - 1);//计算总偏移天数
            return (daysPast / 7) + 1;//返回周序号
        }
        #endregion
    }
}