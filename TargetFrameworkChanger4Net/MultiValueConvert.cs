using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using TargetFrameworkChanger4Net.Model;

namespace TargetFrameworkChanger4Net
{
    public class MultiValueConvert : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            dynamic expando = new ExpandoObject();
            int iCount = values.Length;
            for (int i = 0; i < iCount; i++)
            {
                object val = values[i];

                if(parameter.ToString() == "SelectedProject")
                {
                    expando.ProjectModelType = (ProjectModel.ProjectModelType)values[0];
                    expando.ProjectGuid = values[1] as string;
                    expando.IsSelected = (bool)values[2];
                    expando.RootWindow = values[3];
                    break;
                }
                else if(parameter.ToString() == "SelectAll")
                {
                    expando.IsChecked = (bool)values[0];
                    expando.ListView = (System.Windows.Controls.ListView)values[1];
                    expando.RootWindow = values[2];
                    break;
                }
                else if(parameter.ToString() == "ChangeTargetFrameworkVersions")
                {
                    expando.CurrentTargetFrameworkVersion = values[0] as string;
                    expando.ListView = (System.Windows.Controls.ListView)values[1];
                    expando.RootWindow = values[2];
                    break;
                }
                else
                {
                    (expando as IDictionary<string, object>).Add(val.GetType().Name, val);
                }                
            }

            return expando;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[0];
        }
    }
}
