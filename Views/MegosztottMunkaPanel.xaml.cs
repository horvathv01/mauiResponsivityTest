using Geometria.MirtuszMobil.Client.Properties;
using System;
using System.ComponentModel;
using System.Windows.Controls;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    /// <summary>
    /// Interaction logic for MegosztottMunkaPanel.xaml
    /// </summary>
    public partial class MegosztottMunkaPanel : UserControl, IPanel
    {
        public MegosztottMunkaPanel()
        {
            InitializeComponent();
        }

        public bool IsLathato => true;

        public long Sorrend => 1;

        public string Fejlec => DisplayResources.MegosztottMunkaAdatokPanel;

        public void Close()
        {
            
        }

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged( [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null )
        {
            if( this.PropertyChanged != null )
            {
                this.PropertyChanged( this, new PropertyChangedEventArgs( propertyName ) );
            }
        }
        #endregion
    }
}
