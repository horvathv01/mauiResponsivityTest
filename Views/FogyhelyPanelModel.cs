using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Geometria.GeoMobil.Client.UI.Dialogs;
using Geometria.MirtuszMobil.Client.Messages;
using Geometria.MirtuszMobil.Client.Properties;
using Geometria.MirtuszMobil.Client.Storages;
using Geometria.MirtuszMobil.Client.Storages.Tables;
using Geometria.MirtuszMobil.Client.Views;
using Geometria.MirtuszService.MessageClasses;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    public class FogyhelyPanelModel : INotifyPropertyChanged
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

        #region Init

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

        internal void Close()
        {
            SelectedPecsetek.CollectionChanged -= SelectedPecsetek_Changed;
        }
        #endregion

        #region MezoIsReadOnly
        public bool IsMezoReadOnly { get; set; }
        #endregion

        #region Pecsetek
        public bool IsPecsetLeszerelheto
        {
            get
            {
                if( SelectedPecsetek == null || SelectedPecsetek.Count == 0)
                    return false;
                
                bool leszerlehto = true;
                foreach ( var pecset in SelectedPecsetek )
                {
                    if( pecset.Leszerelt)
                    {
                        leszerlehto = false;
                    }
                    else if ( !pecset.IsSzerverrol && !pecset.Leszerelt ) // Újonnan felszerelt pecsétet csak visszavonni lehessen, ha eltörött ne menjen el az SAP -ba
                    {
                        leszerlehto = false;
                    }

                }
                return leszerlehto && Munkautasitas.IsFolyamatban;
            }
        }

        public bool IsPecsetTorolheto
        {
            get
            {
                if (SelectedPecsetek == null || SelectedPecsetek.Count == 0)
                    return false;

                bool torolheto = true;
                foreach (var pecset in SelectedPecsetek)
                {
                    if( pecset.IsSzerverrol || pecset.Leszerelt)
                    {
                        torolheto = false;
                    }
                }
                return torolheto && Munkautasitas.IsFolyamatban;
            }
        }
        #endregion

        #region SelectedPecsetek

        private ObservableCollection<RotaciosPecset> m_SelectedPecsetek;

        public ObservableCollection<RotaciosPecset> SelectedPecsetek
        {
            get
            {
                return m_SelectedPecsetek;
            }

            set
            {
                if( m_SelectedPecsetek == value )
                    return;

                m_SelectedPecsetek = value;
            }
        }

        private void SelectedPecsetek_Changed(object sender, NotifyCollectionChangedEventArgs e)
        {
            RaisePecsetProperties();
        }

        #endregion

        #region VanPecsetKivalasztva
        public bool VanPecsetKivalasztva
        {
            get
            {
                return SelectedPecsetek != null;
            }
        }

        #endregion

        #region KeszulekElhelyezkedesek & SzekrenyTipusok
        public List<KoSapRecord> KeszulekElhelyezkedesek { get; set; }

        public List<KoSapRecord> SzekrenyTipusok { get; set; }

        public string SelectedKeszulekElhelyezkedesId
        {
            get
            {
                if( Munkautasitas.FogyasztasiHelyAdatai == null )
                {
                    return null;
                }
                if( !string.IsNullOrEmpty( Munkautasitas.FogyasztasiHelyAdatai.KeszulekElhelyezkedesSapCode )
                        && KeszulekElhelyezkedesek != null
                        && !KeszulekElhelyezkedesek.Any( t => t.Megnevezes == Munkautasitas.FogyasztasiHelyAdatai.KeszulekElhelyezkedesSapCode ) )
                {
                    KeszulekElhelyezkedesek.Add( new KoSapRecord
                    {
                        Azonosito = int.MaxValue,
                        Megnevezes = Munkautasitas.FogyasztasiHelyAdatai.KeszulekElhelyezkedesSapCode,
                        SapCode = Munkautasitas.FogyasztasiHelyAdatai.KeszulekElhelyezkedesSapCode,
                        SapOszlopNev = "KESZULEK_ELHELYEZKEDES"
                    } );
                }
                return SAPCodeFeloldo.GetErtek( "KESZULEK_ELHELYEZKEDES", Munkautasitas.FogyasztasiHelyAdatai.KeszulekElhelyezkedesSapCode ) == null ?
                    Munkautasitas.FogyasztasiHelyAdatai.KeszulekElhelyezkedesSapCode
                    : SAPCodeFeloldo.GetErtek( "KESZULEK_ELHELYEZKEDES", Munkautasitas.FogyasztasiHelyAdatai.KeszulekElhelyezkedesSapCode ).SapCode;
            }
            set
            {
                if( Munkautasitas.FogyasztasiHelyAdatai.KeszulekElhelyezkedesSapCode != null
                    && Munkautasitas.FogyasztasiHelyAdatai.KeszulekElhelyezkedesSapCode == value )
                {
                    return;
                }
                Munkautasitas.FogyasztasiHelyAdatai.KeszulekElhelyezkedesSapCode = value;
                RaisePropertyChanged( nameof( SelectedKeszulekElhelyezkedesId ) );
            }
        }

        public string SelectedKeszulekElhelyezkedes
        {
            get
            {
                return SAPCodeFeloldo.GetErtek( "KESZULEK_ELHELYEZKEDES", Munkautasitas.FogyasztasiHelyAdatai.KeszulekElhelyezkedesSapCode ) == null ? null :
                       SAPCodeFeloldo.GetErtek( "KESZULEK_ELHELYEZKEDES", Munkautasitas.FogyasztasiHelyAdatai.KeszulekElhelyezkedesSapCode ).Megnevezes;
            }
        }

        public string SelectedSzekrenyTipusId
        {
            get
            {
                if( Munkautasitas.FogyasztasiHelyAdatai == null )
                {
                    return null;
                }
                if ( !string.IsNullOrEmpty( Munkautasitas.FogyasztasiHelyAdatai.SzekrenyTipusSapCode ) 
                    && SzekrenyTipusok != null
                    && !SzekrenyTipusok.Any( t => t.Megnevezes == Munkautasitas.FogyasztasiHelyAdatai.SzekrenyTipusSapCode ) )
                {
                    SzekrenyTipusok.Add( new KoSapRecord
                    {
                        Azonosito = int.MaxValue,
                        Megnevezes = Munkautasitas.FogyasztasiHelyAdatai.SzekrenyTipusSapCode,
                        SapCode = Munkautasitas.FogyasztasiHelyAdatai.SzekrenyTipusSapCode,
                        SapOszlopNev = "SZEKRENY_TIPUS"
                    } );
                }
                return SAPCodeFeloldo.GetErtek( "SZEKRENY_TIPUS", Munkautasitas.FogyasztasiHelyAdatai.SzekrenyTipusSapCode ) == null ?
                      Munkautasitas.FogyasztasiHelyAdatai.SzekrenyTipusSapCode
                    : SAPCodeFeloldo.GetErtek( "SZEKRENY_TIPUS", Munkautasitas.FogyasztasiHelyAdatai.SzekrenyTipusSapCode ).SapCode;
            }
            set
            {
                if( Munkautasitas.FogyasztasiHelyAdatai.SzekrenyTipusSapCode != null
                    && Munkautasitas.FogyasztasiHelyAdatai.SzekrenyTipusSapCode == value )
                {
                    return;
                }
                Munkautasitas.FogyasztasiHelyAdatai.SzekrenyTipusSapCode = value;
                RaisePropertyChanged( nameof( SelectedSzekrenyTipusId ) );
            }
        }

        public string SelectedSzekrenyTipus
        {
            get
            {
                return SAPCodeFeloldo.GetErtek( "SZEKRENY_TIPUS", Munkautasitas.FogyasztasiHelyAdatai.SzekrenyTipusSapCode ) == null ?
                    Munkautasitas.FogyasztasiHelyAdatai.SzekrenyTipusSapCode
                    : SAPCodeFeloldo.GetErtek( "SZEKRENY_TIPUS", Munkautasitas.FogyasztasiHelyAdatai.SzekrenyTipusSapCode ).Megnevezes;
            }
        }
        #endregion



        public FogyhelyPanelModel( Munkautasitas munka )
        {
            Munkautasitas = munka;
            KeszulekElhelyezkedesek = SAPCodeFeloldo.GetErtekek( "KESZULEK_ELHELYEZKEDES" );
            SzekrenyTipusok = SAPCodeFeloldo.GetErtekek( "SZEKRENY_TIPUS" );
            IsMezoReadOnly = true;

            Messenger.Default.Register<RaktarMessage>( this, RaktarMessage_Handler );
            SelectedPecsetek = new ObservableCollection<RotaciosPecset>();
            SelectedPecsetek.CollectionChanged += SelectedPecsetek_Changed;
        }


        #endregion

        #region Commands

        #region UndoPecsetCommand

        private RelayCommand m_UndoPecsetCommand;

        public RelayCommand UndoPecsetCommand
        {
            get
            {
                if( m_UndoPecsetCommand == null )
                    m_UndoPecsetCommand = new RelayCommand( UndoPecsetCommandCall );
                return m_UndoPecsetCommand;
            }
        }

        public void UndoPecsetCommandCall()
        {
            if( SelectedPecsetek != null )
            {
                for( int i = SelectedPecsetek.Count - 1; i >= 0; i-- )
                {
                    var pecset = SelectedPecsetek[i];

                    if( pecset.IsSzerverrol )
                    {
                        MessageBoxWpf.Show( DisplayResources.KijeloltElemNemTorolheto, DisplayResources.Figyelmeztetes, MessageBoxButton.OK, MessageBoxImage.Information );
                        return;
                    }

                    var anyag = Storage.Instance.Session.FelszereltPecsetek.Where( e => e.GyariSzam.Equals( pecset.Szama ) )
                                                                           .FirstOrDefault();
                    if( anyag != null )
                    {
                        anyag.FelhasznaltMennyiseg = 0;
                        var elszamoltPecset = Munkautasitas.ElszamoltAnyagok.Where( a => a.GyariSzam.Equals( anyag.GyariSzam ) )
                                                                            .FirstOrDefault();
                        if( elszamoltPecset != null )
                            Munkautasitas.ElszamoltAnyagok.Remove( elszamoltPecset );
                        Munkautasitas.ElszamoltPecsetek.Remove( pecset );
                        Storage.Instance.Session.FelszereltPecsetek.Remove( anyag );
                    }
                    else
                        App.Logger.Trace( "Anyagelszámolás listából nem lehetett törlöni, mert az nem található az anyag." );
                }
            }
        }

        #endregion

        #region AddPecsetCommand

        private RelayCommand m_AddPecsetCommand;
        public RelayCommand AddPecsetCommand
        {
            get
            {
                if( m_AddPecsetCommand == null )
                    m_AddPecsetCommand = new RelayCommand( AddPecsetCommandCall, CanExecuteAnyagElszam );
                return m_AddPecsetCommand;
            }
        }

        private void AddPecsetCommandCall()
        {
            ViewManager.Instance.Open( new AnyagElszamView( Munkautasitas, isModosithato: false, pecset: true ) );
        }

        private bool CanExecuteAnyagElszam()
        {
            return Storage.Instance.Session.RaktariAnyagok.Count > 0;
        }

        #endregion

        #region LeszerelCommand

        private RelayCommand m_DismountPecsetCommand;

        public RelayCommand DismountPecsetCommand
        {
            get
            {
                if( m_DismountPecsetCommand == null )
                    m_DismountPecsetCommand = new RelayCommand( DismountPecsetCommandCall );
                return m_DismountPecsetCommand;
            }
        }

        private void DismountPecsetCommandCall()
        {
            if ( MessageBoxWpf.Show( DisplayResources.PlombaLeszerelFigyelem, DisplayResources.Figyelmeztetes, MessageBoxButton.YesNo, MessageBoxImage.Exclamation ) == MessageBoxResult.No )
                return;

            foreach ( var pecset in SelectedPecsetek )
            {
                pecset.Leszerelt = true;
            } 
            RaisePecsetProperties();
        }

        #endregion

        #endregion

        #region Helper Methods

        private void RaisePecsetProperties()
        {
            RaisePropertyChanged( nameof( SelectedPecsetek ) );
            RaisePropertyChanged( nameof( VanPecsetKivalasztva ) );
            RaisePropertyChanged( nameof( IsPecsetLeszerelheto ) );
            RaisePropertyChanged( nameof( IsPecsetTorolheto ) );
        }

        private void RaktarMessage_Handler( RaktarMessage obj )
        {
            AddPecsetCommand.RaiseCanExecuteChanged();
            Messenger.Default.Unregister<RaktarMessage>( this );
        }

        #endregion
    }
}
