using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace PlumbingSolution.FireProtection.Utils
{
    public class IO
    {



        public static string GetAssemlyFolder()
        {
            string location = Assembly.GetExecutingAssembly().Location;
            string dir = Path.GetDirectoryName(location);
            return dir;
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

        public static void LogException(Exception exception)
        {
            string temp_path = Directory.GetParent(Path.GetTempPath()).Parent.FullName;
            string path = Path.Combine(temp_path, "AddinDiritLog", "log_exceptions.txt");
            AppendToFile(path, GetExceptionData(exception));
        }

        public static void LogException(string content)
        {
            string temp_path = Directory.GetParent(Path.GetTempPath()).Parent.FullName;
            string path = Path.Combine(temp_path, "AddinDiritLog", "log_exceptions.txt");
            AppendToFile(path, content);
        }
    }
}
