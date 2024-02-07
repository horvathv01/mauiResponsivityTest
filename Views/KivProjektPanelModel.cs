using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Geometria.MirtuszMobil.Client.Messages;
using Geometria.MirtuszMobil.Client.Properties;
using Geometria.MirtuszMobil.Client.Storages;
using Geometria.GeoMobil.Client.UI.Dialogs;
using Geometria.MirtuszService.MessageClasses;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    public class KivProjektPanelModel : INotifyPropertyChanged
    {
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

        #region PopUpIsOpen
        private bool m_PopUpIsOpen = false;
        public bool PopUpIsOpen
        {
            get { return m_PopUpIsOpen; }
            set
            {
                m_PopUpIsOpen = value;
                RaisePropertyChanged( nameof( PopUpIsOpen ) );
            }
        }
        #endregion

        #region Munkautasitas
        private Munkautasitas m_Munkautasitas;
        public Munkautasitas Munkautasitas
        {
            get
            {
                if( m_Munkautasitas == null )
                {
                    m_Munkautasitas = new Munkautasitas();
                }
                return m_Munkautasitas;
            }
            set
            {
                if( m_Munkautasitas == value )
                {
                    return;
                }
                m_Munkautasitas = value;
                RaisePropertyChanged( nameof( Munkautasitas ) );
            }
        }
        #endregion

        #region KivitelezesiProjekt
        private KivitelezesiProjekt m_Projekt;
        public KivitelezesiProjekt Projekt
        {
            get
            {
                if( Storage.Instance.Session.ProjektMunkak != null )
                    m_Projekt = Storage.Instance.Session.ProjektMunkak.Where( pm => pm.MunkautAzonosito == Munkautasitas.Azonosito ).FirstOrDefault();
                return m_Projekt;
            }
            set
            {
                if( m_Projekt == value )
                {
                    return;
                }
                m_Projekt = value;
                RaisePropertyChanged( nameof( Projekt ) );
            }
        }
        #endregion

        #region LeltariHelyFejezetek

        public List<LeltariHelyFejezet> LeltariHelyFejezetek
        {
            get
            {
                return Projekt.LeltariHelyFejezetek.Where( x => x.Torzstetelek.Count > 0 ).ToList();
            }
        }

        #endregion

        public KivProjektPanelModel( Munkautasitas munkautasitas )
        {
            Messenger.Default.Register<ProjektUpdateMessage>( this, ProjektUpdate_Handler );

            Munkautasitas = munkautasitas;
        }

        #region Commands

        #region MindenKeszCommand

        private RelayCommand<LeltariHelyFejezet> m_MindenKeszCommand;

        public RelayCommand<LeltariHelyFejezet> MindenKeszCommand
        {
            get
            {
                if( m_MindenKeszCommand == null )
                    m_MindenKeszCommand = new RelayCommand<LeltariHelyFejezet>( MindenKeszCommandCall );
                return m_MindenKeszCommand;
            }
        }

        private void MindenKeszCommandCall( LeltariHelyFejezet parameter )
        {
            PopUpIsOpen = true;

            if( MessageBoxWpf.Show( DisplayResources.MindenKesz_KivProjekt, button: MessageBoxButton.YesNo ) == MessageBoxResult.Yes )
            {
                if( parameter != null )
                {
                    parameter.Torzstetelek.ForEach( tt => tt.ElszamoltMennyiseg = tt.Mennyiseg );
                }
            }
            else
            {
                PopUpIsOpen = false;
                return;
            }
        }

        #endregion

        #region MindenKeszForAll

        private RelayCommand m_MindenKeszForAll;

        public RelayCommand MindenKeszForAll
        {
            get
            {
                if( m_MindenKeszForAll == null )
                    m_MindenKeszForAll = new RelayCommand( MindenKeszForAllCall );
                return m_MindenKeszForAll;
            }
        }

        private void MindenKeszForAllCall()
        {
            PopUpIsOpen = true;

            if( MessageBoxWpf.Show( DisplayResources.MindenKeszForAll_KivProjekt, button: MessageBoxButton.YesNo ) == MessageBoxResult.Yes )
            {
                if( Projekt != null && Projekt.LeltariHelyFejezetek != null )
                {
                    foreach( var fejezet in Projekt.LeltariHelyFejezetek )
                    {
                        fejezet.Torzstetelek.ForEach( tt => tt.ElszamoltMennyiseg = tt.Mennyiseg );
                    }
                }
            }
            else
            {
                PopUpIsOpen = false;
                return;
            }
        }

        #endregion

        #endregion

        #region Helper Methods

        private void ProjektUpdate_Handler( ProjektUpdateMessage uzenet )
        {
            Projekt = uzenet.UjProjekt;
        }

        #endregion

        public bool Close()
        {
            Messenger.Default.Unregister<ProjektUpdateMessage>( this );

            return true;
        }
    }
}
