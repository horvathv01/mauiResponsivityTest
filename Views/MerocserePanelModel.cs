using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Geometria.MirtuszMobil.Client.Controls;
using Geometria.MirtuszMobil.Client.Dialogs;
using Geometria.MirtuszMobil.Client.HelperClasses;
using Geometria.MirtuszMobil.Client.Messages;
using Geometria.MirtuszMobil.Client.Properties;
using Geometria.MirtuszMobil.Client.Storages;
using Geometria.MirtuszMobil.Client.Storages.Tables;
using Geometria.MirtuszMobil.Client.Views;
using Geometria.GeoMobil.Client.UI.Dialogs;
using Geometria.MirtuszMobil.Common.Messages;
using Geometria.MirtuszService.MessageClasses;
using Geometria.MirtuszService.MessageClasses.CodeValues;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using Geometria.MirtuszMobil.Common.HelperClasses;
using Geometria.MirtuszMobil.Common.Converters;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    public class MerocserePanelModel : INotifyPropertyChanged
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
        #endregion

        public Berendezes UjBerendezes { get; set; }

        List<Berendezes> KimentettBerendezesek { get; set; }

        public Berendezes BerendezesToCsere { get; set; }

        Berendezes parentMeroBerendezes = null;

        MeroBerendezesMuveletekHelper BerendMuveletekHelper { get; set; }

        public ObservableCollection<Berendezes> MeroBerendezesek { get; set; }

        private List<KoMerohBerTipusRecord> RaktariAnyagFajtak;

        public List<Berendezes> MerokeszulekekForOnalloBerendezes { get; set; }

        public Merokeszulek KivalasztottMerokeszulekForOnalloBerendezes { get; set; }

        // Touch esetnén a mérőórra ellenőrzés lost focusra megjelenő messageboxra nem lehtett csak egérrel nyomni
        private DispatcherTimer m_LostFocusHackTimer = new DispatcherTimer();

        public MerocserePanelModel( Munkautasitas munkautasitas )
        {
            Munkautasitas = munkautasitas;
            RaktariAnyagFajtak = StoredTableHelper.Instance.GetRecord<KoMerohBerTipusRecord>() != null ? StoredTableHelper.Instance.GetRecord<KoMerohBerTipusRecord>().ToList() : null;

            // Mérőcsere helper methods
            InitMerocsere();
            FillMeroBerendezesTulajdonsagok();
            BerendMuveletekHelper = new MeroBerendezesMuveletekHelper();
            Messenger.Default.Register<KapcsolodoFenykepekFrissitesMessage>(this, KapcsolodoFenykepekFrissites_Handler);

            m_LostFocusHackTimer.Interval = new TimeSpan( 0, 0, 0, 0, 500 );
            m_LostFocusHackTimer.Tick += M_LostFocusHackTimer_Tick;


        }

    #endregion

    #region Commands

    #region BerendezesLeszerelCommand

    private RelayCommand<Berendezes> m_BerendLeszerelCommand;

        public RelayCommand<Berendezes> BerendLeszerelCommand
        {
            get
            {
                if( m_BerendLeszerelCommand == null )
                    m_BerendLeszerelCommand = new RelayCommand<Berendezes>( BerendLeszerelCommandCall );
                return m_BerendLeszerelCommand;
            }
        }

        private void BerendLeszerelCommandCall( Berendezes berend )
        {
            if( berend.Berendezesek.Count > 0 && berend.Berendezesek.Any( b => b.Leszerelt != true && !(b is BerendezesSzamlalo) ) )
            {
                // Elsötétítjük a többi contentet
                ViewModelLocator.MainStatic.DarkenUnfocusedArea = true;

                MessageBoxWpf.Show( DisplayResources.NemLehetMeroLeszereles, DisplayResources.Figyelmeztetes, MessageBoxButton.OK, MessageBoxImage.Warning );
            }
            else
            {
                // Elsötétítjük a többi contentet
                ViewModelLocator.MainStatic.DarkenUnfocusedArea = true;

                if( MessageBoxWpf.Show( DisplayResources.MeroLeszerelWarning, DisplayResources.Figyelmeztetes, MessageBoxButton.YesNo, MessageBoxImage.Warning ) == MessageBoxResult.Yes )
                {
                    var merokeszulek = berend as Merokeszulek;
                    if ( merokeszulek != null )
                    {
                        var szamlaloErtekekIsModified = GetIsSzamlaloErtekekModified( merokeszulek );
                        if( !szamlaloErtekekIsModified )
                        {
                            MessageBoxWpf.Show( DisplayResources.MeroCsereModositatlanSzamlaloWarning, DisplayResources.Figyelmeztetes, MessageBoxButton.OK, MessageBoxImage.Warning );
                        }
                        else
                        {
                            BerendLeszerel( berend );
                        }
                    }
                    else
                    {
                        BerendLeszerel( berend );
                    }
                    // Visszaállítjuk az elsötétített részt
                    ViewModelLocator.MainStatic.DarkenUnfocusedArea = false;
                }
            }

            // Visszaállítjuk az elsötétített részt
            ViewModelLocator.MainStatic.DarkenUnfocusedArea = false;
        }


        #endregion

        #region BerendFelszerelCommand

        private RelayCommand<object[]> m_BerendFelszerelCommand;

        public RelayCommand<object[]> BerendFelszerelCommand
        {
            get
            {
                if( m_BerendFelszerelCommand == null )
                {
                    m_BerendFelszerelCommand = new RelayCommand<object[]>( BerendFelszerelCommandCall );
                }
                return m_BerendFelszerelCommand;
            }
        }

        private void BerendFelszerelCommandCall( object[] parameters )
        {
            Berendezes berend = parameters == null ? null : parameters[0] as Berendezes;

            // Beállítjuk az AnyagElszamView-hoz szükséges paramétereket
            BerendMuveletekHelper.MeroBerendezes    = berend;
            BerendMuveletekHelper.IsBerendezesCsere = false;

            ResetKismegszFlags();

            if( berend == null )
            {
                Messenger.Default.Register<AddBerendezesMunka>( this, Add_MerokeszulekToMunka );
                BerendMuveletekHelper.IsMerokeszulek = true;
                BerendMuveletekHelper.IsBerendezes   = false;
                ViewManager.Instance.Open( new AnyagElszamView( Munkautasitas, BerendMuveletekHelper, false ) );
                Messenger.Default.Register<CallUnregistersMerocseren>( this, CallUnregisters );
                Messenger.Default.Send<BerendCollectionChanged>( new BerendCollectionChanged( berend, null ) );
            }
            else if( berend is Merokeszulek )
            {
                Messenger.Default.Register<AddBerendezesMunka>( this, Add_MeroBerendezesToMunka );
                BerendMuveletekHelper.IsBerendezes   = true;
                BerendMuveletekHelper.IsMerokeszulek = false;

                ViewManager.Instance.Open( new AnyagElszamView( Munkautasitas, BerendMuveletekHelper, false ) );
                Messenger.Default.Register<CallUnregistersMerocseren>( this, CallUnregisters );
                Messenger.Default.Send<BerendCollectionChanged>( new BerendCollectionChanged( berend, null ) );

            }

            ViewModelLocator.MainStatic.DarkenUnfocusedArea = false;
        }

        #endregion

        #region BerendCsereCommand

        private RelayCommand<Berendezes> m_BerendCsereCommand;

        public RelayCommand<Berendezes> BerendCsereCommand
        {
            get
            {
                if( m_BerendCsereCommand == null )
                {
                    m_BerendCsereCommand = new RelayCommand<Berendezes>( BerendCsereCommandCall );
                }
                return m_BerendCsereCommand;
            }
        }

        private void BerendCsereCommandCall( Berendezes berend )
        {
            // Elsötétítjük a többi contentet
            ViewModelLocator.MainStatic.DarkenUnfocusedArea = true;

            if( MessageBoxWpf.Show( DisplayResources.MeroCsereWarning, DisplayResources.Figyelmeztetes, MessageBoxButton.YesNo, MessageBoxImage.Warning ) == MessageBoxResult.Yes )
            {
                if( berend is Merokeszulek )
                {
                    // Megnézzük van-e módosítatlan számláló érték ennél a mérőkészüléknél
                    var szamlaloErtekekIsModified = GetIsSzamlaloErtekekModified( berend as Merokeszulek );
                    if( !szamlaloErtekekIsModified )
                    {
                        MessageBoxWpf.Show( DisplayResources.MeroCsereModositatlanSzamlaloWarning, DisplayResources.Figyelmeztetes, MessageBoxButton.OK, MessageBoxImage.Warning );
                        // Visszaállítjuk az elsötétített részt
                        ViewModelLocator.MainStatic.DarkenUnfocusedArea = false;
                        return;
                    }
                    else
                    {
                        Messenger.Default.Register<AddBerendezesMunka>( this, Add_MerokeszulekToMunka );
                        PrepareToReplaceMerokeszulek( berend );
                        ViewManager.Instance.Open( new AnyagElszamView( Munkautasitas, BerendMuveletekHelper, false ) );
                        Messenger.Default.Register<CallUnregistersMerocseren>( this, CallUnregisters );

                        // Visszaállítjuk az elsötétített részt
                        ViewModelLocator.MainStatic.DarkenUnfocusedArea = false;
                    }
                }
                else if( berend is HKV || berend is Modem || berend is Aramvalto )
                {
                    
                    Messenger.Default.Register<AddBerendezesMunka>( this, Add_MeroBerendezesToMunka );
                    PrepareToReplaceMeroberendezes( berend );
                    BerendMuveletekHelper.IsBerendezes = true;
                    ViewManager.Instance.Open( new AnyagElszamView( Munkautasitas, BerendMuveletekHelper, false ) );
                    Messenger.Default.Register<CallUnregistersMerocseren>( this, CallUnregisters );

                    // Visszaállítjuk az elsötétített részt
                    ViewModelLocator.MainStatic.DarkenUnfocusedArea = false;
                }
            }
            // Visszaállítjuk az elsötétített részt
            ViewModelLocator.MainStatic.DarkenUnfocusedArea = false;
        }

        #endregion

        #region BerendVisszaAllitasCommand

        private RelayCommand<Berendezes> m_BerendVisszaAllitasCommand;

        public RelayCommand<Berendezes> BerendVisszaAllitasCommand
        {
            get
            {
                if( m_BerendVisszaAllitasCommand == null )
                {
                    m_BerendVisszaAllitasCommand = new RelayCommand<Berendezes>( BerendVisszaAllitasCommandCall );
                }
                return m_BerendVisszaAllitasCommand;
            }
        }

        private void BerendVisszaAllitasCommandCall( Berendezes berend )
        {
            // Elsötétítjük a többi contentet
            ViewModelLocator.MainStatic.DarkenUnfocusedArea = true;

            if( MessageBoxWpf.Show( DisplayResources.MeroReset, DisplayResources.Figyelmeztetes, MessageBoxButton.YesNo, MessageBoxImage.Warning ) == MessageBoxResult.Yes )
            {
                var kapcsolodoFenykepek = Munkautasitas.Dokumentumok.Any( f => f.MerohberAzonosito != null );
                if( kapcsolodoFenykepek )
                {
                    MessageBoxWpf.Show( "A visszaállítás nem törli az elkészített fényképeket. Javasoljuk töröld a feleslegessé vált képeket a dokumentumok közül.", DisplayResources.Figyelmeztetes, MessageBoxButton.OK, MessageBoxImage.Warning );
                }
                MeroAdatokModositasVisszaallitas();
            }
            else
            {
                // Visszaállítjuk az elsötétített részt
                ViewModelLocator.MainStatic.DarkenUnfocusedArea = false;

                return;
            }

        }

        #endregion

        #region KismegszRFelszerelCommand

        private RelayCommand<Berendezes> m_KismegszRFelszerelCommand;

        public RelayCommand<Berendezes> KismegszRFelszerelCommand
        {
            get
            {
                if( m_KismegszRFelszerelCommand == null )
                {
                    m_KismegszRFelszerelCommand = new RelayCommand<Berendezes>( KismegszRFelszerelCommandCall );
                }
                return m_KismegszRFelszerelCommand;
            }
        }

        private void KismegszRFelszerelCommandCall( Berendezes berend )
        {
            Messenger.Default.Register<AddBerendezesMunka>( this, Add_KismegszToMunka );

            // Beállítjuk az AnyagElszamView-hoz szükséges paramétereket
            KeszletSpinTextBoxHelper.KismegszRSTAdding      = true;
            KeszletSpinTextBoxHelper.KismegszOsszAdding     = false;
            BerendMuveletekHelper.MeroBerendezes            = berend;
            BerendMuveletekHelper.IsKismegszRFelszereles    = true;
            BerendMuveletekHelper.IsKismegszSFelszereles    = false;
            BerendMuveletekHelper.IsKismegszTFelszereles    = false;
            BerendMuveletekHelper.IsKismegszOsszFelszereles = false;

            ViewManager.Instance.Open( new AnyagElszamView( Munkautasitas, BerendMuveletekHelper ) );
            Messenger.Default.Register<CallUnregistersMerocseren>( this, CallUnregisters );
        }

        #endregion

        #region KismegszSFelszerelCommand

        private RelayCommand<Berendezes> m_KismegszSFelszerelCommand;

        public RelayCommand<Berendezes> KismegszSFelszerelCommand
        {
            get
            {
                if( m_KismegszSFelszerelCommand == null )
                {
                    m_KismegszSFelszerelCommand = new RelayCommand<Berendezes>( KismegszSFelszerelCommandCall );
                }
                return m_KismegszSFelszerelCommand;
            }
        }

        private void KismegszSFelszerelCommandCall( Berendezes berend )
        {
            Messenger.Default.Register<AddBerendezesMunka>( this, Add_KismegszToMunka );

            // Beállítjuk az AnyagElszamView-hoz szükséges paramétereket
            KeszletSpinTextBoxHelper.KismegszRSTAdding      = true;
            KeszletSpinTextBoxHelper.KismegszOsszAdding     = false;
            BerendMuveletekHelper.MeroBerendezes            = berend;
            BerendMuveletekHelper.IsKismegszRFelszereles    = false;
            BerendMuveletekHelper.IsKismegszSFelszereles    = true;
            BerendMuveletekHelper.IsKismegszTFelszereles    = false;
            BerendMuveletekHelper.IsKismegszOsszFelszereles = false;

            ViewManager.Instance.Open( new AnyagElszamView( Munkautasitas, BerendMuveletekHelper ) );
            Messenger.Default.Register<CallUnregistersMerocseren>( this, CallUnregisters );
        }

        #endregion

        #region KismegszTFelszerelCommand

        private RelayCommand<Berendezes> m_KismegszTFelszerelCommand;

        public RelayCommand<Berendezes> KismegszTFelszerelCommand
        {
            get
            {
                if( m_KismegszTFelszerelCommand == null )
                {
                    m_KismegszTFelszerelCommand = new RelayCommand<Berendezes>( KismegszTFelszerelCommandCall );
                }
                return m_KismegszTFelszerelCommand;
            }
        }

        private void KismegszTFelszerelCommandCall( Berendezes berend )
        {
            Messenger.Default.Register<AddBerendezesMunka>( this, Add_KismegszToMunka );

            // Beállítjuk az AnyagElszamView-hoz szükséges paramétereket
            KeszletSpinTextBoxHelper.KismegszRSTAdding      = true;
            KeszletSpinTextBoxHelper.KismegszOsszAdding     = false;
            BerendMuveletekHelper.MeroBerendezes            = berend;
            BerendMuveletekHelper.IsKismegszRFelszereles    = false;
            BerendMuveletekHelper.IsKismegszSFelszereles    = false;
            BerendMuveletekHelper.IsKismegszTFelszereles    = true;
            BerendMuveletekHelper.IsKismegszOsszFelszereles = false;

            ViewManager.Instance.Open( new AnyagElszamView( Munkautasitas, BerendMuveletekHelper ) );
            Messenger.Default.Register<CallUnregistersMerocseren>( this, CallUnregisters );
        }

        #endregion

        #region KismegszOsszFelszerelCommand

        private RelayCommand<Berendezes> m_KismegszOsszFelszerelCommand;

        public RelayCommand<Berendezes> KismegszOsszFelszerelCommand
        {
            get
            {
                if( m_KismegszOsszFelszerelCommand == null )
                {
                    m_KismegszOsszFelszerelCommand = new RelayCommand<Berendezes>( KismegszOsszFelszerelCommandCall );
                }
                return m_KismegszOsszFelszerelCommand;
            }
        }

        private void KismegszOsszFelszerelCommandCall( Berendezes berend )
        {
            Messenger.Default.Register<AddBerendezesMunka>( this, Add_KismegszToMunka );

            // Beállítjuk az AnyagElszamView-hoz szükséges paramétereket
            KeszletSpinTextBoxHelper.KismegszOsszAdding     = true;
            KeszletSpinTextBoxHelper.KismegszRSTAdding      = false;
            BerendMuveletekHelper.MeroBerendezes            = berend;
            BerendMuveletekHelper.IsMerokeszulek            = false;
            BerendMuveletekHelper.IsBerendezesCsere         = false;
            BerendMuveletekHelper.IsBerendezes              = false;
            BerendMuveletekHelper.IsKismegszRFelszereles    = false;
            BerendMuveletekHelper.IsKismegszSFelszereles    = false;
            BerendMuveletekHelper.IsKismegszTFelszereles    = false;
            BerendMuveletekHelper.IsKismegszOsszFelszereles = true;

            ViewManager.Instance.Open( new AnyagElszamView( Munkautasitas, BerendMuveletekHelper ) );
            Messenger.Default.Register<CallUnregistersMerocseren>( this, CallUnregisters );
        }

        #endregion

        #region SzamlaloFelszerelCommand

        private RelayCommand<object[]> m_SzamlaloFelszerelCommand;

        public RelayCommand<object[]> SzamlaloFelszerelCommand
        {
            get
            {
                if( m_SzamlaloFelszerelCommand == null )
                {
                    m_SzamlaloFelszerelCommand = new RelayCommand<object[]>( SzamlaloFelszerelCommandCall );
                }
                return m_SzamlaloFelszerelCommand;
            }
        }

        private void SzamlaloFelszerelCommandCall( object[] parameters )
        {
            var berend = parameters[0] as Berendezes;

            if( berend != null && berend is Merokeszulek )
            {
                var lehetsegesHkv = berend.Berendezesek.Where( b => b is HKV && b.Statusz == KoFhBerendezesStatusz.FELSZERELT ).FirstOrDefault();

                var ujSzamlalo = new BerendezesSzamlalo
                {
                    MeroKeszulekAzon = berend.Azonosito,
                    Leszerelt        = false,
                    Statusz          = KoFhBerendezesStatusz.FELSZERELT,
                    Tipus            = "Számláló",
                    UjFelszerelt     = true,
                    Vezerlo          = lehetsegesHkv == null ? "" : lehetsegesHkv.ISUAzonosito,
                    MeroallasEderet  = MeroallaEredetSapCode.SZERELOI
                };

                berend.Berendezesek.Add( ujSzamlalo );
                FillMeroBerendezesTulajdonsagok();
                Messenger.Default.Send<BerendCollectionChanged>( new BerendCollectionChanged( berend, ujSzamlalo ) );
                App.Logger.Info( string.Format( "{0} ISU számú mérőkészülékre új számláló felszerelve", berend.ISUAzonosito ) );
            }
        }

        #endregion

        #region ModemTorolCommand

        private RelayCommand<Berendezes> m_ModemTorolCommand;

        public RelayCommand<Berendezes> ModemTorolCommand
        {
            get
            {
                if( m_ModemTorolCommand == null )
                {
                    m_ModemTorolCommand = new RelayCommand<Berendezes>( ModemTorolCommandCall );
                }
                return m_ModemTorolCommand;
            }
        }

        private void ModemTorolCommandCall( Berendezes berend )
        {
            ViewModelLocator.MainStatic.DarkenUnfocusedArea = true;

            if( !berend.UjFelszerelt )
            {
                MessageBoxWpf.Show( DisplayResources.MeroNemLeszerelhetoWarning, DisplayResources.Figyelmeztetes, MessageBoxButton.OK, MessageBoxImage.Warning );
                ViewModelLocator.MainStatic.DarkenUnfocusedArea = false;
                return;
            }
            

            if( MessageBoxWpf.Show( DisplayResources.FelszerelVisszavon, DisplayResources.Figyelmeztetes, MessageBoxButton.YesNo, MessageBoxImage.Warning ) == MessageBoxResult.Yes )
            {
                Berendezes berendezes = berend;

                var parentMero = MeroBerendezesek.Where( b => b.Berendezesek.Contains( berendezes ) ).FirstOrDefault();

                if( berendezes != null && parentMero != null )
                {
                    parentMero.Berendezesek.Remove( berendezes );
                }

                var anyag = Storage.Instance.Session.AnyagElszamolasok.Where( e => e.GyariSzam.Equals( berendezes.GyariSzam ) || e.GyariSzam.Equals( berendezes.ISUAzonosito ) )
                                                                      .FirstOrDefault();

                if( anyag != null )
                {
                    anyag.FelhasznaltMennyiseg = 0;
                    var elszamoltAnyag = Munkautasitas.ElszamoltAnyagok.Where( a => a.GyariSzam.Equals( anyag.GyariSzam ) ).FirstOrDefault();
                    if( elszamoltAnyag != null )
                        Munkautasitas.ElszamoltAnyagok.Remove( elszamoltAnyag );
                }
                else
                    App.Logger.Trace( "Anyagelszámolás listából nem lehetett törölni, mert nem található az anyag." );
            }

            ViewModelLocator.MainStatic.DarkenUnfocusedArea = false;
        }

        #endregion

        #region SzamlaloTorolCommand

        private RelayCommand<Berendezes> m_SzamlaloTorolCommand;

        public RelayCommand<Berendezes> SzamlaloTorolCommand
        {
            get
            {
                if( m_SzamlaloTorolCommand == null )
                {
                    m_SzamlaloTorolCommand = new RelayCommand<Berendezes>( SzamlaloTorolCommandCall );
                }
                return m_SzamlaloTorolCommand;
            }
        }

        private void SzamlaloTorolCommandCall( Berendezes berend )
        {
            ViewModelLocator.MainStatic.DarkenUnfocusedArea = true;

            if( MessageBoxWpf.Show( DisplayResources.SzamlaloLeszerelesWarning, DisplayResources.Figyelmeztetes, MessageBoxButton.YesNo, MessageBoxImage.Warning ) == MessageBoxResult.Yes )
            {

                var szamlalo = berend as BerendezesSzamlalo;
                var parentMero = MeroBerendezesek.Where( b => b.Berendezesek.Contains( szamlalo ) ).FirstOrDefault();

                if( szamlalo != null && parentMero != null )
                {
                    parentMero.Berendezesek.Remove( szamlalo );
                }
            }

            ViewModelLocator.MainStatic.DarkenUnfocusedArea = false;
        }

        #endregion

        #region PlombaAndZarofoliaResetCommand

        private RelayCommand<Berendezes> m_PlombaAndZarofoliaResetCommand;

        public RelayCommand<Berendezes> PlombaAndZarofoliaResetCommand
        {
            get
            {
                if( m_PlombaAndZarofoliaResetCommand == null )
                {
                    m_PlombaAndZarofoliaResetCommand = new RelayCommand<Berendezes>( PlombaAndZarofoliaResetCommandCall );
                }
                return m_PlombaAndZarofoliaResetCommand;
            }
        }

        private void PlombaAndZarofoliaResetCommandCall( Berendezes berend )
        {
            if( berend is Merokeszulek )
            {
                var merokeszulek         = berend as Merokeszulek;
                merokeszulek.PlombaDarab = null;
                merokeszulek.PlombaSzam  = null;
            }
            else if ( berend is HKV )
            {
                var hkv         = berend as HKV;
                hkv.PlombaDarab = null;
                hkv.PlombaSzam  = null;
            }
        }

        #endregion

        #region SzerelToMero

        private RelayCommand<Berendezes> m_SzerelToMeroCommand;

        public RelayCommand<Berendezes> SzerelToMeroCommand
        {
            get
            {
                if( m_SzerelToMeroCommand == null )
                {
                    m_SzerelToMeroCommand = new RelayCommand<Berendezes>( SzerelToMeroCommandCall );
                }
                return m_SzerelToMeroCommand;
            }
        }

        private void SzerelToMeroCommandCall( Berendezes berend )
        {

            MerokeszulekekForOnalloBerendezes = new List<Berendezes>( Munkautasitas.Merohely.Merokeszulekek
                                                                     .Where( mero => mero.Statusz != KoFhBerendezesStatusz.NEM_VALTOZOTT_LESZERELT
                                                                                  && mero.Statusz != KoFhBerendezesStatusz.VALTOZOTT_LESZERELT
                                                                                  && mero.Statusz != KoFhBerendezesStatusz.FELLELT_LESZERELT ) );
            // Elsötétítjük a többi contentet
            ViewModelLocator.MainStatic.DarkenUnfocusedArea = true;

            var box = new MerokeszulekValasztoDialog( this );
            var dialog = box.Show( App.MainWindow, DisplayResources.ValaszthatoMerokeszulekek, MessageBoxButton.OKCancel );


            if( dialog == MessageBoxResult.OK )
            {
                if ( KivalasztottMerokeszulekForOnalloBerendezes != null )
                {

                    if( berend is Aramvalto )
                    {
                        var aramValto              = berend as Aramvalto;
                        aramValto.MerokeszulekAzon = KivalasztottMerokeszulekForOnalloBerendezes.Azonosito;
                        aramValto.Statusz          = KoFhBerendezesStatusz.FELSZERELT;
                        var merokeszulek           = Munkautasitas.Merohely.Merokeszulekek.Where( m => m.Azonosito == KivalasztottMerokeszulekForOnalloBerendezes.Azonosito ).FirstOrDefault();
                        merokeszulek?.Berendezesek.Add( aramValto );

                        Munkautasitas.Merohely.OnalloAramvaltok.Remove( aramValto );
                    }
                    else if ( berend is Modem )
                    {
                        var modem              = berend as Modem;
                        modem.MeroKeszulekAzon = KivalasztottMerokeszulekForOnalloBerendezes.Azonosito;
                        modem.Statusz          = KoFhBerendezesStatusz.FELSZERELT;
                        var merokeszulek       = Munkautasitas.Merohely.Merokeszulekek.Where( m => m.Azonosito == KivalasztottMerokeszulekForOnalloBerendezes.Azonosito ).FirstOrDefault();
                        merokeszulek?.Berendezesek.Add( modem );

                        Munkautasitas.Merohely.OnalloModemek.Remove( modem );
                    }

                    FillMeroBerendezesTulajdonsagok();
                }

                // Visszaállítjuk az elsötétített részt
                ViewModelLocator.MainStatic.DarkenUnfocusedArea = false;

                return;
            }
            else
            {
                // Visszaállítjuk az elsötétített részt
                ViewModelLocator.MainStatic.DarkenUnfocusedArea = false;
            }
        }

        #endregion

        #region MeroFenykepCsatolasCommand

        private RelayCommand<CsatolasParameter> m_MeroFenykepCsatolasCommand;

        public RelayCommand<CsatolasParameter> MeroFenykepCsatolasCommand
        {
            get
            {
                if( m_MeroFenykepCsatolasCommand == null )
                {
                    m_MeroFenykepCsatolasCommand = new RelayCommand<CsatolasParameter>( MeroFenykepCsatolasCommandCall );
                }
                return m_MeroFenykepCsatolasCommand;
            }
        }

        private void MeroFenykepCsatolasCommandCall( CsatolasParameter parameterek )
        {
            Munkautasitas.MeroForFenykepAttach = parameterek.Data as Merokeszulek;
            CsatolasWindow.CreateAndShowPopup( new CsatolasViewModel(), parameterek.ParentControl as FrameworkElement );
            MeroKapcsolodoFenykepek_CollectionChanged( parameterek.Data as Merokeszulek );
        }

        #endregion

        #region FenykepMegnyitoCommand

        private RelayCommand<DokumentumRecord> m_FenykepMegnyitoCommand;
        public RelayCommand<DokumentumRecord> FenykepMegnyitoCommand
        {
            get
            {
                if( m_FenykepMegnyitoCommand == null )
                    m_FenykepMegnyitoCommand = new RelayCommand<DokumentumRecord>( FenykepMegnyitoCommandCall );
                return m_FenykepMegnyitoCommand;
            }
        }

        private void FenykepMegnyitoCommandCall( DokumentumRecord fenykep )
        {
            if( File.Exists( fenykep.LocalPath ) )
            {
                try
                {
                    System.Diagnostics.Process.Start( fenykep.LocalPath );
                }
                catch( Exception ex )
                {
                    App.Logger.Warn( "Ismeretlen file tipus ", ex );
                    MessageBoxWpf.Show( DisplayResources.IsmeretlenFile + fenykep.FAJL_NEV );
                }
            }
            else
            {
                MessageBoxWpf.Show( string.Format( DisplayResources.FajlNemtalalhato, fenykep.LocalPath ) );
            }
        }
        #endregion

        #region MeroallasEredetupdateCommand

        private RelayCommand<BerendezesSzamlalo> m_SzamlaloMeroallasEredetUpdateCommand;

        public RelayCommand<BerendezesSzamlalo> SzamlaloMeroallasEredetUpdateCommand
        {
            get
            {
                if( m_SzamlaloMeroallasEredetUpdateCommand == null )
                    m_SzamlaloMeroallasEredetUpdateCommand = new RelayCommand<BerendezesSzamlalo>( SzamlaloMeroallasEredetUpdateCommandCall );
                return m_SzamlaloMeroallasEredetUpdateCommand;
            }
        }

        private void SzamlaloMeroallasEredetUpdateCommandCall( BerendezesSzamlalo szamlalo )
        {
            m_LostFocusHackTimer.Tag = szamlalo;
            m_LostFocusHackTimer.Start();
            
        }

        private void M_LostFocusHackTimer_Tick( object sender, EventArgs e )
        {
            m_LostFocusHackTimer.Stop();
            BerendezesSzamlalo szamlalo = m_LostFocusHackTimer.Tag as BerendezesSzamlalo;
            if ( szamlalo == null )
            {
                return;
            }

            var szamlaloMeroallasEredet = SAPCodeFeloldo.GetErtekek( "MEROALLAS_EREDET" );

            if ( szamlaloMeroallasEredet != null )
            {
                szamlalo.MeroallasEderet = szamlaloMeroallasEredet.Where( sz => sz.SapCode == "11" ).FirstOrDefault()?.SapCode;

                szamlalo.MeroAllasEredetDisplay = szamlalo.MeroallasEderet != null ?
                                                  szamlaloMeroallasEredet.Where( sz => sz.SapCode == szamlalo.MeroallasEderet ).FirstOrDefault()?.Megnevezes :
                                                  null;
            }

            if ( szamlalo.MeroallasErtek < szamlalo.EredetiMeroallasErtek )
            {
                // Elsötétítjük a többi contentet
                ViewModelLocator.MainStatic.DarkenUnfocusedArea = true;
                if ( MessageBoxWpf.Show( DisplayResources.KisebbMeroallasErtekWarning, DisplayResources.Figyelmeztetes, MessageBoxButton.YesNo, MessageBoxImage.Warning ) == MessageBoxResult.No )
                {
                    szamlalo.MeroallasErtek = szamlalo.EredetiMeroallasErtek;
                }
                // Elsötétítjük a többi contentet
                ViewModelLocator.MainStatic.DarkenUnfocusedArea = false;
            }
        }

        #endregion

        #region MerokeszulekTarifaFajtaUpdateCommand

        private RelayCommand<Merokeszulek> m_MerokeszulekTarifaFajtaUpdateCommand;

        public RelayCommand<Merokeszulek> MerokeszulekTarifaFajtaUpdateCommand
        {
            get
            {
                if( m_MerokeszulekTarifaFajtaUpdateCommand == null )
                    m_MerokeszulekTarifaFajtaUpdateCommand = new RelayCommand<Merokeszulek>( MerokeszulekTarifaFajtaUpdateCommandCall );
                return m_MerokeszulekTarifaFajtaUpdateCommand;
            }
        }

        private void MerokeszulekTarifaFajtaUpdateCommandCall( Merokeszulek mero )
        {
            mero.TarifaFajtaDisplay = GetMeroTarifafajta( mero );
        }

        #endregion

        #region HKVKodTorolCommand

        public RelayCommand<Berendezes> HKVKodTorolCommand
        {
            get
            {
                if( m_HKVKodTorolCommand == null )
                    m_HKVKodTorolCommand = new RelayCommand<Berendezes>( HKVKodTorolCommandCall );
                return m_HKVKodTorolCommand;
            }
        }
        private RelayCommand<Berendezes> m_HKVKodTorolCommand;

        private void HKVKodTorolCommandCall( Berendezes berend)
        {
            if( berend is BerendezesSzamlalo )
            {
                var Szamlalo = berend as BerendezesSzamlalo;
                Szamlalo.HKV = null;
            }
        }

        #endregion

        #region MeroallasIdoUpdateCommand

        private RelayCommand<BerendezesSzamlalo> m_MeroallasIdoUpdateCommand;

        public RelayCommand<BerendezesSzamlalo> MeroallasIdoUpdateCommand
        {
            get
            {
                if( m_MeroallasIdoUpdateCommand == null )
                    m_MeroallasIdoUpdateCommand = new RelayCommand<BerendezesSzamlalo>( MeroallasIdoUpdateCommandCall );
                return m_MeroallasIdoUpdateCommand;
            }
        }

        private void MeroallasIdoUpdateCommandCall( BerendezesSzamlalo szamlalo )
        {
            szamlalo.MeroallasIdo = DateTime.Now;
        }

        #endregion

        #endregion

        #region Helper Methods

        private void InitMerocsere()
        {
            if( Munkautasitas.Merohely == null )
                return;

            Munkautasitas.Merohely.Merokeszulekek.CollectionChanged   += Berendezes_CollectionChanged;
            Munkautasitas.Merohely.OnalloAramvaltok.CollectionChanged += Berendezes_CollectionChanged;
            Munkautasitas.Merohely.OnalloModemek.CollectionChanged    += Berendezes_CollectionChanged;

            var berendezesek = new List<Berendezes>();

            berendezesek.AddRange( Munkautasitas.Merohely.Merokeszulekek );
            berendezesek.AddRange( Munkautasitas.Merohely.OnalloAramvaltok );
            berendezesek.AddRange( Munkautasitas.Merohely.OnalloModemek );

            MeroBerendezesek = new ObservableCollection<Berendezes>( berendezesek );
        }

        private void Berendezes_CollectionChanged( object sender, NotifyCollectionChangedEventArgs e )
        {
            Berendezes berendezes;
            switch( e.Action )
            {
                case NotifyCollectionChangedAction.Add:
                    berendezes = e.NewItems[0] as Berendezes;
                    if( berendezes != null )
                        MeroBerendezesek.Add( berendezes );
                    break;
                case NotifyCollectionChangedAction.Remove:
                    berendezes = e.OldItems[0] as Berendezes;
                    if( berendezes != null )
                        MeroBerendezesek.Remove( berendezes );
                    break;
                case NotifyCollectionChangedAction.Reset:
                    MeroBerendezesek.Clear();
                    break;
                default:
                    break;
            }
        }

        private void Add_KismegszToMunka( AddBerendezesMunka anyag )
        {
            AddKismegszToMero( anyag );
            Messenger.Default.Unregister<AddBerendezesMunka>( this, Add_KismegszToMunka );
        }

        private void Add_MerokeszulekToMunka( AddBerendezesMunka anyag )
        {
            AddMerokeszulekToMunka( anyag );
            Messenger.Default.Unregister<AddBerendezesMunka>( this, Add_MerokeszulekToMunka );
        }

        private void Add_MeroBerendezesToMunka( AddBerendezesMunka anyag )
        {
            AddMeroBerendezesToMunka( anyag );
            Messenger.Default.Unregister<AddBerendezesMunka>( this, Add_MeroBerendezesToMunka );
        }

        public void CallUnregisters( CallUnregistersMerocseren cum)
        {
            Messenger.Default.Unregister<AddBerendezesMunka>( this, Add_KismegszToMunka );
            Messenger.Default.Unregister<AddBerendezesMunka>( this, Add_MerokeszulekToMunka );
            Messenger.Default.Unregister<AddBerendezesMunka>( this, Add_MeroBerendezesToMunka );
        }

        private void Update_BerendezesDevices( Berendezes result )
        {
            if( result != null )
            {
                if( result is Merokeszulek )
                {
                    // Hozzáadjuk az új berendezéshez a régiben levő berendezéseket ha volt
                    if( KimentettBerendezesek != null && KimentettBerendezesek.Count > 0 )
                    {
                        foreach( var berendezes in KimentettBerendezesek )
                        {
                            if( berendezes is Aramvalto )
                            {
                                var AramvaltoToMove              = berendezes as Aramvalto;
                                AramvaltoToMove.MerokeszulekAzon = result.Azonosito;
                                result.Berendezesek.Add( AramvaltoToMove );
                            }
                            else if( berendezes is Modem )
                            {
                                var modemToMove              = berendezes as Modem;
                                modemToMove.MeroKeszulekAzon = result.Azonosito;
                                result.Berendezesek.Add( modemToMove );
                            }
                            else if( berendezes is HKV )
                            {
                                var HKVToMove              = berendezes as HKV;
                                HKVToMove.MeroKeszulekAzon = result.Azonosito;
                                result.Berendezesek.Add( HKVToMove );
                            }
                            else if( berendezes is BerendezesSzamlalo )
                            {
                                var erdetiSzamlalo = berendezes as BerendezesSzamlalo;
                                var newSzamlalo  = new BerendezesSzamlalo();

                                newSzamlalo.KarakterSzam     = null;
                                newSzamlalo.MeroallasErtek   = null;
                                newSzamlalo.Leszerelt        = false;
                                newSzamlalo.MeroKeszulekAzon = result.Azonosito;
                                newSzamlalo.MeroallasEderet  = MeroallaEredetSapCode.SZERELOI;
                                newSzamlalo.KoSzamlaloAzonosito = erdetiSzamlalo.KoSzamlaloAzonosito;
                                newSzamlalo.HKV = erdetiSzamlalo.HKV;

                                result.Berendezesek.Add( newSzamlalo );
                            }
                            else
                            {
                                result.Berendezesek.Add( berendezes );
                            }
                        }
                    }
                    // Töröljük a régi berendezés berendezéseit és leszereljük
                    if( BerendezesToCsere != null && BerendezesToCsere.Berendezesek != null )
                        foreach( var berendezes in MeroBerendezesek )
                        {
                            if( berendezes == BerendezesToCsere )
                            {
                                if ( berendezes.Berendezesek != null && berendezes.Berendezesek.Count > 0 )
                                    berendezes.Berendezesek = berendezes.Berendezesek.Where( b => b is BerendezesSzamlalo ).ToObservableCollection();

                                BerendLeszerel( berendezes );
                                berendezes.IsCserePar = true;
                                RaisePropertyChanged( nameof( berendezes.IsCserePar ) );
                                RaisePropertyChanged( nameof( berendezes.Self ) );
                                break;
                            }
                        }
                }


                // Leszereljük a cserélni kívánt berendezést a mérőkészülék alól
                if( parentMeroBerendezes != null )
                {
                    foreach( var berendezes in parentMeroBerendezes.Berendezesek )
                    {
                        if( berendezes == BerendezesToCsere )
                        {
                            BerendLeszerel( berendezes );
                            berendezes.IsCserePar = true;
                            RaisePropertyChanged( nameof( berendezes.IsCserePar ) );
                            break;
                        }
                    }
                }
                // Ha önálló modemről van szó
                else if ( BerendezesToCsere is Modem )
                {
                    foreach ( var modem in Munkautasitas.Merohely.OnalloModemek )
                    {
                        if ( modem == BerendezesToCsere )
                        {
                            BerendLeszerel( modem );
                            modem.IsCserePar = true;
                            RaisePropertyChanged( nameof( modem.IsCserePar ) );
                            break;
                        }
                    }
                }
                else if ( BerendezesToCsere is Aramvalto )
                {
                    foreach ( var aramvalto in Munkautasitas.Merohely.OnalloAramvaltok )
                    {
                        if ( aramvalto == BerendezesToCsere )
                        {
                            BerendLeszerel( aramvalto );
                            aramvalto.IsCserePar = true;
                            RaisePropertyChanged( nameof( aramvalto.IsCserePar ) );
                            break;
                        }
                    }
                }

                // Kiürítjük a Kimentett berendezés listát
                if( KimentettBerendezesek != null )
                    KimentettBerendezesek.Clear();
            }
        }

        private void ResetKismegszFlags()
        {
            BerendMuveletekHelper.IsKismegszRFelszereles    = false;
            BerendMuveletekHelper.IsKismegszSFelszereles    = false;
            BerendMuveletekHelper.IsKismegszTFelszereles    = false;
            BerendMuveletekHelper.IsKismegszOsszFelszereles = false;
        }

        private void ResetBerendMuvHelper()
        {
            BerendMuveletekHelper.BerendezesListIndex = 0;
            BerendMuveletekHelper.IsBerendezes        = false;
            BerendMuveletekHelper.IsBerendezesCsere   = false;
            BerendMuveletekHelper.MeroBerendezes      = null;
            BerendMuveletekHelper.Munka               = null;
        }

        private void AddMerokeszulekToMunka( AddBerendezesMunka anyagHelper )
        {
            RaktariAnyagokExtender item = anyagHelper.Anyag;

            if( Munkautasitas.ElszamoltMeroBerendezesek == null )
            {
                Munkautasitas.ElszamoltMeroBerendezesek = new ObservableCollection<Berendezes>();
            }

            var ujMerokeszulekTipus = SAPCodeFeloldo.GetErtekek( "BERENDEZES_TIPUS_MERO" ).Where( mt => mt.Megnevezes == item.Megnevezes ).Select( mt => mt.Megnevezes ).FirstOrDefault();

            var isuSzerep = GetIsuSzerep( MerohberTipus.Merokeszulek );

            var ujMerokeszulek = new Merokeszulek
            {
                KaRaktarAzon = Storage.Instance.Session.RaktarHelyAzonosito,
                Azonosito    = Storage.Instance.Session.UjAzonositoHelper.GetNextValue(),
                GyariSzam    = string.Empty,
                ISUAzonosito = isuSzerep == ISUSzerep.ISU_SZEREP_NORMAL ? item.GyariSzam : "XXX",
                CikkSzam     = item.Cikkszam,
                Leszerelt    = false,
                Statusz      = KoFhBerendezesStatusz.FELSZERELT,
                Tipus        = ujMerokeszulekTipus != null ? ujMerokeszulekTipus : item.Megnevezes,
                UjFelszerelt = true
            };

            Munkautasitas.ElszamoltMeroBerendezesek.Add( ujMerokeszulek );
            // Nem csere
            if( !BerendMuveletekHelper.IsBerendezesCsere )
            {
                Munkautasitas.Merohely.Merokeszulekek.Add( ujMerokeszulek );
                App.Logger.Info( string.Format( "{0} ISU számú mérőkészülék felszerelve", ujMerokeszulek.ISUAzonosito ) );
            }
            // Csere
            else
            {
                ujMerokeszulek.IsCserePar       = true;
                ujMerokeszulek.LeszereltBerAzon = BerendMuveletekHelper.MeroBerendezes.ISUAzonosito != null ? BerendMuveletekHelper.MeroBerendezes.ISUAzonosito : null;

                // Beállítjuk a túláramvédelem fázisokat a lecserélt készülék alapján
                var merokeszulekToCsere = BerendMuveletekHelper.MeroBerendezes as Merokeszulek;
                if( merokeszulekToCsere != null )
                {
                    ujMerokeszulek.TularamVedelemR = merokeszulekToCsere.TularamVedelemR != null ? merokeszulekToCsere.TularamVedelemR : null;
                    ujMerokeszulek.TularamVedelemS = merokeszulekToCsere.TularamVedelemS != null ? merokeszulekToCsere.TularamVedelemS : null;
                    ujMerokeszulek.TularamVedelemT = merokeszulekToCsere.TularamVedelemT != null ? merokeszulekToCsere.TularamVedelemT : null;
                    ujMerokeszulek.TularamVedelemJelleg = merokeszulekToCsere.TularamVedelemJelleg;
                    ujMerokeszulek.MhelyBerendAzon = merokeszulekToCsere.Azonosito;

                }
                Munkautasitas.Merohely.Merokeszulekek.Insert( BerendMuveletekHelper.BerendezesListIndex + 1, ujMerokeszulek );
                App.Logger.Info( string.Format( "{0} ISU számú mérőkészülék lecserélve, új berendezés ISU száma: {1}", ujMerokeszulek.LeszereltBerAzon, ujMerokeszulek.ISUAzonosito ) );
            }

            UjBerendezes = ujMerokeszulek;

            if( BerendMuveletekHelper != null && BerendMuveletekHelper.IsBerendezesCsere )
            {
                var merokeszulek = Munkautasitas.Merohely.Merokeszulekek.Where( mk => mk == BerendMuveletekHelper.MeroBerendezes ).FirstOrDefault();
                if( merokeszulek != null )
                {
                    merokeszulek.IsCserePar = true;
                }

                if( UjBerendezes != null )
                    Update_BerendezesDevices( UjBerendezes );

                FillMeroBerendezesTulajdonsagok();
            }
            else
            {
                FillMeroBerendezesTulajdonsagok();
            }

            BerendMuveletekHelper.IsMerokeszulek = false;
        }

        private void AddMeroBerendezesToMunka( AddBerendezesMunka anyag )
        {
            RaktariAnyagokExtender item = anyag.Anyag;

            var HKVFajtaAzonosito       = RaktariAnyagFajtak.Where( b => (b.Azonosito == MerohberTipus.HKV) ).FirstOrDefault()?.Azonosito;
            var modemFajtaAzonosito     = RaktariAnyagFajtak.Where( b => (b.Azonosito == MerohberTipus.Modem) ).FirstOrDefault()?.Azonosito;
            var aramvaltoFajtaAzonosito = RaktariAnyagFajtak.Where( b => (b.Azonosito == MerohberTipus.Aramvalto) ).FirstOrDefault()?.Azonosito;

            var parentBerendezes = parentMeroBerendezes != null ? parentMeroBerendezes :
                                   Munkautasitas.Merohely.Merokeszulekek.Where( b => b.Azonosito == BerendMuveletekHelper.MeroBerendezes.Azonosito ).FirstOrDefault();

            if( Munkautasitas.ElszamoltMeroBerendezesek == null )
            {
                Munkautasitas.ElszamoltMeroBerendezesek = new ObservableCollection<Berendezes>();
            }

            if( BerendMuveletekHelper.MeroBerendezes != null )
            {
                if( HKVFajtaAzonosito != null && item.KomerohberAzonosito == HKVFajtaAzonosito )
                {
                    var ujHKVTipus = SAPCodeFeloldo.GetErtekek( "BERENDEZES_TIPUS_HKV" ).Where( mt => mt.Megnevezes == item.Megnevezes ).Select( mt => mt.Megnevezes ).FirstOrDefault();
                    long isuSzerep = GetIsuSzerep( MerohberTipus.HKV );

                    var ujHKV = new HKV
                    {
                        KaRaktarAzon     = Storage.Instance.Session.RaktarHelyAzonosito,
                        MeroKeszulekAzon = parentBerendezes.Azonosito,
                        Azonosito        = Storage.Instance.Session.UjAzonositoHelper.GetNextValue(),
                        GyariSzam        = string.Empty,
                        ISUAzonosito     = isuSzerep == ISUSzerep.ISU_SZEREP_NORMAL ? item.GyariSzam : "XXX",
                        CikkSzam         = item.Cikkszam,
                        Leszerelt        = false,
                        Statusz          = KoFhBerendezesStatusz.FELSZERELT,
                        Tipus            = ujHKVTipus != null ? ujHKVTipus : item.Megnevezes,
                        UjFelszerelt     = true
                    };

                    Munkautasitas.ElszamoltMeroBerendezesek.Add( ujHKV );
                    // Nem csere
                    if( !BerendMuveletekHelper.IsBerendezesCsere )
                    {
                        var merokeszulekToAdd = Munkautasitas.Merohely.Merokeszulekek.Where( mk => mk.Azonosito == BerendMuveletekHelper.MeroBerendezes.Azonosito ).FirstOrDefault();
                        if ( merokeszulekToAdd != null )
                            merokeszulekToAdd.Berendezesek.Add( ujHKV );

                        App.Logger.Info( string.Format( "{0} ISU számú HKV felszerelve", ujHKV.ISUAzonosito ) );
                    }
                    // Csere
                    else
                    {
                        ujHKV.IsCserePar       = true;
                        ujHKV.LeszereltBerAzon = BerendMuveletekHelper.MeroBerendezes.ISUAzonosito != null ? BerendMuveletekHelper.MeroBerendezes.ISUAzonosito : null;


                        var hkvToCsere = BerendMuveletekHelper.MeroBerendezes as HKV;
                        if ( hkvToCsere != null )
                        {
                            ujHKV.MhelyBerendAzon = hkvToCsere.Azonosito;
                        }

                        var merokeszulekToInsert = Munkautasitas.Merohely.Merokeszulekek.Where( mk => mk.Azonosito == parentBerendezes.Azonosito ).FirstOrDefault();
                        if ( merokeszulekToInsert != null )
                            merokeszulekToInsert.Berendezesek.Insert( BerendMuveletekHelper.BerendezesListIndex + 1, ujHKV );

                        App.Logger.Info( string.Format( "{0} gyári számú HKV lecserélve, új berendezés ISU száma: {1}", ujHKV.LeszereltBerAzon, ujHKV.ISUAzonosito ) );
                    }
                    UjBerendezes = ujHKV;
                }

                else if( modemFajtaAzonosito != null && item.KomerohberAzonosito == modemFajtaAzonosito )
                {
                    
                    var isuSzerep = GetIsuSzerep( MerohberTipus.Modem );

                    var ujModem = new Modem
                    {
                        KaRaktarAzon     = Storage.Instance.Session.RaktarHelyAzonosito,
                        MeroKeszulekAzon = parentMeroBerendezes == null ? 0 : parentBerendezes.Azonosito,
                        Azonosito        = Storage.Instance.Session.UjAzonositoHelper.GetNextValue(),
                        GyariSzam        = string.Empty,
                        ISUAzonosito     = isuSzerep == ISUSzerep.ISU_SZEREP_NORMAL ? item.GyariSzam : "XXX",
                        CikkSzam         = item.Cikkszam,
                        Leszerelt        = false,
                        Statusz          = KoFhBerendezesStatusz.FELSZERELT,
                        Tipus            = "Modem",
                        UjFelszerelt     = true
                    };

                    Munkautasitas.ElszamoltMeroBerendezesek.Add( ujModem );
                    // Nem csere
                    if( !BerendMuveletekHelper.IsBerendezesCsere )
                    {
                        var merokeszulekToAdd = Munkautasitas.Merohely.Merokeszulekek.Where( mk => mk.Azonosito == BerendMuveletekHelper.MeroBerendezes.Azonosito ).FirstOrDefault();
                        if ( merokeszulekToAdd != null )
                            merokeszulekToAdd.Berendezesek.Add( ujModem );

                        App.Logger.Info( string.Format( "{0} ISU számú modem felszerelve", ujModem.ISUAzonosito ) );
                    }
                    // Csere
                    else
                    {
                        ujModem.IsCserePar       = true;
                        ujModem.LeszereltBerAzon = BerendMuveletekHelper.MeroBerendezes.ISUAzonosito != null ? BerendMuveletekHelper.MeroBerendezes.ISUAzonosito : null;

                        // Ha nem önálló készülékről van szó
                        if ( parentBerendezes != null )
                        {
                            var merokeszulekToInsert = Munkautasitas.Merohely.Merokeszulekek.Where( mk => mk.Azonosito == parentBerendezes.Azonosito ).FirstOrDefault();
                            if ( merokeszulekToInsert != null )
                                merokeszulekToInsert.Berendezesek.Insert( BerendMuveletekHelper.BerendezesListIndex + 1, ujModem );
                        }
                        // Ha önálló készülékről van szó
                        else
                        {
                            if ( Munkautasitas.Merohely.OnalloModemek != null )
                                Munkautasitas.Merohely.OnalloModemek.Insert( BerendMuveletekHelper.BerendezesListIndex + 1, ujModem );
                        }

                        App.Logger.Info( string.Format( "{0} ISU számú modem lecserélve, új berendezés ISU száma: {1}", ujModem.LeszereltBerAzon, ujModem.ISUAzonosito ) );
                    }
                    UjBerendezes = ujModem;
                }
                else if( aramvaltoFajtaAzonosito != null && item.KomerohberAzonosito == aramvaltoFajtaAzonosito )
                {
                    var isuSzerep = GetIsuSzerep( MerohberTipus.Aramvalto );

                    var ujAramvalto = new Aramvalto
                    {
                        KaRaktarAzon     = Storage.Instance.Session.RaktarHelyAzonosito,
                        MerokeszulekAzon = parentMeroBerendezes == null ? 0 : parentBerendezes.Azonosito,
                        Azonosito        = Storage.Instance.Session.UjAzonositoHelper.GetNextValue(),
                        GyariSzam        = string.Empty,
                        ISUAzonosito     = isuSzerep == ISUSzerep.ISU_SZEREP_NORMAL ? item.GyariSzam : "XXX",
                        CikkSzam         = item.Cikkszam,
                        Leszerelt        = false,
                        Statusz          = KoFhBerendezesStatusz.FELSZERELT,
                        Tipus            = "Áramváltó",
                        UjFelszerelt     = true
                    };

                    Munkautasitas.ElszamoltMeroBerendezesek.Add( ujAramvalto );
                    // Nem csere
                    if( !BerendMuveletekHelper.IsBerendezesCsere )
                    {
                        var merokeszulekToAdd = Munkautasitas.Merohely.Merokeszulekek.Where( mk => mk.Azonosito == BerendMuveletekHelper.MeroBerendezes.Azonosito ).FirstOrDefault();
                        if( merokeszulekToAdd != null )
                            merokeszulekToAdd.Berendezesek.Add( ujAramvalto );

                        App.Logger.Info( string.Format( "{0} ISU számú áramváltó felszerelve", ujAramvalto.ISUAzonosito ) );
                    }
                    // Csere
                    else
                    {
                        ujAramvalto.IsCserePar       = true;
                        ujAramvalto.LeszereltBerAzon = BerendMuveletekHelper.MeroBerendezes.ISUAzonosito != null ? BerendMuveletekHelper.MeroBerendezes.ISUAzonosito : null;

                        // Ha nem önálló készülékről van szó
                        if ( parentBerendezes != null )
                        {
                            var merokeszulekToInsert = Munkautasitas.Merohely.Merokeszulekek.Where( mk => mk.Azonosito == parentBerendezes.Azonosito ).FirstOrDefault();
                            if ( merokeszulekToInsert != null )
                                merokeszulekToInsert.Berendezesek.Insert( BerendMuveletekHelper.BerendezesListIndex + 1, ujAramvalto );
                        }
                        // Ha önálló készülékről van szó
                        else
                        {
                            if ( Munkautasitas.Merohely.OnalloAramvaltok != null )
                                Munkautasitas.Merohely.OnalloAramvaltok.Insert( BerendMuveletekHelper.BerendezesListIndex + 1, ujAramvalto );
                        }

                        App.Logger.Info( string.Format( "{0} ISU számú áramváltó lecserélve, új berendezés ISU száma: {1}", ujAramvalto.LeszereltBerAzon, ujAramvalto.ISUAzonosito ) );
                    }
                    UjBerendezes = ujAramvalto;  
                }

                if( BerendMuveletekHelper != null && BerendMuveletekHelper.IsBerendezesCsere )
                {
                    var merokeszulekToUpdate = Munkautasitas.Merohely.Merokeszulekek.Where( mk => mk.Berendezesek.Contains( BerendMuveletekHelper.MeroBerendezes ) ).FirstOrDefault();
                    // Ha mérő alatti készülékről van szó
                    if ( merokeszulekToUpdate != null )
                    {
                        var innerKeszToUpdate = merokeszulekToUpdate != null ? merokeszulekToUpdate.Berendezesek.Where( k => k == BerendMuveletekHelper.MeroBerendezes ).FirstOrDefault() : null;
                        if ( innerKeszToUpdate != null )
                        {
                            innerKeszToUpdate.IsCserePar   = true;
                            innerKeszToUpdate.UjFelszerelt = false;
                        }
                    }
                    // Ha önálló berendezésről van szó
                    else
                    {
                        // Modem
                        if ( UjBerendezes is Modem )
                        {
                            var modemToUpdate = Munkautasitas.Merohely.OnalloModemek != null ? Munkautasitas.Merohely.OnalloModemek.Where( k => k == BerendMuveletekHelper.MeroBerendezes ).FirstOrDefault() : null;
                            if ( modemToUpdate != null )
                            {
                                modemToUpdate.IsCserePar   = true;
                                modemToUpdate.UjFelszerelt = false;
                            }
                        }
                        // Áramváltó
                        else if ( UjBerendezes is Aramvalto )
                        {
                            var aramValtoToUpdate = Munkautasitas.Merohely.OnalloAramvaltok != null ? Munkautasitas.Merohely.OnalloAramvaltok.Where( k => k == BerendMuveletekHelper.MeroBerendezes ).FirstOrDefault() : null;
                            if ( aramValtoToUpdate != null )
                            {
                                aramValtoToUpdate.IsCserePar   = true;
                                aramValtoToUpdate.UjFelszerelt = false;
                            }
                        }
                    }
                    
                    Update_BerendezesDevices( UjBerendezes );

                    FillMeroBerendezesTulajdonsagok();
                }
                else
                {
                    FillMeroBerendezesTulajdonsagok();
                }

                BerendMuveletekHelper.IsBerendezes = false;
            }
        }

        private long GetIsuSzerep( long merohberTipusAzonosito )
        {
            var isuSzerep = ISUSzerep.ISU_SZEREP_ISUBAN_NEM_SZEREPEL;
            if( RaktariAnyagFajtak != null )
            {
                var anyagFajta = RaktariAnyagFajtak.Where( f => f.Azonosito == merohberTipusAzonosito ).FirstOrDefault();
                if( anyagFajta != null )
                {
                    isuSzerep = anyagFajta.ISUSzerep;
                }
            }
            return isuSzerep;
        }

        private void AddKismegszToMero( AddBerendezesMunka anyagHelper )
        {
            RaktariAnyagokExtender item = anyagHelper.Anyag;

            Merokeszulek keszulekToUpdate = Munkautasitas.Merohely.Merokeszulekek.Where( mk => mk.Azonosito == BerendMuveletekHelper.MeroBerendezes.Azonosito ).FirstOrDefault();

            var matchingItem = Regex.Match( item.Megnevezes, @"[0-9]{1,4}[A]", RegexOptions.IgnoreCase ).Value;
            var kismegszAramerossegValue = string.IsNullOrEmpty( matchingItem ) ? null : matchingItem.Remove( matchingItem.Length - 1 ); // Lecsípjük a végéről az A betűt

            if( keszulekToUpdate != null && BerendMuveletekHelper != null && BerendMuveletekHelper.IsKismegszOsszFelszereles )
            {
                // Ha 3 különböző kismegszakítót választott ki, sorrendben beállítjuk a 3 fázist a mérőkészüléknél
                if( KeszletSpinTextBoxHelper.CurrentKismegszakitokToAdd != null && KeszletSpinTextBoxHelper.CurrentKismegszakitokToAdd.Count == 3 )
                {
                    kismegszAramerossegValue              = ExtractAramerossegValue( 0 );
                    keszulekToUpdate.TularamVedelemR      = kismegszAramerossegValue;
                    keszulekToUpdate.KismegszRisModositva = true;

                    kismegszAramerossegValue              = ExtractAramerossegValue( 1 );
                    keszulekToUpdate.TularamVedelemS      = kismegszAramerossegValue;
                    keszulekToUpdate.KismegszSisModositva = true;

                    kismegszAramerossegValue              = ExtractAramerossegValue( 2 );
                    keszulekToUpdate.TularamVedelemT      = kismegszAramerossegValue;
                    keszulekToUpdate.KismegszTisModositva = true;
                }
                // Ha 2 különböző típust választott ki, megnézzük melyikből van 2 és aszerint állítjuk be
                else if ( KeszletSpinTextBoxHelper.CurrentKismegszakitokToAdd != null && KeszletSpinTextBoxHelper.CurrentKismegszakitokToAdd.Count == 2 )
                {
                    if ( KeszletSpinTextBoxHelper.CurrentKismegszakitokToAdd[0].FelhasznaltMennyiseg == 2 )
                    {
                        kismegszAramerossegValue              = ExtractAramerossegValue( 0 );
                        keszulekToUpdate.TularamVedelemR      = kismegszAramerossegValue;
                        keszulekToUpdate.KismegszRisModositva = true;

                        kismegszAramerossegValue              = ExtractAramerossegValue( 0 );
                        keszulekToUpdate.TularamVedelemS      = kismegszAramerossegValue;
                        keszulekToUpdate.KismegszSisModositva = true;

                        kismegszAramerossegValue              = ExtractAramerossegValue( 1 );
                        keszulekToUpdate.TularamVedelemT      = kismegszAramerossegValue;
                        keszulekToUpdate.KismegszTisModositva = true;
                    }
                    else if ( KeszletSpinTextBoxHelper.CurrentKismegszakitokToAdd[1].FelhasznaltMennyiseg == 2 )
                    {
                        kismegszAramerossegValue              = ExtractAramerossegValue( 0 );
                        keszulekToUpdate.TularamVedelemR      = kismegszAramerossegValue;
                        keszulekToUpdate.KismegszRisModositva = true;

                        kismegszAramerossegValue              = ExtractAramerossegValue( 1 );
                        keszulekToUpdate.TularamVedelemS      = kismegszAramerossegValue;
                        keszulekToUpdate.KismegszSisModositva = true;

                        kismegszAramerossegValue              = ExtractAramerossegValue( 1 );
                        keszulekToUpdate.TularamVedelemT      = kismegszAramerossegValue;
                        keszulekToUpdate.KismegszTisModositva = true;
                    }
                    else
                    {
                        kismegszAramerossegValue              = ExtractAramerossegValue( 0 );
                        keszulekToUpdate.TularamVedelemR      = kismegszAramerossegValue;
                        keszulekToUpdate.KismegszRisModositva = true;
                    }
                }
                else
                {
                    keszulekToUpdate.TularamVedelemR = kismegszAramerossegValue;
                    keszulekToUpdate.TularamVedelemS = kismegszAramerossegValue;
                    keszulekToUpdate.TularamVedelemT = kismegszAramerossegValue;

                    keszulekToUpdate.KismegszRisModositva = true;
                    keszulekToUpdate.KismegszSisModositva = true;
                    keszulekToUpdate.KismegszTisModositva = true;
                }
            }
            else
            {
                if( kismegszAramerossegValue != null )
                {
                    if( keszulekToUpdate != null )
                    {
                        if( BerendMuveletekHelper.IsKismegszRFelszereles )
                        {
                            keszulekToUpdate.TularamVedelemR = kismegszAramerossegValue;
                            keszulekToUpdate.KismegszRisModositva = true;
                        }
                        else if( BerendMuveletekHelper.IsKismegszSFelszereles )
                        {
                            keszulekToUpdate.TularamVedelemS = kismegszAramerossegValue;
                            keszulekToUpdate.KismegszSisModositva = true;
                        }
                        else if( BerendMuveletekHelper.IsKismegszTFelszereles )
                        {
                            keszulekToUpdate.TularamVedelemT = kismegszAramerossegValue;
                            keszulekToUpdate.KismegszTisModositva = true;
                        }

                        App.Logger.Info( string.Format( "{0} ISU számú berendezésre kismegszakító felszerelve", keszulekToUpdate.ISUAzonosito ) );
                    }
                }
            }

        }

        private void BerendLeszerel( Berendezes berend )
        {
            if( GetIsKeszulekModified( berend ) )
                berend.Statusz = KoFhBerendezesStatusz.VALTOZOTT_LESZERELT;
            else
                berend.Statusz = KoFhBerendezesStatusz.NEM_VALTOZOTT_LESZERELT;

            // Ha HKV-ról van szó, ki kell üríteni a MEROKESZULEK_AZONOSITO mező értékét
            if( berend is HKV hkv )
                hkv.MeroKeszulekAzon = null;
            else if( berend is Modem modem )
                modem.MeroKeszulekAzon = null;
            else if( berend is Aramvalto aramvalto )
                aramvalto.MerokeszulekAzon = null;

            if( berend.Berendezesek != null )
            {
                foreach( var berendezes in berend.Berendezesek )
                {
                    berendezes.RaiseProperties();
                }
            }
            FillMeroBerendezesTulajdonsagok();
            App.Logger.Info( string.Format( "{0} ISU számú berendezés leszerelve ", berend.ISUAzonosito ) );
        }

        private string ExtractAramerossegValue( int index )
        {
            var kismegsz                 = KeszletSpinTextBoxHelper.CurrentKismegszakitokToAdd.ElementAtOrDefault( index );
            var matchingItem             = Regex.Match( kismegsz.Megnevezes, @"[0-9]{1,4}[A]", RegexOptions.IgnoreCase ).Value;
            var kismegszAramerossegValue = string.IsNullOrEmpty( matchingItem ) ? null : matchingItem.Remove( matchingItem.Length - 1 ); // Lecsípjük a végéről az A betűt
            return kismegszAramerossegValue;
        }

        private void FillMeroBerendezesTulajdonsagok()
        {
            var vanJegyzokonyvPanel = PanelHelper.VanIlyenPanel( Munkautasitas.ElemiMunkaAdatai.NormaTevekenysegAzonosito, nameof( JegyzokonyvPanel ) );

            var nemValtozott = new Berendezes.StatuszComboOpcio()
            {
                Id         = KoFhBerendezesStatusz.NEM_VALTOZOTT,
                Megnevezes = DisplayResources.NemValtozott
            };

            var nemValtozottLeszerelt = new Berendezes.StatuszComboOpcio()
            {
                Id         = KoFhBerendezesStatusz.NEM_VALTOZOTT_LESZERELT,
                Megnevezes = DisplayResources.NemValtozottLeszerelt
            };

            var valtozott = new Berendezes.StatuszComboOpcio()
            {
                Id         = KoFhBerendezesStatusz.VALTOZOTT,
                Megnevezes = DisplayResources.Valtozott
            };

            var valtozottLeszerelt = new Berendezes.StatuszComboOpcio()
            {
                Id         = KoFhBerendezesStatusz.VALTOZOTT_LESZERELT,
                Megnevezes = DisplayResources.ValtozottLeszerelt
            };

            var felleltLeszerelt = new Berendezes.StatuszComboOpcio()
            {
                Id         = KoFhBerendezesStatusz.FELLELT_LESZERELT,
                Megnevezes = DisplayResources.FelleltLeszerelt
            };

            var felszerelt = new Berendezes.StatuszComboOpcio()
            {
                Id         = KoFhBerendezesStatusz.FELSZERELT,
                Megnevezes = DisplayResources.Felszerelt
            };

            // Berendezés Státusz Opciók feltöltése
            List<Berendezes.StatuszComboOpcio> berendezesStatuszOpciok = new List<Berendezes.StatuszComboOpcio>()
            {
                nemValtozott, nemValtozottLeszerelt, valtozott, valtozottLeszerelt, felleltLeszerelt, felszerelt
            };


            var vanMagnesKapcsolo = new HKV.MagneskapcsoloOpcio()
            {
                Ertek      = "1",
                Megnevezes = "Van"
            };

            var nincsMagnesKapcsolo = new HKV.MagneskapcsoloOpcio()
            {
                Ertek      = "0",
                Megnevezes = "Nincs"
            };

            List<HKV.MagneskapcsoloOpcio> HKVMagnesKapcsoloOpciok = new List<HKV.MagneskapcsoloOpcio>()
            {
                vanMagnesKapcsolo, nincsMagnesKapcsolo
            };

            var SapTable = StoredTableHelper.Instance.GetRecord<KoSapRecord>().ToList();

            // Berendezés Típus Opciók feltöltése
            var meroTipusOpciok = SAPCodeFeloldo.GetErtekek( "BERENDEZES_TIPUS_MERO" ).Select( x => x.Megnevezes ).ToList();
            var hkvTipusOpciok  = SAPCodeFeloldo.GetErtekek( "BERENDEZES_TIPUS_HKV" ).Select( x => x.Megnevezes ).ToList();

            // Berendezés Túláramvédelem Opciók feltöltése
            var feszultsegOpciok = SAPCodeFeloldo.GetErtekek( "TULARAM_VEDELEM" ).Select( x => x.Megnevezes ).ToList();
            try
            {
                feszultsegOpciok?.Sort( (x,y) => int.Parse(x) == int.Parse(y) ? 0 : int.Parse( x ) > int.Parse( y ) ? 1 : -1 );
            }
            catch( InvalidOperationException e)
            {
                App.Logger.Warn( string.Format( "TULARAM_VEDELEM kódtábla konvertálás hiba: {0}", e.Message ) );
            }

            var jellegOpciok     = SAPCodeFeloldo.GetErtekek( "TULARAM_VEDELEM_JELLEG" ).Select( x => x.Megnevezes ).ToList();
            var trafoFajtaOpciok = SAPCodeFeloldo.GetErtekek( "BERENDEZES_TRAFO_FAJTA" ).Select( x => x.Megnevezes ).ToList();

            // HKV terhelési Opciók feltöltése
            var HKVTerhelesOpciok = SAPCodeFeloldo.GetErtekek( "TERHELHETOSEG" ).Select( o => o.Megnevezes ).ToList();

            // Berendezés Tekercs Opciók feltöltése
            var tekercsOpciok = SAPCodeFeloldo.GetErtekek( "BERENDEZES_TEKERCS_CSOPORT" ).Select( o => o.Megnevezes ).ToList();

            // Berendezés Feszültségszint Opciók feltöltése
            var feszultsegszintOpciok = SAPCodeFeloldo.GetErtekek( "BERENDEZES_FESZULTSEGSZINT" ).Select( o => o.Megnevezes ).ToList();

            // Számláló HKV kód opcióinak, tarifa típusának és mérőállás eredetének feltöltése
            var hkvKodErtekek           = SAPCodeFeloldo.GetErtekek( "HKV" );
            var szamlaloTipusok         = StoredTableHelper.Instance.GetRecord<KoSzamlaloTipusRecord>().OrderBy( o => o.Megnevezes).ToList();
            var szamlaloMeroallasEredet = SAPCodeFeloldo.GetErtekek( "MEROALLAS_EREDET" );

 
            if( Munkautasitas.Merohely.Merokeszulekek != null )
            {
                foreach( var merokeszulek in Munkautasitas.Merohely.Merokeszulekek )
                {
                    

                    merokeszulek.Statuszok            = berendezesStatuszOpciok;
                    merokeszulek.TularamVedelemOpciok = feszultsegOpciok;
                    merokeszulek.TularamJellegOpciok  = jellegOpciok;
                    merokeszulek.TipusOpciok          = meroTipusOpciok;
                    // Ha nincs benne a tipusOpciokban a mero típusa, akkor hozzáadjuk
                    if( merokeszulek.Tipus != null && !meroTipusOpciok.Contains( merokeszulek.Tipus ) )
                        meroTipusOpciok.Add( merokeszulek.Tipus );
                    merokeszulek.TipusOpciok.Sort();
                    merokeszulek.EfizFlag = SapTable.Where( s => s.Megnevezes == merokeszulek.Tipus ).FirstOrDefault() != null ? 
                                            SapTable.Where( s => s.Megnevezes == merokeszulek.Tipus ).FirstOrDefault().EfizFlag : 
                                            false;

                    // Megnézzük változott-e a készülék és aszerint állítjuk a státuszát de, csak ha nem jegyzőkönyves a munka
                    
                    if ( !vanJegyzokonyvPanel && !merokeszulek.Leszerelt && !merokeszulek.UjFelszerelt )
                        merokeszulek.Statusz = GetIsKeszulekModified( merokeszulek ) ? KoFhBerendezesStatusz.VALTOZOTT : KoFhBerendezesStatusz.NEM_VALTOZOTT;

                    foreach (var berend in merokeszulek.Berendezesek)
                    {
                        var szamlalo = berend as BerendezesSzamlalo;
                        if (szamlalo != null)
                            szamlalo.EfizFlag = merokeszulek.EfizFlag;
                    }
                    MeroKapcsolodoFenykepek_CollectionChanged( merokeszulek );

                    if( merokeszulek.Berendezesek != null )
                    {
                        foreach( var berendezes in merokeszulek.Berendezesek )
                        {
                            berendezes.Statuszok             = berendezesStatuszOpciok;
                            berendezes.TrafoFajtaOpciok      = trafoFajtaOpciok;
                            berendezes.TekercsCsoportOpciok  = tekercsOpciok;
                            berendezes.FeszultsegSzintOpciok = feszultsegszintOpciok;
                            berendezes.TipusOpciok           = meroTipusOpciok;


                            var szamlalo = berendezes as BerendezesSzamlalo;

                            if( szamlalo != null )
                            {
                                if ( szamlalo.KoSzamlaloAzonosito != 0 )
                                {
                                    szamlalo.MeroKeszulekAzon = merokeszulek.Azonosito;
                                    szamlalo.HKVOpciok = hkvKodErtekek;
                                    szamlalo.TarifaFajtaOpciok = szamlaloTipusok;   
                                }

                                var meroAllaseredet = szamlalo.MeroallasEderet != null ?
                                                      szamlaloMeroallasEredet.Where( sz => sz.SapCode == szamlalo.MeroallasEderet ).FirstOrDefault() :
                                                      null;

                                szamlalo.MeroAllasEredetDisplay = meroAllaseredet?.Megnevezes;

                                szamlalo.HKVOpciok         = hkvKodErtekek;
                                szamlalo.TarifaFajtaOpciok = szamlaloTipusok;

                                var hkvForVezerlo = merokeszulek.Berendezesek.Where( b => b is HKV && !b.Leszerelt ).FirstOrDefault();
                                if( hkvForVezerlo != null )
                                {
                                    szamlalo.Vezerlo = hkvForVezerlo.ISUAzonosito;
                                }
                                if( merokeszulek.Leszerelt )
                                    szamlalo.Statusz = KoFhBerendezesStatusz.NEM_VALTOZOTT_LESZERELT;
                                RaisePropertyChanged( nameof( szamlalo.Self ) );
                            }

                            var hkv = berendezes as HKV;

                            if( hkv != null )
                            {
                                hkv.MeroKeszulekAzon = null; // HKV-nál nem kell értéket adni a MEROKESZULEK_AZONOSITO mezőnek
                                hkv.Statuszok        = berendezesStatuszOpciok;
                                hkv.HKVTerhOpciok    = HKVTerhelesOpciok;
                                hkv.TipusOpciok      = hkvTipusOpciok;
                                if( hkv.Tipus != null && !hkvTipusOpciok.Contains( hkv.Tipus ) )
                                    hkvTipusOpciok.Add( hkv.Tipus );
                                hkv.TipusOpciok.Sort();
                                hkv.MagnesKapcsoloOpciok = HKVMagnesKapcsoloOpciok;
                                // Beállítjuk a mágneskapcsolót
                                SetHKVMagneskapcsolo( hkv );
                                // Megnézzük változott-e a készülék és aszerint állítjuk a státuszát, de csak ha nem jegyzőkönyves a munka
                                if ( !vanJegyzokonyvPanel && !hkv.Leszerelt && !hkv.UjFelszerelt )
                                    hkv.Statusz = GetIsKeszulekModified( hkv ) ? KoFhBerendezesStatusz.VALTOZOTT : KoFhBerendezesStatusz.NEM_VALTOZOTT;
                            }

                            var aramValto = berendezes as Aramvalto;

                            if ( aramValto != null )
                            {
                                aramValto.MerokeszulekAzon = aramValto.Leszerelt ? (long?)null : merokeszulek.Azonosito; // Csak felszerelt áramváltónál kell értéket adni a MEROKESZULEK_AZONOSITO mezőnek

                                // Ha nincs benne a feszultsegszintOpciokban a fesz típusa, akkor hozzáadjuk
                                if( aramValto.FeszultsegSzint != null && !feszultsegszintOpciok.Contains( aramValto.FeszultsegSzint ) )
                                    feszultsegszintOpciok.Add( aramValto.FeszultsegSzint );

                                // Megnézzük változott-e a készülék és aszerint állítjuk a státuszát, de csak ha nem jegyzőkönyves a munka
                                if ( !vanJegyzokonyvPanel && !aramValto.Leszerelt && !aramValto.UjFelszerelt )
                                    aramValto.Statusz = GetIsKeszulekModified( aramValto ) ? KoFhBerendezesStatusz.VALTOZOTT : KoFhBerendezesStatusz.NEM_VALTOZOTT;
                            }

                            var modem = berendezes as Modem;

                            if ( modem != null )
                            {
                                modem.MeroKeszulekAzon = modem.Leszerelt ? (long?)null : merokeszulek.Azonosito; // Csak felszerelt modemnél kell értéket adni a MEROKESZULEK_AZONOSITO mezőnek

                                // Megnézzük változott-e a készülék és aszerint állítjuk a státuszát, de csak ha nem jegyzőkönyves a munka
                                if ( !vanJegyzokonyvPanel && !modem.Leszerelt && !modem.UjFelszerelt )
                                    modem.Statusz = GetIsKeszulekModified( modem ) ? KoFhBerendezesStatusz.VALTOZOTT : KoFhBerendezesStatusz.NEM_VALTOZOTT;
                            }

                            merokeszulek.TarifaFajtaDisplay = GetMeroTarifafajta( merokeszulek );
                        }
                    }
                    RaisePropertyChanged( nameof( merokeszulek.Self ) );
                }
            }

            if( Munkautasitas.Merohely.OnalloAramvaltok != null )
            {
                foreach( var aramvalto in Munkautasitas.Merohely.OnalloAramvaltok )
                {
                    aramvalto.Statuszok             = berendezesStatuszOpciok;
                    aramvalto.TipusOpciok           = meroTipusOpciok;
                    aramvalto.TekercsCsoportOpciok  = tekercsOpciok;
                    aramvalto.FeszultsegSzintOpciok = feszultsegszintOpciok;
                }
            }

            if( Munkautasitas.Merohely.OnalloModemek != null )
            {
                foreach( var modem in Munkautasitas.Merohely.OnalloModemek )
                {
                    modem.Statuszok   = berendezesStatuszOpciok;
                    modem.TipusOpciok = meroTipusOpciok;
                }
            }
        }

        private void SetHKVMagneskapcsolo( HKV hkv )
        {
            if( hkv.MagnesKapcsolo != null &&
                ( hkv.MagnesKapcsolo.Equals( "Nincs", StringComparison.InvariantCultureIgnoreCase )
                    || hkv.MagnesKapcsolo.Equals( "Nem", StringComparison.InvariantCultureIgnoreCase )
                    || hkv.MagnesKapcsolo.Equals( "0", StringComparison.InvariantCultureIgnoreCase ) ) )
            {
                hkv.MagnesKapcsolo = "0";
            }
            else if( hkv.MagnesKapcsolo != null &&
                    ( hkv.MagnesKapcsolo.Equals( "Van", StringComparison.InvariantCultureIgnoreCase )
                        || hkv.MagnesKapcsolo.Equals( "Igen", StringComparison.InvariantCultureIgnoreCase )
                        || hkv.MagnesKapcsolo.Equals( "1", StringComparison.InvariantCultureIgnoreCase )) )
            {
                hkv.MagnesKapcsolo = "1";
            }
            else
                hkv.MagnesKapcsolo = null;
        }

        private string GetMeroTarifafajta( Merokeszulek mero )
        {
            // Mérőkészülék tarifafajtájának lekérdezése
            string tarifaFajta  = string.Empty;
            var szamlaloTipusok = StoredTableHelper.Instance.GetRecord<KoSzamlaloTipusRecord>().OrderBy( o => o.Megnevezes ).ToList();

            foreach( var berend in mero.Berendezesek )
            {
                var szamlalo = berend as BerendezesSzamlalo;

                if( szamlalo != null )
                {
                    if( szamlalo.KoSzamlaloAzonosito != 0 )
                    {
                        var szamlaloTipusMegnevezese = szamlaloTipusok.Where( szt => szt.Azonosito == szamlalo.KoSzamlaloAzonosito ).FirstOrDefault()?.Megnevezes;
                        if( tarifaFajta == string.Empty )
                        {
                            tarifaFajta = szamlaloTipusMegnevezese;
                        }
                        else
                        {
                            tarifaFajta = tarifaFajta == szamlaloTipusMegnevezese ? tarifaFajta : DisplayResources.VegyesTarifa;
                        }
                    }
                }
            }

            return tarifaFajta;
        }

        private bool GetIsKeszulekModified( Berendezes berend )
        {
            var isModified = false;

            if ( !berend.UjFelszerelt )
            {
                if( berend is Merokeszulek mero )
                {
                    if( mero.UnmodifiedTularamR == mero.TularamVedelemR
                        && mero.UnmodifiedTularamS == mero.TularamVedelemS
                        && mero.UnmodifiedTularamT == mero.TularamVedelemT
                        && mero.UnmodifiedTularamRST == mero.TularamVedelemRST
                        && mero.UnmodifiedTularamJelleg == mero.TularamVedelemJelleg
                        && mero.UnmodifiedHitelesEv == mero.HitelesEv )
                    {
                        isModified = false;
                    }
                    else
                        isModified = true;
                }
                if( !isModified )
                {
                    if( berend.UnmodifiedTipus == berend.Tipus
                        && berend.UnmodifiedGyartasEv == berend.GyartasEv
                        && berend.UnmodifiedHitelesitesIdo == berend.HitelesitesIdo
                        && berend.UnmodifiedTerhelhetoseg == berend.Terhelhetoseg
                        && berend.UnmodifiedMagnesKapcsolo == berend.MagnesKapcsolo
                        && berend.UnmodifiedFeszultsegSzint == berend.FeszultsegSzint
                        && berend.UnmodifiedTekercsCsoport == berend.TekercsCsoport )
                    {
                        isModified = false;
                    }
                    else
                        isModified = true;
                }
            }                    

            return isModified;
        }

        private void GetUnmodifiedSzamlaloErtekek( ObservableCollection<Merokeszulek> merokeszulekek )
        {
            foreach( var mero in merokeszulekek )
            {
                if ( mero.Berendezesek != null )
                {
                    foreach( var berendezes in mero.Berendezesek )
                    {
                        var szamlalo = berendezes as BerendezesSzamlalo;
                        if ( szamlalo != null )
                        {
                            szamlalo.UnmodifiedMeroAllasErtek = szamlalo.MeroallasErtek;
                        }
                    }
                }
            }
        }

        private bool GetIsSzamlaloErtekekModified( Merokeszulek mero )
        {
            var isModified = false;
            if ( mero.Berendezesek != null )
            {
                if ( mero.Berendezesek.Where( b => b is BerendezesSzamlalo )
                                      .Cast<BerendezesSzamlalo>()
                                      .Any( sz => sz.MeroallasIdo < DateTime.Today ) )
                {
                    isModified = false;
                }
                else
                {
                    isModified = true;
                }
            }
            return isModified;
        }

        private void PrepareToReplaceMerokeszulek( Berendezes berend )
        {
            // Kimentjük a cserélni kívánt berendezést
            BerendezesToCsere = berend;

            // Beállítjuk az AnyagElszamView-hoz szükséges paramétereket
            BerendMuveletekHelper.IsMerokeszulek      = true;
            BerendMuveletekHelper.MeroBerendezes      = berend;
            BerendMuveletekHelper.IsBerendezesCsere   = true;
            BerendMuveletekHelper.BerendezesListIndex = Munkautasitas.Merohely.Merokeszulekek.IndexOf( (Merokeszulek)berend );
            ResetKismegszFlags();

            // Kimentjük a mérőkészülékben levő berendezéseket ha van
            SaveBerendezesekFromMero( berend );
        }

        private void PrepareToReplaceMeroberendezes( Berendezes berend )
        {
            // Kimentjük a cserélni kívánt berendezést
            BerendezesToCsere = berend;

            // Kimentjük a szülő berendezést ha van
            parentMeroBerendezes = Munkautasitas.Merohely.Merokeszulekek.Where( b => b.Berendezesek.Contains( berend ) ).FirstOrDefault();

            // Beállítjuk az AnyagElszamView-hoz szükséges paramétereket
            BerendMuveletekHelper.MeroBerendezes      = berend;
            BerendMuveletekHelper.IsBerendezesCsere   = true;
            // Ha mérő alatti készülékről van szó, akkor az alatt keressük a helyét
            if ( parentMeroBerendezes != null )
                BerendMuveletekHelper.BerendezesListIndex = parentMeroBerendezes.Berendezesek.IndexOf( berend );
            // Ha önálló berendezésről, akkor az önálló készülék listában keressük
            else
            {
                if ( berend is Modem modem && Munkautasitas.Merohely.OnalloModemek != null )
                    BerendMuveletekHelper.BerendezesListIndex = Munkautasitas.Merohely.OnalloModemek.IndexOf( modem );

                else if ( berend is Aramvalto aramvalto && Munkautasitas.Merohely.OnalloAramvaltok != null )
                    BerendMuveletekHelper.BerendezesListIndex = Munkautasitas.Merohely.OnalloAramvaltok.IndexOf( aramvalto );
            }

            ResetKismegszFlags();
        }

        private void SaveBerendezesekFromMero( Berendezes berend )
        {
            if( berend != null && berend.Berendezesek != null && berend.Berendezesek.Count > 0 )
            {
                KimentettBerendezesek = new List<Berendezes>();

                foreach( var berendezes in berend.Berendezesek )
                {
                    if( berendezes is Aramvalto )
                        KimentettBerendezesek.Add( berendezes as Aramvalto );
                    else if( berendezes is Modem )
                        KimentettBerendezesek.Add( berendezes as Modem );
                    else if( berendezes is HKV )
                        KimentettBerendezesek.Add( berendezes as HKV );
                    else if( berendezes is BerendezesSzamlalo )
                        KimentettBerendezesek.Add( berendezes as BerendezesSzamlalo );

                }
            }
        }

        private void MeroKapcsolodoFenykepek_CollectionChanged( Merokeszulek merokeszulek )
        {
            if ( merokeszulek != null )
            {
                merokeszulek.KapcsolodoFenykepek = Munkautasitas.Dokumentumok != null ?
                                                   Munkautasitas.Dokumentumok.Where( f => f.MerohberAzonosito == merokeszulek.Azonosito ).ToObservableCollection() :
                                                   null;
            }
        }

        private void KapcsolodoFenykepekFrissites_Handler(KapcsolodoFenykepekFrissitesMessage obj)
        {
            foreach (var merokeszulek in Munkautasitas.Merohely.Merokeszulekek)
            {
                MeroKapcsolodoFenykepek_CollectionChanged( merokeszulek );
            }
        }

        private void MeroAdatokModositasVisszaallitas()
        {
            UzenetSerializer serializer = new UzenetSerializer();
            var clone = serializer.CreateFromByteArray<Merohely>( Munkautasitas.MerohelyClone );

            Munkautasitas.Merohely.Merokeszulekek.Clear();
            Munkautasitas.Merohely.OnalloAramvaltok.Clear();
            Munkautasitas.Merohely.OnalloModemek.Clear();

            if( clone.Merokeszulekek.Count > 0 )
            {
                foreach( var item in clone.Merokeszulekek )
                {
                    Munkautasitas.Merohely.Merokeszulekek.Add( item );
                }
            }

            if( clone.OnalloAramvaltok.Count > 0 )
            {
                foreach( var item in clone.OnalloAramvaltok )
                {
                    Munkautasitas.Merohely.OnalloAramvaltok.Add( item );
                }
            }

            if( clone.OnalloModemek.Count > 0 )
            {
                foreach( var item in clone.OnalloModemek )
                {
                    Munkautasitas.Merohely.OnalloModemek.Add( item );
                }
            }

            // Kivesszük az elszámolt berendezéseket az elszámolt anyagok közül
            if( Munkautasitas.ElszamoltMeroBerendezesek != null && Munkautasitas.ElszamoltAnyagok != null )
            {
                foreach( var berendezes in Munkautasitas.ElszamoltMeroBerendezesek )
                {
                    var match = Munkautasitas.ElszamoltAnyagok.FirstOrDefault( b => b.Azonosito == berendezes.Azonosito || b.Megnevezes == berendezes.Tipus || b.GyariSzam == berendezes.ISUAzonosito );
                    if( match != null )
                    {
                        Munkautasitas.ElszamoltAnyagok.Remove( match );
                    }
                }
            }
            if( Munkautasitas.ElszamoltKismegsz != null && Munkautasitas.ElszamoltAnyagok != null )
            {
                foreach( var kismegsz in Munkautasitas.ElszamoltKismegsz )
                {
                    var match = Munkautasitas.ElszamoltAnyagok.FirstOrDefault( km => km.Azonosito == kismegsz.Azonosito );
                    if( match != null )
                    {
                        Munkautasitas.ElszamoltAnyagok.Remove( match );
                    }
                }
            }

            FillMeroBerendezesTulajdonsagok();
            ResetBerendMuvHelper();
            ResetKismegszFlags();
            // Visszaállítjuk az elsötétített részt
            ViewModelLocator.MainStatic.DarkenUnfocusedArea = false;
            App.Logger.Info( "Mérőcsere változtatások törölve, visszaállítás alaphelyzetbe." );
        }

        #endregion

        public bool Close()
        {

            m_LostFocusHackTimer.Tick -= M_LostFocusHackTimer_Tick;

            Munkautasitas.Merohely.Merokeszulekek.CollectionChanged   -= Berendezes_CollectionChanged;
            Munkautasitas.Merohely.OnalloAramvaltok.CollectionChanged -= Berendezes_CollectionChanged;
            Munkautasitas.Merohely.OnalloModemek.CollectionChanged    -= Berendezes_CollectionChanged;
            Messenger.Default.Unregister<KapcsolodoFenykepekFrissitesMessage>(this, KapcsolodoFenykepekFrissites_Handler);


            FillMeroBerendezesTulajdonsagok();

            return true;
        }
    }
}
