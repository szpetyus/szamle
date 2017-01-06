using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Threading;

namespace szamle
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const string absoluteUriError = "Abszolút címet kell megadni, pl: http://www.microsoft.com/";
        private const string usernameempty = "A bejelentkezéshez szükség lesz a felhasználó névre";
        private const string passwordempy = "A bejelentkezéshez szükség lesz a jelszóra";
        public static string mask_szolgaltato = "$szolgáltató";
        public static string mask_ev = "$év";
        public static string mask_issuer = "$kibocsátó";
        private const string statusdownload = "Letöltés..";
        private const String searchTabUrl = "https://www.dijnet.hu/ekonto/control/szamla_search";
        private const String logonUrl = "https://www.dijnet.hu/ekonto/login/login_check_password";
        private const String invDownloadTabUrl = "https://www.dijnet.hu/ekonto/control/szamla_letolt#tab_szamla_letolt";
        public static InvoiceIpc invoiceIpc;
        private List<String> filesDone = new List<string>();
        private int subRow = 0;
        private volatile dlStatus status = dlStatus.zero;
        private volatile dlStatus subStatus = dlStatus.zero;
        private volatile int rowNum = 0;
        private String fileNameMask { get; set; }
        private String _directoryMask;
        private bool _isProvider;
        private bool _isYear;
        private bool _isIssuer;

        public String directoryMask {
            get
            {
                return _directoryMask;
            }
            set
            {
                _directoryMask = value;
                validateDirectoryMask();
            }
        }

        public bool isProvider
        {
            get
            {
                return _isProvider;
            }
            set
            {
                if (!_isProvider)
                {
                    _directoryMask = String.Format(@"{0}\{1}\",directoryMask,mask_szolgaltato);
                }
                else
                {
                    _directoryMask = _directoryMask.Replace(mask_szolgaltato, "");
                }
                validateDirectoryMask();
            }
        }

        public bool isYear
        {
            get
            {
                return _isYear;
            }
            set
            {
                if (!_isYear)
                {
                    _directoryMask = String.Format(@"{0}\{1}\", directoryMask, mask_ev);
                }
                else
                {
                    _directoryMask = _directoryMask.Replace(mask_ev, "");
                }
                validateDirectoryMask();
            }
        }

        public bool isIssuer
        {
            get
            {
                return _isIssuer;
            }
            set
            {
                if (!_isIssuer)
                {
                    _directoryMask = String.Format(@"{0}\{1}\", directoryMask, mask_issuer);
                }
                else
                {
                    _directoryMask = _directoryMask.Replace(mask_issuer, "");
                }
                validateDirectoryMask();
            }
        }

        private void validateDirectoryMask()
        {
            _directoryMask = _directoryMask.Replace(@"\\", @"\");
            _isYear = _directoryMask.Contains(mask_ev);
            _isProvider = _directoryMask.Contains(mask_szolgaltato);
            _isIssuer = _directoryMask.Contains(mask_issuer);
            OnPropertyChanged("isProvider");
            OnPropertyChanged("isYear");
            OnPropertyChanged("isIssuer");
            OnPropertyChanged("directoryMask");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(String name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public MainWindow()
        {
            //var settings = new CefSharp.CefSettings { RemoteDebuggingPort = 8088 };
            //CefSharp.Cef.Initialize(settings);
            InitializeComponent();

            DataContext = this;
            string pathUser = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string pathDownload = System.IO.Path.Combine(pathUser, "Szamle");
            
            if (invoiceIpc != null)
            {
                MessageBox.Show("Csak egy példányban futtatható a program!\n Ha nem látsz másik ugyanilyen ablakot, indítsd újra a gépet");
            }
            invoiceIpc = new InvoiceIpc();
            this.webBrowser.RegisterJsObject("szamleIpc", invoiceIpc);
            if (String.IsNullOrWhiteSpace(Properties.Settings.Default.DirectoryMask))
            {
                directoryMask = pathDownload;
            }
            else
            {
                directoryMask = Properties.Settings.Default.DirectoryMask;
            }
            this.webBrowser.LoadingStateChanged += WebBrowser_LoadingStatusChanged;
            this.webBrowser.LifeSpanHandler = new szamle.LifeSpanHandler();
        }

        private void Url_Open_CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (Properties.Settings.Default.StartUrl == null)
            {
                e.CanExecute = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.UserName))
            {
                statustext.Text = usernameempty;
                e.CanExecute = false;
            }

            if (pwd == null || string.IsNullOrWhiteSpace(pwd.Password))
            {
                if (statustext != null) statustext.Text = passwordempy;
                e.CanExecute = false;
            }
            e.CanExecute = true;
        }

        private void Url_Open_CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                this.RajtaGomb.IsEnabled = false;
                user.IsEnabled = false;
                pwd.IsEnabled = false;
                dlpath.IsEnabled = false;
                pProvider.IsEnabled = false;
                pIssuer.IsEnabled = false;
                pYear.IsEnabled = false;
                this.webBrowser.DownloadHandler = new DownloadHandler(directoryMask, this.webBrowser, Properties.Settings.Default.OverwriteFiles);
                busy(true);
                //if (!System.IO.Directory.Exists(directoryMask)) System.IO.Directory.CreateDirectory(directoryMask);
                status = dlStatus.home;
                this.webBrowser.Load(Properties.Settings.Default.StartUrl);
            }
            catch (Exception ex)
            {
                statustext.Text = ex.Message;
            }
        }

        private void busy(bool busyState)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                Mouse.OverrideCursor = busyState ? Cursors.Wait : null;
            }));
        }

        private void sendLogon()
        {
            Application.Current.Dispatcher.Invoke(new Action(() => { 
                webBrowser.GetBrowser().MainFrame.EvaluateScriptAsync(@"
                    document.getElementsByName('loginform')[0].username.value='" + Properties.Settings.Default.UserName + @"';
                    document.getElementsByName('loginform')[0].password.value='" + pwd.Password + @"';
                    document.getElementsByName('loginform')[0].submit();");
                status = dlStatus.logon;
            }));
        }

        private void WebBrowser_LoadingStatusChanged(object sender, CefSharp.LoadingStateChangedEventArgs e)
        {
            if(!e.IsLoading)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    statustext.Text = String.Format("{0} {1} {2}", webBrowser.Address, status, subStatus);
                }));
                switch (status)
                {
                    case dlStatus.zero:
                        break;
                    case dlStatus.home:
                        sendLogon();
                        break;
                    case dlStatus.logon:
                        searchTab();
                        break;
                    case dlStatus.search:
                        searchInvoices();
                        break;
                    case dlStatus.found:
                        switch (subStatus)
                        {
                            case dlStatus.zero:
                                collectInvoices(); // azután pegig új szálon letöltés
                                break;
                            case dlStatus.search:
                                clickOnJs("Letöltés");
                                subStatus = dlStatus.found;
                                subRow = 0;
                                Application.Current.Dispatcher.Invoke(new Action(() =>
                                {
                                    statustext.Text = String.Format("Letöltés alatt: {0}", invoiceIpc.invoices[rowNum].fileNameMask);
                                }));
                                break;
                            case dlStatus.found:
                                Application.Current.Dispatcher.Invoke(new Action(() =>
                                {
                                    if (!invDownloadTabUrl.Equals(webBrowser.Address))
                                    {
                                        subStatus = dlStatus.search;
                                        return;
                                    }
                                    webBrowser.GetBrowser().MainFrame.EvaluateScriptAsync(@"
                                    tdata = document.getElementById('content').children[2].children[1].children[0].children[0].children[1].children[0].children[0].children[0].children[0];
                                    if(row=tdata.rows[" + subRow++ + @"]) {
                                        if (row && row.children[1] && row.children[1].children[0] && row.children[1].children[0].href)
                                        {
                                            href = row.children[1].children[0].href;
                                            // Dwonload: szamla_pdf, szamla_teho_pdf, teho_all_pdf, szamla_hiteles
                                            if(href.indexOf('szamla_pdf') != -1 || href.indexOf('szamla_teho_pdf') != -1 || href.indexOf('teho_all_pdf') != -1 || href.indexOf('szamla_hiteles') != -1 ) 
                                            {
                                                row.children[1].children[0].click();
                                                console.info('row ' + " + subRow + @");
                                            }
                                            else // Skip: minden más
                                            {
                                                console.info('skip row ' + " + subRow + @");
                                                " + mkAnchorIteratorJs("Letöltés") + @"
                                            }
                                            //TODO: popup ablak kezelése és letöltés  if(row.children[1].children[0].href.indexOf('szamla_xml') != -1) {row.children[1].children[0].click(); alert('2');}
                                        } 
                                        else
                                        {
                                            // trigger the LoadingStatusChanged event from JS
                                            console.info('skip row ' + " + subRow + @");
                                            " + mkAnchorIteratorJs("Letöltés") + @"
                                        }
                                    }
                                    else
                                    {
                                        // trigger the LoadingStatusChanged event from JS
                                        console.info('skip row ' + " + subRow + @");
                                        " + mkAnchorIteratorJs("Letöltés") + @"
                                    }");
                                    if (subRow>9)
                                    {
                                        clickOnJs(" vissza a listához");
                                        subStatus = dlStatus.download;
                                        subRow = 0;
                                    }
                                    Thread.Yield();
                                }));
                                Thread.Yield();
                                break;
                            case dlStatus.download:
                                Application.Current.Dispatcher.Invoke(new Action(() =>
                                {
                                    if (invDownloadTabUrl.Equals(webBrowser.Address))
                                    {
                                        Thread.Yield();
                                        Thread.Sleep(2000);
                                        Thread.Yield();
                                    }
                                    else
                                    {
                                        subStatus = dlStatus.logoff;
                                    }
                                }));
                                break;
                            case dlStatus.logoff:
                                break;
                        }
                        //collectInvoices();
                        break;
                }

            }
        }

        private void searchTab()
        {
            clickOnJs("Számlakeresés");
            status = dlStatus.search;
        }

        private void searchInvoices()
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (!searchTabUrl.Equals(webBrowser.Address))
                {
                    statustext.Text = "Akadály, a kereső oldalra navigáláskor. Újra próbálom";
                    searchTab();
                    return;
                }
                webBrowser.GetBrowser().MainFrame.ExecuteJavaScriptAsync("document.forms[0].submit();");
                status = dlStatus.found;
            }));
        }

        private void collectInvoices()
        {
            //szamlák -> dictionary úgymint row: url, szolgáltató, kibocsátó, számlaszám, kibocsátás (bemutatás?) dátuma, összeg, fizetési határidő, fizetendő, állapot
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                statustext.Text = String.Format("Számlák összegyűjtése...");
                webBrowser.GetBrowser().MainFrame.EvaluateScriptAsync(@"
                list_rows = document.getElementById('content').children[4].children[0].children[1].children;
                r = 0;
                while(row=list_rows[r++]) {
                    szamleIpc.addNewInvoice(row.children[0].children[0].href, row.children[0].children[0].text, 
                    row.children[1].children[0].text, row.children[2].children[0].text, row.children[3].children[0].text, 
                    row.children[4].children[0].text, row.children[5].children[0].text, row.children[6].children[0].text, 
                    row.children[7].children[0].text);
                }").ContinueWith((t) => downloadInvoicesWorker());
            }));
        }

        //Számlakeresés
        //Letöltés
        // vissza a listához
        protected void clickOnJs(String anchorText)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                webBrowser.GetBrowser().MainFrame.EvaluateScriptAsync(mkAnchorIteratorJs(anchorText));
            }));
        }

        protected String mkAnchorIteratorJs(String anchorText)
        {
            return String.Format("linkz = document.getElementsByTagName('a'); for(i=0;i<linkz.length;i++) if ('{0}'==linkz[i].innerText) linkz[i].click();", anchorText);
        }

        private void downloadInvoicesWorker()
        {
            rowNum = 0;
            foreach (Invoice invoice in invoiceIpc.invoices)
            {
                subStatus = dlStatus.search;
                fileNameMask = invoice.fileNameMask;
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    webBrowser.GetBrowser().MainFrame.EvaluateScriptAsync(
                        "try {document.getElementById('content').children[4].children[0].children[1].children[" + rowNum +
                        "].children[0].children[0].click();} catch (err) {alert('eltévedtünk, a kereső oldalon kellene lenni');}");

                }));
                int timeout = 30;
                while (subStatus != dlStatus.logoff)
                {
                    Thread.Yield();
                    Thread.Sleep(1000);
                    Thread.Yield();
                    if (timeout-- == 0)
                    {
                        Application.Current.Dispatcher.Invoke(new Action(() => { webBrowser.GetBrowser().Reload(); }));
                        timeout = 30;
                    }
                }
                rowNum++;
            }
            status = dlStatus.logoff;
            busy(false);
            clickOnJs("Kilépés");
            MessageBox.Show("Ennyi volt");
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //webBrowser = null;
            webBrowser.GetBrowser().StopLoad();
            webBrowser.Dispose();
            Properties.Settings.Default.DirectoryMask = _directoryMask;
            Properties.Settings.Default.Save();
            Application.Current.Shutdown();
        }
    }
}
