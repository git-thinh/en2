using System;
using System.Windows.Forms;
using Skybound.Gecko;
using System.Runtime.InteropServices;
using System.Net;
using System.Reflection;
using System.IO;

// Tested with mono 2.6.3 and mono 2.8
// Run this with the following command:
// MONO_PATH=/usr/lib/cli/gdk-sharp-2.0/ LD_LIBRARY_PATH="/usr/lib/xulrunner-1.9.2.13/" mono --debug GeckoFxTest.exe
// requires gdk-sharp assembly in the gac (which is in package libgtk2.0-cil)
namespace en2
{
    class App
    {

        static App()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (se, ev) =>
            {
                Assembly asm = null;
                string comName = ev.Name.Split(',')[0];

                string resourceName = @"DLL\" + comName + ".dll";
                var assembly = Assembly.GetExecutingAssembly();
                resourceName = typeof(App).Namespace + "." + resourceName.Replace(" ", "_").Replace("\\", ".").Replace("/", ".");
                //using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (Stream stream = File.OpenRead("bin/" + comName + ".dll"))
                {
                    if (stream == null)
                    {
                        //Debug.WriteLine(resourceName);
                    }
                    else
                    {
                        byte[] buffer = new byte[stream.Length];
                        using (MemoryStream ms = new MemoryStream())
                        {
                            int read;
                            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                                ms.Write(buffer, 0, read);
                            buffer = ms.ToArray();
                        }
                        asm = Assembly.Load(buffer);
                    }
                }
                return asm;
            };
        }

        [STAThread]
        public static void Main(string[] args)
        {
            //System.Net.ServicePointManager.DefaultConnectionLimit = 1000;
            //// active SSL 1.1, 1.2, 1.3 for WebClient request HTTPS
            //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | (SecurityProtocolType)3072 | (SecurityProtocolType)0x00000C00 | SecurityProtocolType.Tls;

            //Xpcom.Initialize(@"D:\temp\xulrunner");
            Xpcom.Initialize(@"Bin");

            Application.Run(new MyForm());
        }
    }

    class MyForm : Form
    {
        private TabControl m_tabControl;

        public MyForm()
        {
            this.Width = 800;
            this.Height = 600;

            m_tabControl = new TabControl();
            m_tabControl.Dock = DockStyle.Fill;

            AddTab();

            Controls.Add(m_tabControl);

            m_tabControl.ControlRemoved += delegate {
                if (m_tabControl.TabCount == 0)
                {
                    AddTab();
                }
            };

        }

        protected void ModifyElements(GeckoElement element, string tagName, Action<GeckoElement> mod)
        {
            while (element != null)
            {
                if (element.TagName == tagName)
                {
                    mod(element);
                }
                ModifyElements(element.FirstChild as GeckoElement, tagName, mod);
                element = (element.NextSibling as GeckoElement);
            }
        }

        protected void TestModifyingDom(GeckoWebBrowser browser)
        {
            GeckoElement g = browser.Document.DocumentElement;
            ModifyElements(g, "BODY", e =>
            {
                for (int i = 1; i < 4; ++i)
                {
                    var newElement = g.OwnerDocument.CreateElement(String.Format("h{0}", i));
                    newElement.TextContent = "Geckofx added this text.";
                    g.InsertBefore(newElement, e);
                }
            });
        }

        protected void DisplayElements(GeckoElement g)
        {
            while (g != null)
            {
                Console.WriteLine("tag = {0} value = {1}", g.TagName, g.TextContent);
                DisplayElements(g.FirstChild as GeckoElement);
                g = (g.NextSibling as GeckoElement);
            }

        }

        protected void TestQueryingOfDom(GeckoWebBrowser browser)
        {
            GeckoElement g = browser.Document.DocumentElement;
            DisplayElements(g);
        }

        protected void AddTab()
        {
            var tabPage = new TabPage();
            tabPage.Text = "blank";
            var browser = new GeckoWebBrowser();
            browser.Dock = DockStyle.Fill;

            tabPage.DockPadding.Top = 25;
            tabPage.Dock = DockStyle.Fill;

            // add a handler showing how to view the DOM
            //browser.DocumentCompleted += (s, e) => TestQueryingOfDom(browser);

            // add a handler showing how to modify the DOM.
            //browser.DocumentCompleted += (s, e) => TestModifyingDom(browser);

            AddToolbarAndBrowserToTab(tabPage, browser);

            m_tabControl.TabPages.Add(tabPage);
            tabPage.Show();
            m_tabControl.SelectedTab = tabPage;

            // Uncomment this to stop links from navigating.
            // browser.DomClick += StopLinksNavigating;

        }

        /// <summary>
        /// An example event handler for the DomClick event.
        /// Prevents a link click from navigating.
        /// </summary>
        void StopLinksNavigating(object sender, GeckoDomEventArgs e)
        {
            if (sender != null && e != null && e.Target != null && e.Target.TagName != null)
            {
                GeckoElement clicked = e.Target;

                // prevent clicking on Links from navigation to the
                if (clicked.TagName == "A")
                {
                    e.Handled = true;
                    MessageBox.Show(sender as IWin32Window, String.Format("You clicked on Link {0}", clicked.GetAttribute("href")));
                }

            }
        }

        protected void AddToolbarAndBrowserToTab(TabPage tabPage, GeckoWebBrowser browser)
        {
            TextBox urlbox = new TextBox();
            urlbox.Top = 0;
            urlbox.Width = 200;

            Button nav = new Button();
            nav.Text = "Go";
            nav.Left = 200;

            Button newTab = new Button();
            newTab.Text = "NewTab";
            newTab.Left = 200 + nav.Width;

            Button closeTab = new Button();
            closeTab.Text = "Close";
            closeTab.Left = 200 + nav.Width + newTab.Width;

            nav.Click += delegate {
                // use javascript to warn if url box is empty.
                if (string.IsNullOrEmpty(urlbox.Text.Trim()))
                    browser.Navigate("javascript:alert('hey try typing a url!');");

                browser.Navigate(urlbox.Text);
                tabPage.Text = urlbox.Text;
            };

            newTab.Click += delegate { AddTab(); };

            closeTab.Click += delegate {
                m_tabControl.Controls.Remove(tabPage);
                browser.Dispose();
            };

            tabPage.Controls.Add(urlbox);
            tabPage.Controls.Add(nav);
            tabPage.Controls.Add(newTab);
            tabPage.Controls.Add(closeTab);
            tabPage.Controls.Add(browser);
        }
    }
}
