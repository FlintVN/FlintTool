using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Runtime.InteropServices;
using WinRT;
using FlintTool.Models;
using FlintTool.ViewModels;
using FlintTool.Views;
using FlintTool.Services;
using System.Net;
using System.IO.Ports;

namespace FlintTool {
    public sealed partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(appTitleBar);

            FlintClient flintClient = new FlintClient(new FlintUart("COM19", 921600));
            flintClient.Connect();
            flintClient.EnterDebugModeRequest();
            flintClient.TerminateRequest(false);

            fileViewer.Client = flintClient;
        }
    }
}
