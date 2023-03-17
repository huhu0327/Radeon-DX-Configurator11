using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Navigation;

namespace Radeon_DX_Configurator.Model
{
    public class MainWindowModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private ObservableCollection<string> currentValue;
        public ObservableCollection<string> CurrentValue
        {
            get { return currentValue; }
            set
            {
                currentValue = value;
                OnPropertyChanged("CurrentValue");
            }
        }
            
        private ObservableCollection<string> currentWOWValue;
        public ObservableCollection<string> CurrentWOWValue
        {
            get { return currentWOWValue; }
            set
            {
                currentWOWValue = value;
                OnPropertyChanged("CurrentWOWValue");
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}

