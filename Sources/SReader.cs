namespace PornHub_Checker
{
    using System.Collections.Generic;
    using System.IO;
    using System.Windows.Forms;

    class SReader
    {
        public static string path = null;
        public static List<string> list = new List<string>();

        public static int ACount()
        {
            int i = 0;
            try
            {
                if (path != null)
                {
                    using (StreamReader sr = new StreamReader(path))
                    {
                        string line;

                        while ((line = sr.ReadLine()) != null)
                        {
                            list.Add(line);
                            i+= 1;
                        }
                        return i;
                    }                   
                }
                else
                {
                    MessageBox.Show("Please, upload base!");
                    return 0;
                }
            }
            catch
            {
                return 0;
            }
        }
    }
}
