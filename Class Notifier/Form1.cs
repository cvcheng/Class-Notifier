using System;
using System.Windows.Forms;
//using CefSharp.WinForms;
using HtmlAgilityPack;
using System.Drawing;
using System.Threading;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace Class_Notifier
{
    public partial class Form1 : Form
    {
        //private readonly ChromiumWebBrowser browser;
        private NotifyIcon trayIcon = new NotifyIcon();
        private ContextMenu trayMenu = new ContextMenu();
        private MenuItem[] menuItems = new MenuItem[4];

        public Form1()
        {
            InitializeComponent();

            this.ControlBox = false;
            webBrowser1.ScriptErrorsSuppressed = true;

            menuItems[0] = new MenuItem("Start");
            menuItems[0].Click += new EventHandler(startClick);
            menuItems[0].Enabled = false;

            menuItems[1] = new MenuItem("Settings");
            menuItems[1].Click += new EventHandler(showForm);

            menuItems[2] = new MenuItem("-");

            menuItems[3] = new MenuItem("Exit");
            menuItems[3].Click += new EventHandler(exitClick);

            trayMenu.MenuItems.AddRange(menuItems);

            trayIcon.Text = "Class Notifier";
            trayIcon.Icon = new Icon(Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location), 40, 40);

            // Add menu to tray icon and show it.
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;

            if (Properties.Settings.Default.username.Equals("") || Properties.Settings.Default.password.Equals(""))
            {
                this.Show();
                ShowInTaskbar = true;
            }
            else
            {
                textBox1.Text = StringCipher.Decrypt(Properties.Settings.Default.username, "KiUIUmwJv");
                textBox2.Text = StringCipher.Decrypt(Properties.Settings.Default.password, "F6UaCtrVz");
                menuItems[0].Enabled = true;
            }
            textBox3.Text = Properties.Settings.Default.interval.ToString();
            checkBox1.Checked = Properties.Settings.Default.popup;
            checkBox3.Checked = Properties.Settings.Default.autoenroll;

            /*browser = new ChromiumWebBrowser("")
            {
                Dock = DockStyle.Fill,
            };
            toolStripContainer1.ContentPanel.Controls.Add(browser);
            browser.FrameLoadEnd += delegate { OnLoadCompleted(); };*/
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        private void startClick(object sender, EventArgs e)
        {
            if (menuItems[0].Text.Equals("Start"))
            {
                menuItems[0].Text = "Stop";
                trayIcon.ShowBalloonTip(1000, "Class Notifier", "Logging into CUNYfirst...", ToolTipIcon.Info);
                timer1.Interval = Convert.ToInt32(textBox3.Text) * 1000;
                timer1.Enabled = true;
                menuItems[1].Enabled = false;
                webBrowser1.Navigate("https://impweb.cuny.edu/oam/Portal_Login1.html");
            }
            else
            {
                menuItems[0].Text = "Start";
                menuItems[1].Enabled = true;
                timer1.Enabled = false;
            }
        }

        private void showForm(object sender, EventArgs e)
        {
            menuItems[0].Enabled = false;
            ShowInTaskbar = true;
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.username))
                textBox1.Text = StringCipher.Decrypt(Properties.Settings.Default.username, "KiUIUmwJv");
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.password))
                textBox2.Text = StringCipher.Decrypt(Properties.Settings.Default.password, "F6UaCtrVz");
            textBox3.Text = Properties.Settings.Default.interval.ToString();
            checkBox1.Checked = Properties.Settings.Default.popup;
            checkBox3.Checked = Properties.Settings.Default.autoenroll;
            this.Show();
            this.BringToFront();
        }

        private void exitClick(object sender, EventArgs e)
        {
            trayIcon.Dispose();
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int parsedValue;
            if (string.IsNullOrWhiteSpace(textBox1.Text) || string.IsNullOrWhiteSpace(textBox2.Text))
            {
                MessageBox.Show("Username or password field is empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else if (!int.TryParse(textBox3.Text, out parsedValue))
            {
                MessageBox.Show("Interval is not a number.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else {
                Properties.Settings.Default["username"] = StringCipher.Encrypt(textBox1.Text, "KiUIUmwJv");
                Properties.Settings.Default["password"] = StringCipher.Encrypt(textBox2.Text, "F6UaCtrVz");
                Properties.Settings.Default["interval"] = Int32.Parse(textBox3.Text);
                Properties.Settings.Default["popup"] = checkBox1.Checked;
                Properties.Settings.Default["autoenroll"] = checkBox3.Checked;
                Properties.Settings.Default.Save();
                menuItems[0].Enabled = true;
                this.Visible = false;
                //browser.Load("https://impweb.cuny.edu/oam/Portal_Login1.html");
            }
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (e.Url.AbsolutePath != (sender as WebBrowser).Url.AbsolutePath)
                return;
            if (menuItems[0].Text.Equals("Stop"))
            {
                if (webBrowser1.Url.ToString() == "https://impweb.cuny.edu/oam/Portal_Login1.html")
                {
                    System.Windows.Forms.HtmlDocument source = webBrowser1.Document;
                    HtmlElement head = source.GetElementsByTagName("head")[0];
                    HtmlElement s = source.CreateElement("script");
                    s.SetAttribute("text", "document.getElementById('login').value = '" + textBox1.Text + "'; void(0);");
                    head.AppendChild(s);
                    s.SetAttribute("text", "document.getElementById('password').value = '" + textBox2.Text + "'; void(0);");
                    head.AppendChild(s);
                    s.SetAttribute("text", "document.getElementsByName(\"submit\")[0].click();");
                    head.AppendChild(s);
                }
                else if (webBrowser1.Url.ToString().Contains("https://impweb.cuny.edu/oam/InvalidLogin.html"))
                {
                    trayIcon.ShowBalloonTip(1000, "Class Notifier", "Invalid login. Please check your username/password.", ToolTipIcon.Error);
                    timer1.Enabled = false;
                    menuItems[0].Text = "Start";
                    menuItems[1].Enabled = true;
                }
                else if (webBrowser1.Url.ToString() == "https://impweb.cuny.edu/xlWebApp/")
                {
                    trayIcon.ShowBalloonTip(1000, "Class Notifier", "Successfully logged in. Will now run in the background and check for classes every " + textBox3.Text + " seconds.", ToolTipIcon.Info);
                    webBrowser1.Navigate("https://hrsa.cunyfirst.cuny.edu/psc/cnyhcprd/EMPLOYEE/HRMS/c/SA_LEARNER_SERVICES.SSR_SSENRL_CART.GBL?");
                }
                else if (webBrowser1.Url.ToString() == "https://hrsa.cunyfirst.cuny.edu/psc/cnyhcprd/EMPLOYEE/HRMS/c/SA_LEARNER_SERVICES.SSR_SSENRL_CART.GBL?&")
                {
                    HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                    string source = webBrowser1.DocumentText;
                    htmlDoc.LoadHtml(source);
                    var pageTitle = htmlDoc.GetElementbyId("DERIVED_REGFRM1_SS_TRANSACT_TITLE");
                    if (pageTitle != null)
                    {
                        if (pageTitle.InnerText == "Add Classes")
                        {
                            checkClasses();
                            timer1.Enabled = true;
                        }
                    }
                    else
                    {
                        System.Windows.Forms.HtmlDocument source1 = webBrowser1.Document;
                        HtmlElement head = source1.GetElementsByTagName("head")[0];
                        HtmlElement s = source1.CreateElement("script");
                        s.SetAttribute("text", "var buttons = document.getElementsByName(\"SSR_DUMMY_RECV1$sels$0\"); buttons[1].checked = true; void(0);");
                        head.AppendChild(s);
                        s.SetAttribute("text", "submitAction_win0(document.win0, 'DERIVED_SSS_SCT_SSR_PB_GO');");
                        head.AppendChild(s);
                    }
                }
                //confirm classes
                if (webBrowser1.Url.ToString().Contains("https://hrsa.cunyfirst.cuny.edu/psc/cnyhcprd/EMPLOYEE/HRMS/c/SA_LEARNER_SERVICES.SSR_SSENRL_ADD.GBL?Page=SSR_SSENRL_ADD_C"))
                    webBrowser1.Navigate("javascript: document.getElementsByName(\"DERIVED_REGFRM1_SSR_PB_SUBMIT\")[0].click();");
                //finished enrolling
                if (webBrowser1.Url.ToString() == "https://hrsa.cunyfirst.cuny.edu/psc/cnyhcprd/EMPLOYEE/HRMS/c/SA_LEARNER_SERVICES.SSR_SSENRL_ADD.GBL")
                    trayIcon.ShowBalloonTip(1000, "Class Notifier", "Finished enrolling. Will continue checking for other classes to open.", ToolTipIcon.Info);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            webBrowser1.Navigate("https://hrsa.cunyfirst.cuny.edu/psc/cnyhcprd/EMPLOYEE/HRMS/c/SA_LEARNER_SERVICES.SSR_SSENRL_CART.GBL?&");
        }

        public void checkClasses()
        {
            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            string source = webBrowser1.DocumentText;
            htmlDoc.LoadHtml(source);
            /*foreach (HtmlNode node in htmlDoc.DocumentNode.SelectNodes("//img"))
            {
                if (node.Attributes["class"] != null)
                {
                    if (node.Attributes["class"].Value == "SSSIMAGECENTER")
                    {
                        if (node.Attributes["alt"].Value == "Open")
                            //MessageBox.Show(node.Attributes["alt"].Value);
                            MessageBox.Show("Class opened!");
                        else
                            webBrowser1.Navigate("https://hrsa.cunyfirst.cuny.edu/psc/cnyhcprd/EMPLOYEE/HRMS/c/SA_LEARNER_SERVICES.SSR_SSENRL_CART.GBL?&");
                    }
                }
            }*/
            int classNum = 0;
            int classesOpen = 0;
            string classes = "";
            int classesInCart = 0;
            foreach (HtmlNode node in htmlDoc.DocumentNode.SelectNodes("//div[starts-with(@id, 'win0divDERIVED_REGFRM1_SSR_STATUS_LONG$')]//img//@src"))
            {
                if (node.Attributes["alt"].Value == "Open")
                {
                    classesOpen++;
                    classes += htmlDoc.GetElementbyId("win0divP_CLASS_NAME$" + classNum).InnerText;
                }
                if (node.Attributes["alt"].Value == "Open" || node.Attributes["alt"].Value == "Closed" || node.Attributes["alt"].Value == "Wait List")
                    classesInCart++;
            }
            if (classesInCart == 0)
            {
                trayIcon.ShowBalloonTip(1000, "Class Notifier", "No classes found in cart, stopping program.", ToolTipIcon.Info);
                timer1.Enabled = false;
                menuItems[0].Text = "Start";
                menuItems[1].Enabled = true;
            }
            if (classesOpen != 0)
            {
                if (checkBox3.Checked)
                {
                    trayIcon.ShowBalloonTip(1000, "Class Notifier", classesOpen + " class(es) are open! Enrolling all classes...", ToolTipIcon.Info);
                    webBrowser1.Navigate("javascript: document.getElementsByName(\"DERIVED_REGFRM1_LINK_ADD_ENRL$113$\")[0].click();");
                }
                else
                    trayIcon.ShowBalloonTip(1000, classesOpen + " class(es) are open!", classes, ToolTipIcon.Info);
                if (checkBox1.Checked)
                    MessageBox.Show("Class(es) are open!");
            }
        }

        public void ShowMessageBox()
        {
            var thread = new Thread(
              () =>
              {
                  MessageBox.Show("Opened!!!!");
              });
            thread.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.username))
                textBox1.Text = StringCipher.Decrypt(Properties.Settings.Default.username, "KiUIUmwJv");
            else
                textBox1.Text = "";
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.password))
                textBox2.Text = StringCipher.Decrypt(Properties.Settings.Default.password, "F6UaCtrVz");
            else
                textBox2.Text = "";
            textBox3.Text = Properties.Settings.Default.interval.ToString();
            checkBox1.Checked = Properties.Settings.Default.popup;
            checkBox3.Checked = Properties.Settings.Default.autoenroll;
            if (textBox1.Text != "" && textBox2.Text != "" && textBox3.Text != "")
                menuItems[0].Enabled = true;
            this.Visible = false;
        }

        public static class StringCipher
        {
            // This constant string is used as a "salt" value for the PasswordDeriveBytes function calls.
            // This size of the IV (in bytes) must = (keysize / 8).  Default keysize is 256, so the IV must be
            // 32 bytes long.  Using a 16 character string here gives us 32 bytes when converted to a byte array.
            private static readonly byte[] initVectorBytes = Encoding.ASCII.GetBytes("tu89geji340t89u2");

            // This constant is used to determine the keysize of the encryption algorithm.
            private const int keysize = 256;

            public static string Encrypt(string plainText, string passPhrase)
            {
                byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                using (PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null))
                {
                    byte[] keyBytes = password.GetBytes(keysize / 8);
                    using (RijndaelManaged symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.Mode = CipherMode.CBC;
                        using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes))
                        {
                            using (MemoryStream memoryStream = new MemoryStream())
                            {
                                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                                {
                                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                    cryptoStream.FlushFinalBlock();
                                    byte[] cipherTextBytes = memoryStream.ToArray();
                                    return Convert.ToBase64String(cipherTextBytes);
                                }
                            }
                        }
                    }
                }
            }

            public static string Decrypt(string cipherText, string passPhrase)
            {
                byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
                using (PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null))
                {
                    byte[] keyBytes = password.GetBytes(keysize / 8);
                    using (RijndaelManaged symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.Mode = CipherMode.CBC;
                        using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes))
                        {
                            using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
                            {
                                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                                {
                                    byte[] plainTextBytes = new byte[cipherTextBytes.Length];
                                    int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                    return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                                }
                            }
                        }
                    }
                }
            }
        }

        /*public async void OnLoadCompleted()
        {
            if (browser.Address == "https://impweb.cuny.edu/oam/Portal_Login1.html")
            {
                browser.ExecuteScriptAsync("document.getElementById('login').value = '" + textBox1.Text + "'; void(0);");
                browser.ExecuteScriptAsync("document.getElementById('password').value = '" + textBox2.Text + "'; void(0);");
                browser.ExecuteScriptAsync("document.getElementsByName(\"submit\")[0].click();");
            }
            else if (browser.Address == "https://impweb.cuny.edu/xlWebApp/")
            {
                browser.Load("https://hrsa.cunyfirst.cuny.edu/psc/cnyhcprd/EMPLOYEE/HRMS/c/SA_LEARNER_SERVICES.SSR_SSENRL_CART.GBL?");
            }
            else if (browser.Address == "https://hrsa.cunyfirst.cuny.edu/psc/cnyhcprd/EMPLOYEE/HRMS/c/SA_LEARNER_SERVICES.SSR_SSENRL_CART.GBL?&")
            {
                browser.ExecuteScriptAsync("var buttons = document.getElementsByName(\"SSR_DUMMY_RECV1$sels$0\"); buttons[1].checked = true; void(0);");
                browser.ExecuteScriptAsync("submitAction_win0(document.win0, 'DERIVED_SSS_SCT_SSR_PB_GO');");
                MessageBox.Show("loaded this");
                string source = await browser.GetSourceAsync();
                HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(source);
                foreach (HtmlNode node in htmlDoc.DocumentNode.SelectNodes("//img"))
                {
                    if (node.Attributes["class"] != null)
                    {
                        if (node.Attributes["class"].Value == "SSSIMAGECENTER")
                            //if (node.Attributes["alt"].Value == "Open")
                            MessageBox.Show(node.Attributes["alt"].Value);
                    }
                }
            }
            else if (browser.Address.Contains("https://hrsa.cunyfirst.cuny.edu/psc/cnyhcprd/EMPLOYEE/HRMS/c/SA_LEARNER_SERVICES.SSR_SSENRL_CART.GBL?Page=SSR_SSENRL_CART"))
            {
                MessageBox.Show("loaded");
                string source = await browser.GetSourceAsync();
                HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(source);
                foreach (HtmlNode node in htmlDoc.DocumentNode.SelectNodes("//img"))
                {
                    if (node.Attributes["class"] != null)
                    {
                        if (node.Attributes["class"].Value == "SSSIMAGECENTER")
                            //if (node.Attributes["alt"].Value == "Open")
                                MessageBox.Show(node.Attributes["alt"].Value);
                    }
                }
            }
        }

        /*public void OnFrameLoadEnd(string url, bool isMainFrame)
        {
            if (browser.Address == "https://impweb.cuny.edu/oam/Portal_Login1.html")
                MessageBox.Show("", "Error Title", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }*/
    }
}

