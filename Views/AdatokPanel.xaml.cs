using Geometria.MirtuszMobil.Client.HelperClasses;
using Geometria.MirtuszMobil.Client.Properties;
using Geometria.MirtuszMobil.Client.StatuszFSM;
using Geometria.MirtuszMobil.Client.Storages;
using Geometria.MirtuszService.MessageClasses;
using Geometria.MirtuszService.MessageClasses.CodeValues;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    /// <summary>
    /// Interaction logic for AdatokPanel.xaml
    /// </summary>
    public partial class AdatokPanel : UserControl, IPanel
    {
        public AdatokPanel( Munkautasitas munkautasitas )
        {
            InitializeComponent();
            SetTenyMennyisegIsEnabled( munkautasitas );
        }

        private void SetTenyMennyisegIsEnabled(Munkautasitas munkautasitas)
        {
           if( Storage.Instance.Session.MegnyitottMunkautasitas.KomplexMunkaAdatai.KomplexMunkaTipus == KomplexMunkaTipus.FOGYHELY)
           {
                munkautasitas.ElemiMunkaAdatai.TenyMennyiseg = 1;
                TenyMennyTextBox.IsEnabled = false;
                return;
           }
        }

        public bool IsLathato => true;

        public long Sorrend => 3;

        public string Fejlec => DisplayResources.MunkaAdatok;

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
