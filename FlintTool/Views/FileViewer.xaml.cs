using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using FlintTool.Models;
using FlintTool.ViewModels;
using FlintTool.Services;
using Windows.UI.Popups;
using CommunityToolkit.WinUI.UI.Controls;
using Windows.Media.Protection.PlayReady;
using System.Threading;
using System.Threading.Tasks;

namespace FlintTool.Views {
    public sealed partial class FileViewer : UserControl {
        private string directory;
        private FlintClient client;
        private ObservableCollection<FileInfoItemViewModel> filesItemViewModels;

        public FileViewer() {
            InitializeComponent();
            DataContext = this;
            filesItemViewModels = new ObservableCollection<FileInfoItemViewModel>();
            fileViewDataGrid.ItemsSource = filesItemViewModels;
            Directory = "";
        }

        public FlintClient Client {
            get => client;
            set {
                if(client != value) {
                    client = value;
                    Refresh();
                }
            }
        }

        public string Directory {
            get => directory;
            set {
                string dir = value.Replace("\\", "/").Replace("//", "/");
                if(dir != directory) {
                    directory = dir;
                    List<string> tmp = new List<string>();
                    tmp.Add("Home");
                    tmp.AddRange(directory.Split('/'));
                    addressBreadcrumbBar.ItemsSource = tmp;
                    Refresh();
                }
            }
        }

        private void Refresh() {
            if(client == null)
                return;
            filesItemViewModels.Clear();
            Thread thread = new Thread(() => {
                lock(client) {
                    if(!client.OpenDirRequest(directory))
                        return;
                    while(true) {
                        FlintFileInfo fileInfo = client.ReadDirRequest();
                        if(fileInfo == null)
                            break;
                        DispatcherQueue.TryEnqueue(() => filesItemViewModels.Add(new FileInfoItemViewModel(fileInfo)));
                    }
                    client.ClosedDirRequest();
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private void addressTextBoxLostFocus(object sender, RoutedEventArgs e) {
            addressTextBox.Text = "";
            addressBreadcrumbBar.Visibility = Visibility.Visible;
        }

        private void addressTextBoxGotFocus(object sender, RoutedEventArgs e) {
            addressTextBox.Text = directory;
            if(directory != null) {
                addressTextBox.SelectionStart = 0;
                addressTextBox.SelectionLength = directory.Length;
            }
            addressBreadcrumbBar.Visibility = Visibility.Collapsed;
        }

        private void upButtonClick(object sender, RoutedEventArgs e) {
            int index = directory.LastIndexOf("/");
            if(index > 0) {
                string parentPath = directory.Substring(0, index);
                Directory = parentPath;
            }
            else
                Directory = "";
        }

        private void refreshButtonClick(object sender, RoutedEventArgs e) {
            Refresh();
        }

        private void fileViewDataGridDoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
            FileInfoItemViewModel selectedItem = fileViewDataGrid.SelectedItem as FileInfoItemViewModel;
            if(!selectedItem.IsFile)
                Directory = Path.Combine(directory, selectedItem.Name);
        }
    }
}
