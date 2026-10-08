using System;
using System.IO;
using System.Windows;

namespace PlumbingSolution.Ultis
{
    public class IO
    {
        public static void LogException(Exception exception)
        {
            string temp_path = Directory.GetParent(Path.GetTempPath()).Parent.FullName;
            string path = Path.Combine(temp_path, "AddinPlumbingSolutionLog", "log_exceptions.txt");
            AppendToFile(path, GetExceptionData(exception));
        }

        public static void LogException(string content)
        {
            string temp_path = Directory.GetParent(Path.GetTempPath()).Parent.FullName;
            string path = Path.Combine(temp_path, "AddinPlumbingSolutionLog", "log_exceptions.txt");
            AppendToFile(path, content);
        }

        private static string GetExceptionData(Exception ex)
        {
            string mess = "Time log: " + DateTime.Now + "\n";
            mess += "Exception: " + ex.Message + "\n";
            mess += "StackTrace " + ex.StackTrace + "\n";
            mess += "Source " + ex.Source.ToString() + "\n";
            mess += "Data " + ex.Data.ToString() + "\n";
            if (ex.InnerException != null)
                mess += "Inner exception \n\t" + GetExceptionData(ex.InnerException);
            return mess;
        }

        public static void AppendToFile(string filePath, string content)
        {
            try
            {
                if (!Directory.Exists(Directory.GetParent(filePath).FullName))
                    Directory.CreateDirectory(Directory.GetParent(filePath).FullName);
                using (StreamWriter writer = new StreamWriter(filePath, append: true))
                {
                    writer.WriteLine(content);
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        /// <summary>
        /// Show exception information
        /// </summary>
        /// <param name="ex"></param>
        public static void ShowException(Exception ex)
        {
            MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
        }

        /// <summary>
        /// Show message information
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        public static void ShowInformation(string message, string title = "Information")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Show message warning
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        public static void ShowWarning(string message, string title = "Warning")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Show message error
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        public static void ShowError(string message, string title = "Error")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}