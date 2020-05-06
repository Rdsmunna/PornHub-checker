namespace PornHub_Checker
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;

    class Helper
    {
        public static string Trim(string data) //normalize string
        {
            Regex regex = new Regex("[А-Яа-я]");

            string ShortResult = data.Trim(new Char[] { ' ', '*', '%', '^', '&', '?', ',', '<', '>', '*', '(', ')', '+', '=', '®', '~', '!', '"', '\'', '|', '\\', '/', '№', '#', '$' });
            string result = regex.Replace(ShortResult, "");

            return result;
        }
        public static string Pars(string strSource, string strStart, string strEnd, int startPos = 0) //method for parsing answer
        {
            string result = string.Empty;
            try
            {
                int length = strStart.Length,
                    num = strSource.IndexOf(strStart, startPos),
                    num2 = strSource.IndexOf(strEnd, num + length);
                if (num != -1 & num2 != -1)
                    result = strSource.Substring(num + length, num2 - (num + length));
            }
            catch (Exception ex) { File.WriteAllText("ParsError.txt", ex.Message); }
            return result;
        }

        public static void LogSaveGood(string data) //save goods
        {
            File.AppendAllText(Application.StartupPath.ToString() + @"\Good.log", data);
        }

        public static void LogSavePremium(string data) //save premium accaunts
        {
            File.AppendAllText(Application.StartupPath.ToString() + @"\Premium.log", data); 
        }

        public static void LogSaveBad(string data) //save bads
        {
            File.AppendAllText(Application.StartupPath.ToString() + @"\Bad.log", data);
        }

        public static void LogSaveBadProxies(string data) //save bad connections
        {
            File.AppendAllText(Application.StartupPath.ToString() + @"\BadProxies.log", data);
        }
    }
}
