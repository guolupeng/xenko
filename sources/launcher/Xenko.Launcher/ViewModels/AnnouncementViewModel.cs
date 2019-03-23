// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.LauncherApp.ViewModels
{   //公告视图模型
    internal class AnnouncementViewModel : DispatcherViewModel
    {
        private readonly LauncherViewModel launcher;    //启动视图模型
        private readonly string announcementName;       //公告名
        private bool validated = true;      //已被看见
        private bool dontShowAgain;     //不再显示

        public AnnouncementViewModel(LauncherViewModel launcher, string announcementName)
            : base(launcher.SafeArgument(nameof(launcher)).ServiceProvider) //根据名称获取服务提供者
        {
            this.launcher = launcher;
            this.announcementName = announcementName;
            if (!LauncherViewModel.HasDoneTask(TaskName))
            {
                MarkdownAnnouncement = Initialize(announcementName);
            }
            // 我们希望明确地触发视图故事板的属性更改通知 We want to explicitely trigger the property change notification for the view storyboard
            Dispatcher.InvokeAsync(() => Validated = false);
            CloseAnnouncementCommand = new AnonymousCommand(ServiceProvider, CloseAnnouncement);
        }

        private void CloseAnnouncement()
        {
            Validated = true;
            if (DontShowAgain)  //不在显示
            {
                LauncherViewModel.SaveTaskAsDone(TaskName);
            }
        }
        //公告的md文本
        public string MarkdownAnnouncement { get; }

        public bool Validated { get { return validated; } set { SetValue(ref validated, value); } }

        public bool DontShowAgain { get { return dontShowAgain; } set { SetValue(ref dontShowAgain, value); } }

        public ICommandBase CloseAnnouncementCommand { get; }
        //任务名
        private string TaskName => GetTaskName(announcementName);

        public static string GetTaskName(string announcementName)
        {
            return "Announcement" + announcementName;
        }

        private static string Initialize(string announcementName)
        {
            try
            {
                var executingAssembly = Assembly.GetExecutingAssembly();        //获取程序集资源中的 公告名.md文件路径
                var path = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(x => x.EndsWith(announcementName + ".md"));
                using (var stream = executingAssembly.GetManifestResourceStream(path))
                {
                    if (stream == null)
                        return null;

                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
