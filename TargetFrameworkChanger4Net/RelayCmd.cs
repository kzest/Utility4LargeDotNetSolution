using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TargetFrameworkChanger4Net
{
    public class RelayCmd : ICommand
    {
        public Func<object, bool> _canExecute { get; set; }
        public Action<object> _execute { get; set; }

        public RelayCmd(Func<object, bool> canExecute, Action<object> execute)
        {
            this._canExecute = canExecute;
            this._execute = execute;
        }

        #region ICommand
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (this._canExecute != null)
                return this._canExecute(parameter);
            else
                return false;
        }

        public void Execute(object parameter)
        {
            if (this._execute != null)
                this._execute(parameter);
        }
        #endregion
    }
}
