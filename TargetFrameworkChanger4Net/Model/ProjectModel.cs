using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TargetFrameworkChanger4Net.Model
{
    public class ProjectModel
    {
        private Action<string> NotifierAction;

        public enum ProjectModelType
        {
            Folder,
            Project
        }
        private ProjectModelType _ProjectEntryType = ProjectModelType.Project;
        public ProjectModelType ProjectEntryType
        {
            get { return _ProjectEntryType; }
            set { if (_ProjectEntryType != value) { _ProjectEntryType = value; NotifyPropertyChanged(); } }
        }

        private string _CurrentTargetFrameworkVersion;
        public string CurrentTargetFrameworkVersion
        {
            get { return _CurrentTargetFrameworkVersion; }
            set { if (_CurrentTargetFrameworkVersion != value) { _CurrentTargetFrameworkVersion = value; SetCurrentTargetFrameworkVersion(); NotifyPropertyChanged(); } }
        }

        private string _PreviousTargetFrameworkVersion;
        public string PreviousTargetFrameworkVersion
        {
            get { return _PreviousTargetFrameworkVersion; }
            set { if (_PreviousTargetFrameworkVersion != value) { _PreviousTargetFrameworkVersion = value; NotifyPropertyChanged(); } }
        }

        private string _AssemblyName;
        public string AssemblyName
        {
            get { return _AssemblyName; }
            set { if (_AssemblyName != value) { _AssemblyName = value; NotifyPropertyChanged(); } }
        }

        private string _FullName;
        public string FullName
        {
            get { return _FullName; }
            set { if (_FullName != value) { _FullName = value; SetObjFolderFullName(); NotifyPropertyChanged(); } }
        }

        private string _ProjectGuid;
        public string ProjectGuid
        {
            get { return _ProjectGuid; }
            set { if (_ProjectGuid != value) { _ProjectGuid = value; NotifyPropertyChanged(); } }
        }

        private bool _IsSelected;
        public bool IsSelected
        {
            get { return _IsSelected; }
            set { if (_IsSelected != value) { _IsSelected = value; NotifyPropertyChanged(); } }
        }

        private string _SolutionDirectoryPath;
        public string SolutionDirectoryPath
        {
            get { return _SolutionDirectoryPath; }
            set { if (_SolutionDirectoryPath != value) { _SolutionDirectoryPath = value; LoadProjectXml(); NotifyPropertyChanged(); } }
        }
        public string ObjFolderFullName { get; set; }


        public XmlDocument ProjectXmlDocument { get; set; } = new System.Xml.XmlDocument();


        public ProjectModel(Action<string> notifierAction)
        {
            this.NotifierAction = notifierAction;
        }


        private void LoadProjectXml()
        {
            string fullPath = _SolutionDirectoryPath + "\\" + FullName;

            this.ProjectXmlDocument.Load(fullPath);

            GetCurrentTargetFrameworkVersion();
        }

        private void SetObjFolderFullName()
        {
            this.ObjFolderFullName = _FullName.Substring(0, _FullName.LastIndexOf("\\")) + "\\obj";
        }

        private void GetCurrentTargetFrameworkVersion()
        {
            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(this.ProjectXmlDocument.NameTable);
            xmlNamespaceManager.AddNamespace("msbuild", "http://schemas.microsoft.com/developer/msbuild/2003");

            XmlNodeList xmlNodeList = this.ProjectXmlDocument.SelectNodes("msbuild:Project/msbuild:PropertyGroup/msbuild:TargetFrameworkVersion", xmlNamespaceManager);
            if (xmlNodeList != null && xmlNodeList.Count > 0)
                this.CurrentTargetFrameworkVersion = xmlNodeList[0].InnerText;
        }

        private void SetCurrentTargetFrameworkVersion()
        {
            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(this.ProjectXmlDocument.NameTable);
            xmlNamespaceManager.AddNamespace("msbuild", "http://schemas.microsoft.com/developer/msbuild/2003");

            XmlNodeList xmlNodeList = this.ProjectXmlDocument.SelectNodes("msbuild:Project/msbuild:PropertyGroup/msbuild:TargetFrameworkVersion", xmlNamespaceManager);
            if (xmlNodeList != null && xmlNodeList.Count > 0)
            {
                this._PreviousTargetFrameworkVersion = xmlNodeList[0].InnerText;
                xmlNodeList[0].InnerText = this.CurrentTargetFrameworkVersion;
            }
        }

        #region INotifyPropertyChanged Delegate

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (this.NotifierAction != null)
                this.NotifierAction(propertyName);
        }

        #endregion
    }
}
