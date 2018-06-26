using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TargetFrameworkChanger4Net.Model
{
    public class MainModel : INotifyPropertyChanged
    {

        private string _SolutionFileName = "[Solution File Name]";
        public string SolutionFileName
        {
            get { return _SolutionFileName; }
            set { if (_SolutionFileName != value) { _SolutionFileName = value; NotifyPropertyChanged(); } }
        }

        private string _SolutionFileText;
        public string SolutionFileText
        {
            get { return _SolutionFileText; }
            set { if (_SolutionFileText != value) { _SolutionFileText = value; new Task(() => GenerateProjectsList()).Start(); NotifyPropertyChanged(); } }
        }

        public ConcurrentObservableList<ProjectModel> ListProjects { get; set; }

        private string _VersionBtnContent = "Select V.";
        public string VersionBtnContent
        {
            get { return _VersionBtnContent; }
            set { if (_VersionBtnContent != value) { _VersionBtnContent = value; NotifyPropertyChanged(); } }
        }

        public ConcurrentObservableList<string> ListInstalledVersions { get; set; }

        private int _LstVerSelectedIndex = 0;
        public int LstVerSelectedIndex
        {
            get { return _LstVerSelectedIndex; }
            set { if (_LstVerSelectedIndex != value) { _LstVerSelectedIndex = value; NotifyPropertyChanged(); SetVersionBtnContent(); } }
        }


        private bool _DisplayMessage = false;
        public bool DisplayMessage
        {
            get { return _DisplayMessage; }
            set { if (_DisplayMessage != value) { _DisplayMessage = value; NotifyPropertyChanged(); } }
        }

        private string _Message;
        public string Message
        {
            get { return _Message; }
            set { if (_Message != value) { _Message = value; NotifyPropertyChanged(); } }
        }

        private Brush _MessageBackground;
        public Brush MessageBackground
        {
            get { return _MessageBackground; }
            set { if (_MessageBackground != value) { _MessageBackground = value; NotifyPropertyChanged(); } }
        }





        public MainModel(ViewModel.ViewModel ownerModel)
        {
            this.ListProjects = new ConcurrentObservableList<ProjectModel>();
            this.ListInstalledVersions = new ConcurrentObservableList<string>();
        }

        private async Task GenerateProjectsList()
        {
            this.ListProjects.Clear();

            List<string> solFileLines = SolutionFileText.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            int iCount = solFileLines.Count;
            for(int i = 0; i < iCount; i++)
            {
                string projectLine = null;

                if (solFileLines[i].StartsWith("Project(\"{"))
                    projectLine = solFileLines[i];

                if (!string.IsNullOrWhiteSpace(projectLine))
                {
                    List<string> projectLineComponents = projectLine.Split('=').ToList();

                    if (projectLineComponents.Count > 0)
                        projectLineComponents = projectLineComponents[1].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).ToList();

                    projectLineComponents = projectLineComponents.Select<string, string>(x => x.Replace("\"", "").Replace(",", "")).ToList();

                    ProjectModel.ProjectModelType projectModelType = projectLineComponents[1].EndsWith(".csproj") ? ProjectModel.ProjectModelType.Project : ProjectModel.ProjectModelType.Folder;

                    if (projectModelType == ProjectModel.ProjectModelType.Project)
                    {
                        ProjectModel projectModel = new ProjectModel(new Action<string>(NotifyPropertyChanged))
                        {
                            AssemblyName = projectLineComponents[0],
                            FullName = projectLineComponents[1],
                            ProjectGuid = projectLineComponents[2],
                            ProjectEntryType = projectModelType,
                            SolutionDirectoryPath = _SolutionFileName.Substring(0, _SolutionFileName.LastIndexOf("\\"))
                        };

                        this.ListProjects.Add(projectModel);
                    }
                }
            }

            this.ListProjects.Sort((x1, x2) => x1.AssemblyName.CompareTo(x2.AssemblyName));
        }

        private void SetVersionBtnContent()
        {
            if (this.LstVerSelectedIndex != -1)
                this.VersionBtnContent = this.ListInstalledVersions[this.LstVerSelectedIndex];
        }

        #region INotifyPropertyChanged
        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}
