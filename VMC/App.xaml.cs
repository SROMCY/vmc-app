using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using VMC.Misc;

namespace VMC
{
    public partial class App : Application
    {
        private readonly Mutex _mutex;
        public App()
        {
            // Try to grab mutex
            _mutex = new Mutex(true, this.GetType().GUID.ToString(), out bool createdNew);

            if (createdNew)
            {
                // Add Event handler to exit event.
                Exit += CloseMutexHandler;
            }
            else
            {
                MessageBox.Show("Application is already running");
                _mutex.Close();
                Application.Current.Shutdown();
            }
        }

        // Handler that closes the mutex.
        protected virtual void CloseMutexHandler(object sender, EventArgs e)
        {
            _mutex?.Close();
        }

        public static string GetFolder(string key)
        {
            // read application settings
            NameValueCollection folderStr = ConfigurationManager.GetSection("directory") as NameValueCollection;
            return folderStr.Get(key);
        }
    }
}