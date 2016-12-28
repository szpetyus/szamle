using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                using (callback)
                {
                    callback.Continue(System.IO.Path.Combine(_defaultPath, downloadItem.SuggestedFileName), showDialog: false);
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
                //browser.Reload();
            }
        }
    }
}
