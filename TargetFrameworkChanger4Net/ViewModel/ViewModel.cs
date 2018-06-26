using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace TargetFrameworkChanger4Net.ViewModel
{
    public class ViewModel : DependencyObject
    {
        public Model.MainModel TheMainModel { get; set; }
        public RelayCmd FileOpenDialogCmd { get; set; }
        public OpenFileDialog FileOpenDialog { get; set; } = new OpenFileDialog();
        public RelayCmd CheckBoxCmd { get; set; }
        public RelayCmd ShowVersionsCmd { get; set; }
        public RelayCmd ChangeTargetFrameworkVersionsCmd { get; set; }
        public RelayCmd UndoChangeTargetFrameworkVersionsCmd { get; set; }
        public RelayCmd DeleteObjFolderCmd { get; set; }

        public enum MessageTypes
        {
            Info,
            Error,
            Exception
        }


        public View.MainWindow RootWindow
        {
            get { return (View.MainWindow)GetValue(RootWindowProperty); }
            set { SetValue(RootWindowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RootWindow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RootWindowProperty =
            DependencyProperty.Register("RootWindow", typeof(View.MainWindow), typeof(ViewModel), new PropertyMetadata(new PropertyChangedCallback((d, e) => {})));





        public ViewModel()
        {
            this.TheMainModel = new Model.MainModel(this);
            this.FileOpenDialogCmd = new RelayCmd(new Func<object, bool>(x => true), new Action<object>(showFileOpenDialog));
            this.CheckBoxCmd = new RelayCmd(new Func<object, bool>(x => true), new Action<object>(ChangeCheckBoxCheck));
            this.ShowVersionsCmd = new RelayCmd(new Func<object, bool>(x => true), new Action<object>(ShowVersions));
            this.ChangeTargetFrameworkVersionsCmd = new RelayCmd(new Func<object, bool>(x => true), new Action<object>(ChangeTargetFrameworkVersions));
            this.UndoChangeTargetFrameworkVersionsCmd = new RelayCmd(new Func<object, bool>(x => true), new Action<object>(UndoChangeTargetFrameworkVersions));
            this.DeleteObjFolderCmd = new RelayCmd(new Func<object, bool>(x => true), new Action<object>(DeleteObjFolder));

            this.TheMainModel.ListInstalledVersions = GetAllNetVersionsInstalled();
        }

        private void showFileOpenDialog(object parameter)
        {
            if (parameter != null && parameter is View.MainWindow)
                this.RootWindow = parameter as View.MainWindow;

            this.FileOpenDialog.Filter = "Solution Files (.SLN)|*.sln";

            this.FileOpenDialog.ShowDialog(this.RootWindow);

            this.TheMainModel.SolutionFileName = this.FileOpenDialog.FileName;

            new Task(() => LoadSolFileTask()).Start();
        }

        private async Task LoadSolFileTask()
        {
            try
            {
                FileInfo fInfo = new FileInfo(this.TheMainModel.SolutionFileName);

                if (!fInfo.Exists)
                {
                    return;
                }
                else
                {
                    byte[] arrBytes = null;

                    using (FileStream fs = fInfo.OpenRead())
                    {
                        int len = (int)fs.Length;
                        arrBytes = new byte[len];
                        fs.Seek(0, SeekOrigin.Begin);
                        int i = await fs.ReadAsync(arrBytes, 0, len);
                    }

                    if (arrBytes != null)
                    {
                        this.TheMainModel.SolutionFileText = ASCIIEncoding.ASCII.GetString(arrBytes);
                    }
                }
            }
            catch(Exception e)
            {
                DisplayMessage(e.Message, MessageTypes.Exception);
            }
        }

        private void ChangeCheckBoxCheck(object parameter)
        {
            dynamic expando = (ExpandoObject)parameter;

            if (this.RootWindow == null && ((IDictionary<string, object>)parameter).ContainsKey("RootWindow"))
                this.RootWindow = expando.RootWindow;

            if (parameter is ExpandoObject && ((IDictionary<string, object>)parameter).ContainsKey("ProjectGuid"))
            {
                this.TheMainModel.ListProjects.Where(x => x.ProjectGuid == expando.ProjectGuid).FirstOrDefault().IsSelected = expando.IsSelected;
            }
            else if (parameter is ExpandoObject && ((IDictionary<string, object>)parameter).ContainsKey("ListView"))
            {
                foreach (Model.ProjectModel projectModel in this.TheMainModel.ListProjects)
                {
                    projectModel.IsSelected = expando.IsChecked;
                    (expando.ListView as System.Windows.Controls.ListView).Items.Refresh();
                }
            }
        }

        private void ShowVersions(object parameter)
        {
            if (this.RootWindow == null && parameter is View.MainWindow)
                this.RootWindow = parameter as View.MainWindow;

            this.TheMainModel.LstVerSelectedIndex = -1;
        }

        private void ChangeTargetFrameworkVersions(object parameter)
        {
            try
            {
                dynamic expando = parameter as ExpandoObject;

                if (this.RootWindow == null && ((IDictionary<string, object>)parameter).ContainsKey("RootWindow"))
                    this.RootWindow = expando.RootWindow;

                if (parameter is ExpandoObject && ((IDictionary<string, object>)parameter).ContainsKey("CurrentTargetFrameworkVersion"))
                {
                    if (expando.CurrentTargetFrameworkVersion != null
                        && expando.CurrentTargetFrameworkVersion is string
                        && expando.CurrentTargetFrameworkVersion != "Select V.")
                    {
                        int iCount = this.TheMainModel.ListProjects.Count;
                        bool anySelected = false;
                        for (int i = 0; i < iCount; i++)
                        {
                            if (this.TheMainModel.ListProjects[i].IsSelected)
                            {
                                this.TheMainModel.ListProjects[i].CurrentTargetFrameworkVersion = expando.CurrentTargetFrameworkVersion;
                                FileInfo fInfo = new FileInfo(this.TheMainModel.ListProjects[i].SolutionDirectoryPath + "\\" + this.TheMainModel.ListProjects[i].FullName);
                                if (fInfo.Exists)
                                    this.TheMainModel.ListProjects[i].ProjectXmlDocument.Save(fInfo.FullName);

                                anySelected = true;
                            }
                        }

                        (expando.ListView as System.Windows.Controls.ListView).Items.Refresh();

                        if (!anySelected)
                            DisplayMessage("Please select a project", MessageTypes.Error);
                        else
                            DisplayMessage("Changed target framework version successfully", MessageTypes.Info);

                    }
                    else if (expando.CurrentTargetFrameworkVersion == "Select V.")
                    {
                        DisplayMessage("Please select a target framework version", MessageTypes.Error);
                    }
                }
            }
            catch(Exception e)
            {
                DisplayMessage(e.Message, MessageTypes.Exception);
            }
        }
        
        private void UndoChangeTargetFrameworkVersions(object parameter)
        {
            try
            {
                dynamic expando = parameter as ExpandoObject;

                if (this.RootWindow == null && ((IDictionary<string, object>)parameter).ContainsKey("RootWindow"))
                    this.RootWindow = expando.RootWindow;

                if (parameter is ExpandoObject && ((IDictionary<string, object>)parameter).ContainsKey("CurrentTargetFrameworkVersion"))
                {
                    if (expando.CurrentTargetFrameworkVersion != null
                        && expando.CurrentTargetFrameworkVersion is string
                        && expando.CurrentTargetFrameworkVersion != "Select V.")
                    {
                        int iCount = this.TheMainModel.ListProjects.Count;
                        bool anySelected = false;
                        for (int i = 0; i < iCount; i++)
                        {
                            if (this.TheMainModel.ListProjects[i].IsSelected)
                            {
                                this.TheMainModel.ListProjects[i].CurrentTargetFrameworkVersion = this.TheMainModel.ListProjects[i].PreviousTargetFrameworkVersion;
                                FileInfo fInfo = new FileInfo(this.TheMainModel.ListProjects[i].SolutionDirectoryPath + "\\" + this.TheMainModel.ListProjects[i].FullName);
                                if (fInfo.Exists)
                                    this.TheMainModel.ListProjects[i].ProjectXmlDocument.Save(fInfo.FullName);

                                anySelected = true;
                            }
                        }

                        (expando.ListView as System.Windows.Controls.ListView).Items.Refresh();

                        if (!anySelected)
                            DisplayMessage("Please select a project", MessageTypes.Error);
                        else
                            DisplayMessage("Target Framework change reverted successfully", MessageTypes.Info);

                    }
                }
            }
            catch(Exception e)
            {
                DisplayMessage(e.Message, MessageTypes.Exception);
            }
        }
        
        private void DeleteObjFolder(object parameter)
        {
            try
            {
                if (this.RootWindow == null && parameter is View.MainWindow)
                    this.RootWindow = parameter as View.MainWindow;

                int iCount = this.TheMainModel.ListProjects.Count;
                bool anySelected = false;
                for (int i = 0; i < iCount; i++)
                {
                    if (this.TheMainModel.ListProjects[i].IsSelected)
                    {
                        this.TheMainModel.ListProjects[i].CurrentTargetFrameworkVersion = this.TheMainModel.ListProjects[i].PreviousTargetFrameworkVersion;
                        DirectoryInfo dirInfo = new DirectoryInfo(this.TheMainModel.ListProjects[i].SolutionDirectoryPath + "\\" + this.TheMainModel.ListProjects[i].ObjFolderFullName);
                        if (dirInfo.Exists)
                            dirInfo.Delete(true);

                        anySelected = true;
                    }
                }

                if(!anySelected)
                    DisplayMessage("Please select a project", MessageTypes.Error);
                else
                    DisplayMessage("Selected Obj folders deleted successfully", MessageTypes.Info);
            }
            catch(Exception e)
            {
                DisplayMessage(e.Message, MessageTypes.Exception);
            }
        }

        private void DisplayMessage(string message, MessageTypes messageType)
        {
            switch(messageType)
            {
                case MessageTypes.Error:
                    this.TheMainModel.MessageBackground = Brushes.Goldenrod;
                    break;

                case MessageTypes.Exception:
                    this.TheMainModel.MessageBackground = Brushes.Red;
                    break;

                case MessageTypes.Info:
                    this.TheMainModel.MessageBackground = Brushes.LightGreen;
                    break;
            }

            this.TheMainModel.Message = message;
            this.TheMainModel.DisplayMessage = true;
            new Task(new Action(() => this.TheMainModel.DisplayMessage = false)).Start();
        }

        private ConcurrentObservableList<string> GetAllNetVersionsInstalled()
        {
            string path = @"SOFTWARE\Microsoft\NET Framework Setup\NDP";
            ConcurrentObservableList<string> display_framwork_name = new ConcurrentObservableList<string>();
            string temp_name = string.Empty;

            using (RegistryKey installed_versions = Registry.LocalMachine.OpenSubKey(path))
            {
                string[] version_names = installed_versions.GetSubKeyNames();
                for (int i = 1; i <= version_names.Length - 1; i++)
                {
                    //string temp_name = "Microsoft .NET Framework " + version_names[i].ToString() + "  SP" + installed_versions.OpenSubKey(version_names[i]).GetValue("SP");
                    temp_name = version_names[i].ToString();
                    display_framwork_name.Add(temp_name);
                }
            }

            using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                                        .OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
            {
                int releaseKey = Convert.ToInt32(ndpKey.GetValue("Release"));

                if (releaseKey >= 461808)
                {
                    temp_name =  "v4.7.2";
                    display_framwork_name.Add(temp_name);
                }
                if (releaseKey >= 461308)
                {
                    temp_name =  "v4.7.1";
                    display_framwork_name.Add(temp_name);
                }
                if (releaseKey >= 460798)
                {
                    temp_name =  "v4.7";
                    display_framwork_name.Add(temp_name);
                }
                if (releaseKey >= 394802)
                {
                    temp_name =  "v4.6.2";
                    display_framwork_name.Add(temp_name);
                }
                if (releaseKey >= 394254)
                {
                    temp_name =  "v4.6.1";
                    display_framwork_name.Add(temp_name);
                }
                if (releaseKey >= 393295)
                {
                    temp_name =  "v4.6";
                    display_framwork_name.Add(temp_name);
                }
                if ((releaseKey >= 379893))
                {
                    temp_name =  "v4.5.2";
                    display_framwork_name.Add(temp_name);
                }
                if ((releaseKey >= 378675))
                {
                    temp_name =  "v4.5.1";
                    display_framwork_name.Add(temp_name);
                }
                if ((releaseKey >= 378389))
                {
                    temp_name =  "v4.5";
                    display_framwork_name.Add(temp_name);
                }
            }

            display_framwork_name.Sort((x1, x2) => x1.CompareTo(x2));

            return display_framwork_name;
        }
    }
}
