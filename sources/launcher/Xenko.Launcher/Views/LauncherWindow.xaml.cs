// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;

using Xenko.LauncherApp.Services;
using Xenko.LauncherApp.ViewModels;
using Xenko.Core.Packages;
using Xenko.Core.Presentation.Dialogs;
using Xenko.Core.Presentation.Extensions;
using Xenko.Core.Presentation.View;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.LauncherApp.Views
{
    /// <summary>
    /// Interaction logic for LauncherWindow.xaml
    /// </summary>
    public partial class LauncherWindow
    {
        
        public LauncherWindow()
        {
            InitializeComponent();
            ExitOnUserClose = true;
            Loaded += OnLoaded;
            TabControl.SelectedIndex = LauncherSettings.CurrentTab >= 0 ? LauncherSettings.CurrentTab : 0;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var handle = new WindowInteropHelper(this);
            LauncherViewModel.WindowHandle = handle.Handle;     //设置LauncherViewModel windows句柄
            //初始化窗口大小
            InitializeWindowSize();
        }

        private void InitializeWindowSize()
        {   //窗口大小为当前屏幕大小的一半 居中
            var workArea = this.GetWorkArea();
            Width = Math.Min(Width, workArea.Width);
            Height = Math.Min(Height, workArea.Height);
            this.CenterToArea(workArea);       
        }

        public bool ExitOnUserClose { get; set; }
        
        private LauncherViewModel ViewModel => (LauncherViewModel)DataContext;

        internal void Initialize(NugetStore store, string defaultLogText = null)
        {
            var dispatcherService = new DispatcherService(Dispatcher);      //调度员服务  
            var dialogService = new DialogService(dispatcherService, Launcher.ApplicationName);     //对话框服务
            var serviceProvider = new ViewModelServiceProvider(new object[] {dispatcherService, dialogService});        //服务提供者
            DataContext = new LauncherViewModel(serviceProvider, store);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (ViewModel.XenkoVersions.Any(x => x.IsProcessing))
            {
                var forceClose = Launcher.DisplayMessage("Some background operations are still in progress. Force close?");

                if (!forceClose)
                {
                    e.Cancel = true;
                    return;
                }
            }

            var viewModel = (LauncherViewModel)DataContext;
            LauncherSettings.ActiveVersion = viewModel.ActiveVersion != null ? viewModel.ActiveVersion.Name : ""; 
            LauncherSettings.Save();
            if (ExitOnUserClose)
                Environment.Exit(1);
        }

        private void SelectedTabChanged(object sender, SelectionChangedEventArgs e)
        {
            LauncherSettings.CurrentTab = TabControl.SelectedIndex;
        }

        private void OpenWithClicked(object sender, RoutedEventArgs e)
        {
            var dependencyObject = sender as DependencyObject;
            if (dependencyObject == null)
                return;

            var scrollViewer = dependencyObject.FindVisualParentOfType<ScrollViewer>();
            scrollViewer?.FindLogicalParentOfType<Popup>()?.SetCurrentValue(Popup.IsOpenProperty, false);
        }
    }
}
