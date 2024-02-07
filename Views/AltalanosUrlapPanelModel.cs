using Geometria.MirtuszMobil.Common.ViewModels;
using GalaSoft.MvvmLight.Messaging;
using Geometria.MirtuszMobil.Client.Messages;
using Geometria.MirtuszService.MessageClasses;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    public class AltalanosUrlapPanelModel : ViewModelBase
    {
        #region Urlap

        /// <summary>
        /// Urlap member változója
        /// </summary>
        private Urlap m_Urlap;

        /// <summary>
        /// Urlap get/set
        /// </summary>
        public Urlap Urlap
        {
            get
            {
                return m_Urlap;
            }

            set
            {
                if( m_Urlap == value )
                    return;

                m_Urlap = value;

                // Binding frissítése
                RaisePropertyChanged( nameof(Urlap) );
            }
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the UrlapViewModel class.
        /// </summary>
        public AltalanosUrlapPanelModel( Urlap urlap )
        {
            this.Urlap = urlap;

            FeliratkozasUrlapValtasEsemenyre();
        }


        private void FeliratkozasUrlapValtasEsemenyre()
        {
            Messenger.Default.Register<UrlapChanged>( this, UrlapChangedHandler );
        }

        private void UrlapChangedHandler( UrlapChanged uc )
        {
            Urlap = uc.Urlap;
        }

        #region IClosable Members

        public bool Close()
        {
            LeiratkozasUrlapValtasEsemenyrol();

            // jelezzük, hogy bezárásra kerülünk
            Messenger.Default.Send<Messages.AltalanosUrlapClosed>( new Messages.AltalanosUrlapClosed() );

            return true;
        }

        private void LeiratkozasUrlapValtasEsemenyrol()
        {
            Messenger.Default.Unregister<UrlapChanged>( this, UrlapChangedHandler );
        }

        public bool IsCloseEnabled()
        {
            return true;
        }

        #endregion

        public bool IsStatuszEnabled()
        {
            return false;
        }
    }
}
