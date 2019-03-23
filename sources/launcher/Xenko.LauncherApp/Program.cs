// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Xenko.LauncherApp
{
    static class Program
    {
        private const string LauncherPrerequisites = @"Prerequisites\launcher-prerequisites.exe";

        [STAThread]
        [LoaderOptimization(LoaderOptimization.MultiDomainHost)] // Optimize loading of AppDomain assemblies 优化AppDomain程序集的加载
        private static void Main(string[] args)
        {
            // Check prerequisites 检查先决条件
            var prerequisiteLog = new StringBuilder();      //先决条件
            var prerequisitesFailedOnce = false;            //先决条件失败一次
            while (!CheckPrerequisites(prerequisiteLog)) //如果.net版本低于4.7.2
            {
                prerequisitesFailedOnce = true;

                // 检查启动程序先决安装程序是否存在 Check if launcher prerequisite installer exists 
                if (!File.Exists(LauncherPrerequisites))
                {
                    MessageBox.Show($"Some prerequisites are missing, but no prerequisite installer was found!\n\n{prerequisiteLog}\n\nPlease install them manually or report the problem.", "Prerequisite error", MessageBoxButtons.OK);
                    return;
                }

                // 其中一个先决条件失败，启动先决条件安装程序 One of the prerequisite failed, launch the prerequisite installer
                var prerequisitesApproved = MessageBox.Show($"Some prerequisites are missing, do you want to install them?\n\n{prerequisiteLog}", "Install missing prerequisites?", MessageBoxButtons.OKCancel);
                if (prerequisitesApproved == DialogResult.Cancel)
                    return;
                //如果用户选择安装先决条件 启动安装进程
                try
                {
                    var prerequisitesInstallerProcess = Process.Start(LauncherPrerequisites);
                    if (prerequisitesInstallerProcess == null)
                    {
                        MessageBox.Show($"There was an error running the prerequisite installer {LauncherPrerequisites}.", "Prerequisite error", MessageBoxButtons.OK);
                        return;
                    }

                    prerequisitesInstallerProcess.WaitForExit();
                }
                catch
                {
                    MessageBox.Show($"There was an error running the prerequisite installer {LauncherPrerequisites}.", "Prerequisite error", MessageBoxButtons.OK);
                    return;
                }
                prerequisiteLog.Length = 0;
            }
            //先决条件安装完成
            if (prerequisitesFailedOnce)
            {
                // 如果先决条件至少失败一次，我们希望重新启动，以使用适当的.NET框架运行 If prerequisites failed at least once, we want to restart ourselves to run with proper .NET framework
                var exeLocation = Assembly.GetEntryAssembly().Location;
                if (File.Exists(exeLocation))
                {
                    // Forward arguments
                    for (int i = 0; i < args.Length; ++i)
                    {
                        // 带空格的引数 Quote arguments with spaces
                        if (args[i].IndexOf(' ') != -1)
                            args[i] = '\"' + args[i] + '\"';
                    }
                    var arguments = string.Join(" ", args);

                    // Start process
                    Process.Start(exeLocation, arguments);
                }
                return;
            }

            // 将程序集加载为嵌入式资源 Loading assemblies as embedded resources
            // see http://www.digitallycreated.net/Blog/61/combining-multiple-assemblies-into-a-single-exe-for-a-wpf-application
            // 该类不应引用任何嵌入式类型，以确保在此之前注册了处理程序 NOTE: this class should not reference any of the embedded type to ensure the handler is registered before
            // these types are loaded
            // TODO: can we register this handler in the Module initializer?
            AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;       //程序集解析失败处理
            AppDomain.CurrentDomain.ExecuteAssemblyByName("Xenko.Launcher", null, args);        //启动Xenko.Launcher程序集
        }

        private static bool CheckPrerequisites(StringBuilder prerequisiteLog)
        {
            var result = true;

            // Check for .NET 4.7.2+ 检查先决条件
            if (!CheckDotNet4Version(461808))
            {   //没有.net4.7.2.+版本
                prerequisiteLog.AppendLine("- .NET framework 4.7.2");
                result = false;
            }

            // Everything passed
            return result;
        }

        private static bool CheckDotNet4Version(int requiredVersion)
        {
            // Check for .NET v4 version 检测.net版本
            using (var ndpKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
            {
                if (ndpKey == null)
                    return false;

                int releaseKey = Convert.ToInt32(ndpKey.GetValue("Release"));
                if (releaseKey < requiredVersion)
                    return false;
            }

            return true;
        }

        private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            var assemblyName = new AssemblyName(args.Name);

            // PCL系统组件使用的是2.0.5.0版本，而我们使用的是4.0版本 PCL System assemblies are using version 2.0.5.0 while we have a 4.0
            // 将PCL重定向到从当前应用程序域使用4.0 Redirect the PCL to use the 4.0 from the current app domain.
            if (assemblyName.Name.StartsWith("System") && (assemblyName.Flags & AssemblyNameFlags.Retargetable) != 0)
            {
                Assembly systemCoreAssembly = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.GetName().Name == assemblyName.Name)
                    {
                        systemCoreAssembly = assembly;
                        break;
                    }
                }
                return systemCoreAssembly;
            }

            foreach (var extension in new string[] { ".dll", ".exe" })
            {
                var path = assemblyName.Name + extension;
                if (assemblyName.CultureInfo != null && assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture) == false)
                {
                    path = $@"{assemblyName.CultureInfo}\{path}";
                }

                using (Stream stream = executingAssembly.GetManifestResourceStream(path))
                {
                    if (stream != null)
                    {
                        var assemblyRawBytes = new byte[stream.Length];
                        stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
#if DEBUG
                        byte[] symbolsRawBytes = null;
                        // Let's load the PDB if it exists
                        using (Stream symbolsStream = executingAssembly.GetManifestResourceStream(assemblyName.Name + ".pdb"))
                        {
                            if (symbolsStream != null)
                            {
                                symbolsRawBytes = new byte[symbolsStream.Length];
                                symbolsStream.Read(symbolsRawBytes, 0, symbolsRawBytes.Length);
                            }
                        }
                        return Assembly.Load(assemblyRawBytes, symbolsRawBytes);
#else
                        return Assembly.Load(assemblyRawBytes);
#endif
                    }
                }
            }

            return null;
        }
    }
}
