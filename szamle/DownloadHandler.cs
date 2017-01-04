using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace szamle
{
    public class DownloadHandler : IDownloadHandler
    {
        public event EventHandler<DownloadItem> OnBeforeDownloadFired;
        public event EventHandler<DownloadItem> OnDownloadUpdatedFired;
        private String _defaultPath;
        private CefSharp.Wpf.ChromiumWebBrowser webBrowser;

        public DownloadHandler(String path, CefSharp.Wpf.ChromiumWebBrowser browser)
        {
            _defaultPath = path;
            webBrowser = browser;
        }

        public void OnLoadingStateChanged(CefSharp.LoadingStateChangedEventArgs e)
        {
            ILoadHandler handler = webBrowser.LoadHandler;
            if (handler != null)
            {
                handler.OnLoadingStateChange(webBrowser, e);
            }
        }

        public void OnBeforeDownload(IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
        {
            var handler = OnBeforeDownloadFired;
            if (handler != null)
            {
                handler(this, downloadItem);
            }

            if (!callback.IsDisposed)
            {
                String thisPath = _defaultPath;
                IEnumerable<Invoice> thisInvoice = MainWindow.invoiceIpc.invoices.Where(x => !String.IsNullOrWhiteSpace(x.norm_szamlaszam) && downloadItem.SuggestedFileName.Contains(x.norm_szamlaszam));
                if (thisInvoice != null && thisInvoice.Count()>0 && thisInvoice.First() != null)
                {
                    thisPath = legalisePath( _defaultPath.Replace(MainWindow.mask_ev, thisInvoice.First().kibocsatas.Substring(0, 4)).Replace(MainWindow.mask_szolgaltato, thisInvoice.First().szolgaltato));
                    System.IO.Directory.CreateDirectory(thisPath);
                }
                else
                {
                    thisPath = legalisePath( _defaultPath.Replace(MainWindow.mask_ev, "").Replace(MainWindow.mask_szolgaltato, "").Replace(@"\\", @"\"));
                    System.IO.Directory.CreateDirectory(thisPath);
                }
                using (callback)
                {
                    callback.Continue(System.IO.Path.Combine(thisPath, legaliseFileName( downloadItem.SuggestedFileName)), showDialog: false);
                }
            }
        }

        public void OnDownloadUpdated(IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
        {
            var handler = OnDownloadUpdatedFired;
            if (handler != null)
            {
                handler(this, downloadItem);
            }
            if (downloadItem!= null && browser != null && downloadItem.IsComplete)
            {
                CefSharp.LoadingStateChangedEventArgs e = new LoadingStateChangedEventArgs(browser, true, false, false);
                OnLoadingStateChanged(e);
            }
        }

        private String legalisePath(String path)
        {
            string regexSearch = new string(System.IO.Path.GetInvalidPathChars());
            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(string.Format("[{0}]", System.Text.RegularExpressions.Regex.Escape(regexSearch)));
            return r.Replace(path, "");
        }

        private String legaliseFileName(String filename)
        {
            string regexSearch = new string(System.IO.Path.GetInvalidPathChars());
            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(string.Format("[{0}]", System.Text.RegularExpressions.Regex.Escape(regexSearch)));
            return r.Replace(filename, "");
        }
    }
}
