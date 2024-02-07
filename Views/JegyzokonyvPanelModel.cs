using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Geometria.GeoMobil.Client.UI.Dialogs;
using Geometria.MirtuszMobil.Client.Controls;
using Geometria.MirtuszMobil.Client.Dialogs;
using Geometria.MirtuszMobil.Client.HelperClasses;
using Geometria.MirtuszMobil.Client.Messages;
using Geometria.MirtuszMobil.Client.Properties;
using Geometria.MirtuszMobil.Client.Storages;
using Geometria.MirtuszMobil.Client.Views;
using Geometria.MirtuszMobil.Common.Converters;
using Geometria.MirtuszMobil.Common.HelperClasses;
using Geometria.MirtuszMobil.Common.Messages;
using Geometria.MirtuszService.MessageClasses;
using Geometria.MirtuszService.MessageClasses.CodeValues;
using Geometria.MirtuszService.MessageClasses.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    public class JegyzokonyvPanelModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged
;
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

        public ObservableCollection<Berendezes> Dobozok { get; set; }
        private ObservableCollection<KoDobBizonyTipRecord> BizonyitekTipusok { get; set; }
        MeroBerendezesMuveletekHelper BerendMuveletekHelper { get; set; }
        public ObservableCollection<RaktariAnyagokExtender> Matricak { get; set; }

        public bool IsAnyFenykep
        {
            get
            {
                return     Munkautasitas.Jegyzokonyv.IsFenykep1
                        || Munkautasitas.Jegyzokonyv.IsFenykep1
                        || Munkautasitas.Jegyzokonyv.IsFenykep1
                        || Munkautasitas.Jegyzokonyv.IsFenykep1;
            }
        }

        public JegyzokonyvPanelModel( Munkautasitas munkautasitas )
        {
            Munkautasitas = munkautasitas;
            GetCodeTables();
            InitJegyzokonyv();

            Matricak              = GetMatricakFromRaktar();
            BerendMuveletekHelper = new MeroBerendezesMuveletekHelper();

            Messenger.Default.Register<JegyzokonyvFenykepTorlesMessage>( this, JegyzokonyvFenykepTorlesHandler );
            Munkautasitas.Dokumentumok.CollectionChanged += Dokumentumok_CollectionChanged;
        }

        #endregion

        #region Commands

        #region DobozFelveszCommand

        private RelayCommand m_DobozFelveszCommand;

        public RelayCommand DobozFelveszCommand
        {
            get
            {
                if( m_DobozFelveszCommand == null )
                    m_DobozFelveszCommand = new RelayCommand( DobozFelveszCommandCall );
                return m_DobozFelveszCommand;
            }
        }

        private void DobozFelveszCommandCall()
        {
            var newDoboz                              = new Doboz();
            newDoboz.Azonosito                        = Storage.Instance.Session.UjAzonositoHelper.GetNextValue();
            newDoboz.JegyzokonyvAzon                  = Munkautasitas.Jegyzokonyv.Azonosito;
            newDoboz.VonalkodChangedEvent            += VonalkodChanged_Event;
            newDoboz.IsNeedToWarnTheUserChangedEvent += IsNeedToWarnTheUserChanged_Event;
            newDoboz.IsKeziVonalkodHozzaadasVisible   = true;

            Munkautasitas.Jegyzokonyv.Dobozok.Add( newDoboz );

            SetDobozSorszam( newDoboz );
            App.Logger.Info( string.Format("{0} számú doboz felvéve a {1} azonosítójú jegyzőkönyvhöz", newDoboz.Azonosito, Munkautasitas.Jegyzokonyv.Azonosito) );
        }

        #endregion

        #region BizonyitekHozzaadCommand

        private RelayCommand<Doboz> m_BizonyitekHozzaadCommand;

        public RelayCommand<Doboz> BizonyitekHozzaadCommand
        {
            get
            {
                if( m_BizonyitekHozzaadCommand == null )
                    m_BizonyitekHozzaadCommand = new RelayCommand<Doboz>( BizonyitekHozzaadCommandCall );
                return m_BizonyitekHozzaadCommand;
            }
        }

        private void BizonyitekHozzaadCommandCall( Doboz doboz )
        {
            // Ha nem valódi matrica vonalkódja szerepel a doboz vonalkód mezőjében, akkor azt töröljük
            if( !string.IsNullOrEmpty( doboz.Vonalkod ) && !Storage.Instance.Session.RaktariAnyagok.Any( ra => ra.GyariSzam == doboz.Vonalkod ) )
                ClearDobozVonalkodProperties( doboz );


            // ha olyan matrica vonalkódja szerepel a doboz vonalkód mezőjében ami már fel lett használva, akkor is töröljük
            if( !string.IsNullOrEmpty( doboz.Vonalkod ) && doboz.VonalkodIsAlreadyUsed )
                ClearDobozVonalkodProperties( doboz );

            // Ha olyan matrica vonalkódja szerepel a doboz vonalkód mezőjében ami valós, de még nem lett elszámolva, akkor is törölszük
            if( !string.IsNullOrEmpty( doboz.Vonalkod ) && doboz.IsAutoVonalkodHozzaadasVisible && !Munkautasitas.ElszamoltAnyagok.Any( ea => ea.GyariSzam == doboz.Vonalkod ) )
                ClearDobozVonalkodProperties( doboz );

            var newBizonyitek                = new Bizonyitek();
            newBizonyitek.Azonosito          = Storage.Instance.Session.UjAzonositoHelper.GetNextValue();
            newBizonyitek.DobozAzon          = doboz.Azonosito;
            newBizonyitek.BizTipusOpciok     = GetSortedBizTipOpciok( newBizonyitek );

            newBizonyitek.IsuAzonChangedEvent += doboz.ISUAzon_Changed;
            doboz.Bizonyitekok.Add( newBizonyitek );

            App.Logger.Info( string.Format( "{0} számú bizonyíték felvéve a {1} azonosítójú dobozhoz", newBizonyitek.Azonosito, newBizonyitek.DobozAzon ) );
        }

        #endregion

        #region DobozTorolCommand

        private RelayCommand<Doboz> m_DobozTorolCommand;

        public RelayCommand<Doboz> DobozTorolCommand
        {
            get
            {
                if( m_DobozTorolCommand == null )
                    m_DobozTorolCommand = new RelayCommand<Doboz>( DobozTorolCommandCall );
                return m_DobozTorolCommand;
            }
        }

        private void DobozTorolCommandCall( Doboz doboz )
        {
            if( doboz.Bizonyitekok.Count > 0 )
            {
                if( MessageBoxWpf.Show( DisplayResources.DobozLeszerelWarning, DisplayResources.Figyelmeztetes, MessageBoxButton.YesNo, MessageBoxImage.Warning ) == MessageBoxResult.Yes )
                {
                    // Biztos ami biztos, leíratkozunk a doboz eventjeiről
                    doboz.VonalkodChangedEvent            -= VonalkodChanged_Event;
                    doboz.IsNeedToWarnTheUserChangedEvent -= IsNeedToWarnTheUserChanged_Event;

                    Munkautasitas.Jegyzokonyv.Dobozok.Remove( doboz );

                    // Frissítjük a dobozok sorszámát
                    foreach( var box in Munkautasitas.Jegyzokonyv.Dobozok )
                    {
                        SetDobozSorszam( box );
                    }
                }
            }
            else
            {
                // Biztos ami biztos, leíratkozunk a doboz eventjeiről
                doboz.VonalkodChangedEvent            -= VonalkodChanged_Event;
                doboz.IsNeedToWarnTheUserChangedEvent -= IsNeedToWarnTheUserChanged_Event;

                Munkautasitas.Jegyzokonyv.Dobozok.Remove( doboz );

                // Frissítjük a dobozok sorszámát
                foreach( var box in Munkautasitas.Jegyzokonyv.Dobozok )
                {
                    SetDobozSorszam( box );
                }
            }
        }

        #endregion

        #region BizonyitekTorolCommand

        private RelayCommand<object[]> m_BizonyitekTorolCommand;

        public RelayCommand<object[]> BizonyitekTorolCommand
        {
            get
            {
                if( m_BizonyitekTorolCommand == null )
                {
                    m_BizonyitekTorolCommand = new RelayCommand<object[]>( BizonyitekTorolCommandCall );
                }
                return m_BizonyitekTorolCommand;
            }
        }

        private void BizonyitekTorolCommandCall( object[] parameters )
        {
            Bizonyitek bizonyitek = parameters == null ? null : parameters[0] as Bizonyitek;
            Doboz doboz           = parameters == null ? null : parameters[1] as Doboz;

            if ( doboz != null && bizonyitek != null )
            {
                doboz.Bizonyitekok.Remove( bizonyitek );
                if ( !doboz.Bizonyitekok.Any( b => ( b.KoTipusAzon == KoDobBizTipus.Mero || b.KoTipusAzon == KoDobBizTipus.HKV )
                                                 && !string.IsNullOrEmpty( b.IsuAzon ) ) )
                {
                    doboz.VonalkodIsVisible      = true;
                    doboz.BizIsuAzonToVonalkod = null;
                }

                doboz.ISUAzon_Changed( null, null );
            }
        }

        #endregion

        #region BizTipusChangedEventCommand

        private RelayCommand<Bizonyitek> m_BizTipusChangedEventCommand;

        public RelayCommand<Bizonyitek> BizTipusChangedEventCommand
        {
            get
            {
                if( m_BizTipusChangedEventCommand == null )
                    m_BizTipusChangedEventCommand = new RelayCommand<Bizonyitek>( BizTipusChangedEventCommandCall );
                return m_BizTipusChangedEventCommand;
            }
        }

        private void BizTipusChangedEventCommandCall( Bizonyitek biz )
        {
            if (   biz.KoTipusAzon == KoDobBizTipus.Mero
                || biz.KoTipusAzon == KoDobBizTipus.HKV
                || biz.KoTipusAzon == KoDobBizTipus.Aramvalto )
            {
                biz.BizIsuAzonOpciok.Clear();
                UpdateBizIsuOpciok( biz );
            }
            else
            {
                biz.IsBerendezes = false;
                biz.IsuAzon = null;
                UpdateBizIsuOpciok( biz );
                RemoveDobozAzonIfNoMero( biz );
            }
        }

        private void RemoveDobozAzonIfNoMero( Bizonyitek biz )
        {
            var parentDoboz = Munkautasitas.Jegyzokonyv.Dobozok.Where( d => d.Bizonyitekok.Any( b => b.IsuAzon == biz.IsuAzon ) ).FirstOrDefault();

            if ( parentDoboz != null )
            {
                var firstMeroOrHKV = parentDoboz.Bizonyitekok.Where( b => b.KoTipusAzon == KoDobBizTipus.Mero || b.KoTipusAzon == KoDobBizTipus.HKV ).FirstOrDefault();

                if( firstMeroOrHKV != null )
                {
                    parentDoboz.BizIsuAzonToVonalkod = firstMeroOrHKV.IsuAzon;
                }
                else
                    parentDoboz.BizIsuAzonToVonalkod = null;
            }
        }

        #endregion

        #region BizIsuAzonChangedEventCommand

        private RelayCommand<Bizonyitek> m_BizIsuAzonChangedEventCommand;

        public RelayCommand<Bizonyitek> BizIsuAzonChangedEventCommand
        {
            get
            {
                if( m_BizIsuAzonChangedEventCommand == null )
                    m_BizIsuAzonChangedEventCommand = new RelayCommand<Bizonyitek>( BizIsuAzonChangedEventCommandCall );
                return m_BizIsuAzonChangedEventCommand;
            }
        }

        private void BizIsuAzonChangedEventCommandCall( Bizonyitek biz )
        {
            UpdateBizIsuOpciok( biz );
        }

        private void UpdateBizIsuOpciok( Bizonyitek biz )
        {
            if( Munkautasitas.Jegyzokonyv.Dobozok != null && Munkautasitas.Jegyzokonyv.Dobozok.Count > 0 )
            {
                foreach( var doboz in Munkautasitas.Jegyzokonyv.Dobozok )
                {
                    if( doboz.Bizonyitekok != null && doboz.Bizonyitekok.Count > 0 )
                    {
                        foreach( var bizonyitek in doboz.Bizonyitekok )
                        {
                            if( (bizonyitek.KoTipusAzon == KoDobBizTipus.Mero
                                  || bizonyitek.KoTipusAzon == KoDobBizTipus.HKV
                                  || bizonyitek.KoTipusAzon == KoDobBizTipus.Aramvalto) )
                            {
                                bizonyitek.BizIsuAzonOpciok = GetLeszereltKeszISUAzon( bizonyitek );
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region FenykepCsatolasCommand

        private RelayCommand<CsatolasParameter> m_FenykepCsatolasCommand;

        public RelayCommand<CsatolasParameter> FenykepCsatolasCommand
        {
            get
            {
                if( m_FenykepCsatolasCommand == null )
                {
                    m_FenykepCsatolasCommand = new RelayCommand<CsatolasParameter>( FenykepCsatolasCommandCall );
                }
                return m_FenykepCsatolasCommand;
            }
        }

        private void FenykepCsatolasCommandCall( CsatolasParameter parameterek )
        {
            string hibaUzenet = "";
            var isFenykepCsatolhato = GetIsFenykepCsatolhato( ref hibaUzenet );
            if( !isFenykepCsatolhato && string.IsNullOrEmpty( hibaUzenet ) )
                return;

            if ( !isFenykepCsatolhato )
            {
                MessageBoxWpf.Show( hibaUzenet, DisplayResources.Figyelmeztetes, MessageBoxButton.OK, MessageBoxImage.Warning );
            }
            else
            {
                string fenykepOldalSzam = parameterek.Data as string;

                var oldalszam = 0;
                if( fenykepOldalSzam.Equals( "1", System.StringComparison.InvariantCultureIgnoreCase ) )
                {
                    oldalszam = 1;
                }
                else if( fenykepOldalSzam.Equals( "2", System.StringComparison.InvariantCultureIgnoreCase ) )
                {
                    oldalszam = 2;
                }
                else if( fenykepOldalSzam.Equals( "3", System.StringComparison.InvariantCultureIgnoreCase ) )
                {
                    oldalszam = 3;
                }
                else if( fenykepOldalSzam.Equals( "4", System.StringComparison.InvariantCultureIgnoreCase ) )
                {
                    oldalszam = 4;
                }
                else if ( fenykepOldalSzam.Equals( "TajekoztatoLap", System.StringComparison.InvariantCultureIgnoreCase ) )
                {
                    oldalszam = 5;
                }

                Munkautasitas.Jegyzokonyv.OldalszamToAttach = oldalszam;
                CsatolasWindow.CreateAndShowPopup( new CsatolasViewModel(), parameterek.ParentControl as FrameworkElement );

            }
            RaisePropertyChanged( nameof(IsAnyFenykep) );
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
                    App.Logger.WarnException( "Ismeretlen file tipus ", ex );
                    MessageBoxWpf.Show( DisplayResources.IsmeretlenFile + fenykep.FAJL_NEV );
                }
            }
            else
            {
                MessageBoxWpf.Show( string.Format( DisplayResources.FajlNemtalalhato, fenykep.LocalPath ) );
                App.Logger.Error( string.Format(" {0} fénykép megnyitása sikertelen, mert nem található!", fenykep.LocalPath) );
            }
        }
        #endregion

        #region KeziVonalkodFelszerelesCommand

        private RelayCommand<Doboz> m_KeziVonalkodFelszerelesCommand;

        public RelayCommand<Doboz> KeziVonalkodFelszerelesCommand
        {
            get
            {
                if( m_KeziVonalkodFelszerelesCommand == null )
                    m_KeziVonalkodFelszerelesCommand = new RelayCommand<Doboz>( KeziVonalkodFelszerelesCommandCall );
                return m_KeziVonalkodFelszerelesCommand;
            }
        }

        private void KeziVonalkodFelszerelesCommandCall( IVonalkodos doboz )
        {
            // Beállítjuk a raktári anyagok szűréséhez a szükséges paramétert
            SetParametersForAnyagelszamView( doboz );

            Messenger.Default.Register<AddBerendezesMunka>( this, Add_VonalkodMatricaToDoboz );

            ViewManager.Instance.Open( new AnyagElszamView( Munkautasitas, BerendMuveletekHelper, true ) );
        }

        private void Add_VonalkodMatricaToDoboz( AddBerendezesMunka anyagHelper )
        {
            RaktariAnyagokExtender item = anyagHelper.Anyag;
            IVonalkodos szulo           = anyagHelper.Szulo;

            var doboz = szulo as Doboz;
            if ( doboz != null )
            {
                doboz.VonalkodIsAlreadyUsed          = false;
                doboz.Vonalkod                       = item.GyariSzam;
                doboz.VonalkodIsVisible              = false;
                doboz.BizIsuAzonToVonalkod           = null;
                doboz.IsAutoVonalkodHozzaadasVisible = false;
                doboz.IsKeziVonalkodHozzaadasVisible = false;
                doboz.IsVonalkodTorlesVisible        = true;
            }

            Messenger.Default.Unregister<AddBerendezesMunka>( this, Add_VonalkodMatricaToDoboz );
            BerendMuveletekHelper.IsJegyzokonyvMatrica = false;
            App.Logger.Info( string.Format( " {0} gyári számú vonalkód felvéve a {1} azonosítójú dobozhoz", item.GyariSzam, doboz.Azonosito ) );
        }

        #endregion

        #region AutomatikusVonalkodFelszerelesCommand

        private RelayCommand<Doboz> m_AutomatikusVonalkodFelszerelesCommand;

        public RelayCommand<Doboz> AutomatikusVonalkodFelszerelesCommand
        {
            get
            {
                if( m_AutomatikusVonalkodFelszerelesCommand == null )
                    m_AutomatikusVonalkodFelszerelesCommand = new RelayCommand<Doboz>( AutomatikusVonalkodFelszerelesCommandCall );
                return m_AutomatikusVonalkodFelszerelesCommand;
            }
        }

        private void AutomatikusVonalkodFelszerelesCommandCall( IVonalkodos doboz )
        {
            // Frissítjük az raktari anyagokat, ha esetleg közben az általános elszámolást megnyitotta
            Matricak = GetMatricakFromRaktar();
            var dobozToAddVonalkod = doboz as Doboz;

            var matrica = Storage.Instance.Session.AnyagElszamolasok.Where( e => e.GyariSzam.Equals( doboz.Vonalkod ) )
                                                                    .FirstOrDefault();

            if ( dobozToAddVonalkod != null && matrica != null )
            {
                VonalkodMatricaElszamolasRaktarbol( dobozToAddVonalkod, matrica );
                dobozToAddVonalkod.FoundRaktariAnyagTulajdonsagok = null;
            }
            
        }

        #endregion

        #region VonalkodLelszerelesCommand

        private RelayCommand<Doboz> m_VonalkodLeszerelesCommand;

        public RelayCommand<Doboz> VonalkodLeszerelesCommand
        {
            get
            {
                if( m_VonalkodLeszerelesCommand == null )
                    m_VonalkodLeszerelesCommand = new RelayCommand<Doboz>( VonalkodLeszerelesCommandCall );
                return m_VonalkodLeszerelesCommand;
            }
        }

        private void VonalkodLeszerelesCommandCall( IVonalkodos doboz )
        {
            var dobozFromRemoveVonalkod = doboz as Doboz;
            var elszamoltMatrica = Munkautasitas.ElszamoltAnyagok.Where( a => a.GyariSzam == dobozFromRemoveVonalkod.Vonalkod )
                                                                 .FirstOrDefault();


            if( elszamoltMatrica != null && dobozFromRemoveVonalkod != null )
            {
                var anyag = Storage.Instance.Session.AnyagElszamolasok.Where( e => e.Cikkszam.Equals( elszamoltMatrica.Cikkszam ) &&
                                                                                   e.GyariSzam.Equals( elszamoltMatrica.GyariSzam ) &&
                                                                                   e.Megnevezes.Equals( elszamoltMatrica.Megnevezes ) )
                                                                      .FirstOrDefault();

                if( anyag != null )
                    anyag.FelhasznaltMennyiseg = 0;

                dobozFromRemoveVonalkod.Vonalkod                       = null;
                dobozFromRemoveVonalkod.IsVonalkodTorlesVisible        = false;
                dobozFromRemoveVonalkod.IsKeziVonalkodHozzaadasVisible = true;
                dobozFromRemoveVonalkod.IsElszamoltRaktariAnyagFound   = false;
                dobozFromRemoveVonalkod.VonalkodIsAlreadyUsed          = false;
                Munkautasitas.ElszamoltAnyagok.Remove( elszamoltMatrica );
            }
            else if ( !Munkautasitas.ElszamoltAnyagok.Any( ea => ea.GyariSzam == dobozFromRemoveVonalkod.Vonalkod ) )
            {
                dobozFromRemoveVonalkod.Vonalkod = null;
            }
        }

        #endregion

        #endregion

        #region Helper Methods

        /// <summary>
        /// Létrehozza a jegyzőkönyvet, ha még nem tartozik a munkához egy sem
        /// </summary>
        private void InitJegyzokonyv()
        {
            try
            {
                if ( Munkautasitas.Jegyzokonyv == null )
                {
                    Munkautasitas.Jegyzokonyv = new Jegyzokonyv() 
                    {
                        Azonosito = Storage.Instance.Session.UjAzonositoHelper.GetNextValue(),
                        KomplexMunAzon = Munkautasitas.KomplexMunkaAdatai.Azonosito,
                        IsFenykep1 = false,
                        IsFenykep2 = false,
                        IsFenykep3 = false,
                        IsFenykep4 = false
                    };

                    if ( !Munkautasitas.Jegyzokonyv.LeszereltMerokDobozolva )
                        CreateBizFromLeszereltMero();
                }
                else
                    FillJegyzokonyvTulajdonsagok();
            }
            catch ( Exception ex )
            {
                App.Logger.Warn( "A jegyzőkönvy panel inicializálása sikertelen a következő hiba miatt: ", ex );
            }
            
        }

        /// <summary>
        /// Kitölti a dobozok sorszámát, valamint a bizonyítékok tipus comboboxát
        /// </summary>
        private void FillJegyzokonyvTulajdonsagok()
        {
            if ( Munkautasitas.Jegyzokonyv.Dobozok.Count > 0 )
            {
                foreach( var doboz in Munkautasitas.Jegyzokonyv.Dobozok )
                {
                    SetDobozSorszam( doboz );
                    SetVonalkodIsReadOnly( doboz );
                    doboz.VonalkodChangedEvent             += VonalkodChanged_Event;
                    doboz.IsNeedToWarnTheUserChangedEvent  += IsNeedToWarnTheUserChanged_Event;
                    doboz.IsKeziVonalkodHozzaadasVisible    = ( string.IsNullOrEmpty( doboz.Vonalkod ) && string.IsNullOrEmpty( doboz.BizIsuAzonToVonalkod ) );
                    doboz.IsAutoVonalkodHozzaadasVisible    = !string.IsNullOrEmpty( doboz.FoundRaktariAnyagTulajdonsagok );
                    doboz.IsElszamoltRaktariAnyagFound      = false;
                    doboz.IsVonalkodTorlesVisible           = ( !string.IsNullOrEmpty( doboz.Vonalkod ) && !doboz.IsAutoVonalkodHozzaadasVisible );

                    if ( doboz.Bizonyitekok.Count > 0 )
                    {
                        foreach( var bizonyitek in doboz.Bizonyitekok )
                        {
                            bizonyitek.IsuAzonChangedEvent += doboz.ISUAzon_Changed;

                            // Ha van már gyári száma, csak egy opciót rakunk a gyáris szám comboboxba
                            if ( !string.IsNullOrEmpty( bizonyitek.IsuAzon ) )
                                bizonyitek.BizIsuAzonOpciok = new ObservableCollection<string>() { bizonyitek.IsuAzon };
                            // Ha gyáriszámos készülékről van szó, megjelenítjük a gyári szám comboboxot
                            if (    bizonyitek.KoTipusAzon == KoDobBizTipus.Mero
                                ||  bizonyitek.KoTipusAzon == KoDobBizTipus.HKV 
                                ||  bizonyitek.KoTipusAzon == KoDobBizTipus.Aramvalto )
                            {
                                bizonyitek.IsBerendezes = true;
                            }
                            // Ha már van tipus azonosítója, csak azt a típust jelenítjük meg a comboboxban
                            if ( BizonyitekTipusok != null && bizonyitek.KoTipusAzon != 0 )
                            {
                                bizonyitek.BizTipusOpciok = new ObservableCollection<KoDobBizonyTipRecord>()
                                {
                                    BizonyitekTipusok.Where( bt => bt.Azonosito == bizonyitek.KoTipusAzon ).FirstOrDefault()
                                };
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// A leszerelt mérőkből készít dobozolt bizonyítékokat
        /// </summary>
        private void CreateBizFromLeszereltMero()
        {
            if ( Munkautasitas.TempLeszereltBerendezesek != null && Munkautasitas.TempLeszereltBerendezesek != null )
            {
                foreach( var berend in Munkautasitas.TempLeszereltBerendezesek )
                {
                    if ( berend is Merokeszulek mero && mero.Leszerelt )
                    {
                        var newDoboz                              = new Doboz();
                        newDoboz.Azonosito                        = Storage.Instance.Session.UjAzonositoHelper.GetNextValue();
                        newDoboz.JegyzokonyvAzon                  = Munkautasitas.Jegyzokonyv.Azonosito;
                        newDoboz.VonalkodIsVisible                = !string.IsNullOrEmpty( mero.ISUAzonosito );
                        newDoboz.BizIsuAzonToVonalkod             = !string.IsNullOrEmpty( mero.ISUAzonosito ) ? mero.ISUAzonosito : null;
                        newDoboz.VonalkodChangedEvent            += VonalkodChanged_Event;
                        newDoboz.IsKeziVonalkodHozzaadasVisible   = string.IsNullOrEmpty( mero.ISUAzonosito );
                        newDoboz.IsNeedToWarnTheUserChangedEvent += IsNeedToWarnTheUserChanged_Event;

                        var newBizonyitek                = new Bizonyitek();
                        newBizonyitek.Azonosito          = Storage.Instance.Session.UjAzonositoHelper.GetNextValue();
                        newBizonyitek.DobozAzon          = newDoboz.Azonosito;
                        newBizonyitek.IsuAzon            = berend.ISUAzonosito;
                        newBizonyitek.BizTipusOpciok     = BizonyitekTipusok != null ? BizonyitekTipusok.Where( bt => bt.Azonosito == KoDobBizTipus.Mero ).ToObservableCollection() : null; // Csak mérő típus kell a listába

                        if ( BizonyitekTipusok != null )
                        {
                            var bizTipus = BizonyitekTipusok.Where( b => b.Azonosito == KoDobBizTipus.Mero ).FirstOrDefault();
                            if ( bizTipus != null )
                                newBizonyitek.KoTipusAzon = bizTipus.Azonosito;
                        }

                        newBizonyitek.IsBerendezes         = true;
                        newBizonyitek.BizIsuAzonOpciok     = new ObservableCollection<string>(){ mero.ISUAzonosito };
                        newBizonyitek.IsuAzonChangedEvent += newDoboz.ISUAzon_Changed;

                        newDoboz.Bizonyitekok.Add( newBizonyitek );
                        Munkautasitas.Jegyzokonyv.Dobozok.Add( newDoboz );
                        SetDobozSorszam( newDoboz );
                    }
                }
                Munkautasitas.Jegyzokonyv.LeszereltMerokDobozolva         = true;
                Munkautasitas.Jegyzokonyv.IsTajekoztatoLapKerdesNyugtazva = false;
            } 
        }

        /// <summary>
        /// Visszaadja egy újonnan létrehozott bizonyíték választható típusait, hogy csak azokat a típusokat lehessen kiválasztani, ami még nem lett bedobozolva
        /// </summary>
        /// <param name="newBizonyitek">Az új bizonyíték, aminek a típuslistáját kell szűrni</param>
        /// <returns>A leszűrt típus lista</returns>
        private ObservableCollection<KoDobBizonyTipRecord> GetSortedBizTipOpciok( Bizonyitek newBizonyitek )
        {
            var result                           = new ObservableCollection<KoDobBizonyTipRecord>();
            var isAnyMeroWithoutDoboz            = false;
            var isAnyHKVWithoutDoboz             = false;
            var isAnyOrphanHKVWithoutDoboz       = false;
            var isAnyAramvaltoWithoutDoboz       = false;
            var isAnyOrphanAramvaltoWithoutDoboz = false;

            if ( Munkautasitas.TempLeszereltBerendezesek != null && Munkautasitas.TempLeszereltBerendezesek.Count > 0 )
            {
                var dobozoltKeszIsuAzonositok    = Munkautasitas.Jegyzokonyv.Dobozok.SelectMany( d => d.Bizonyitekok.Select( db => db.IsuAzon ) )
                                                                                    .ToList();

                isAnyMeroWithoutDoboz            = Munkautasitas.TempLeszereltBerendezesek.Where( mero => mero.Leszerelt && mero is Merokeszulek )
                                                                                          .Any( m => !dobozoltKeszIsuAzonositok.Contains( m.ISUAzonosito ) );

                isAnyHKVWithoutDoboz             = Munkautasitas.TempLeszereltBerendezesek.Where( mero => mero.Leszerelt )
                                                                                          .SelectMany( b => b.Berendezesek.OfType<HKV>() )
                                                                                          .Any( m => !dobozoltKeszIsuAzonositok.Contains( m.ISUAzonosito ) );

                isAnyOrphanHKVWithoutDoboz       = Munkautasitas.TempLeszereltBerendezesek.Where( berend => berend.Leszerelt && berend is HKV )
                                                                                          .Any( b => !dobozoltKeszIsuAzonositok.Contains( b.ISUAzonosito ) );

                isAnyAramvaltoWithoutDoboz       = Munkautasitas.TempLeszereltBerendezesek.Where( mero => mero.Leszerelt )
                                                                                          .SelectMany( b => b.Berendezesek.OfType<Aramvalto>() )
                                                                                          .Any( m => !dobozoltKeszIsuAzonositok.Contains( m.ISUAzonosito ) );

                isAnyOrphanAramvaltoWithoutDoboz = Munkautasitas.TempLeszereltBerendezesek.Where( berend => berend.Leszerelt && berend is Aramvalto )
                                                                                          .Any( b => !dobozoltKeszIsuAzonositok.Contains( b.ISUAzonosito ) );

                if ( BizonyitekTipusok != null && BizonyitekTipusok.Count > 0 )
                {
                    foreach( var tipus in BizonyitekTipusok )
                    {
                        if( tipus.Azonosito == KoDobBizTipus.Mero && isAnyMeroWithoutDoboz )
                            result.Add( tipus );
                        if( tipus.Azonosito == KoDobBizTipus.HKV && ( isAnyHKVWithoutDoboz || isAnyOrphanHKVWithoutDoboz ) )
                            result.Add( tipus );
                        if( tipus.Azonosito == KoDobBizTipus.Aramvalto && ( isAnyAramvaltoWithoutDoboz || isAnyOrphanAramvaltoWithoutDoboz ) )
                            result.Add( tipus );
                        if ( tipus.Azonosito == KoDobBizTipus.Feszultsegvalto )
                            result.Add( tipus );
                        if( tipus.Azonosito == KoDobBizTipus.Fovezetek )
                            result.Add( tipus );
                        if( tipus.Azonosito == KoDobBizTipus.Kismegszakito )
                            result.Add( tipus );
                        if( tipus.Azonosito == KoDobBizTipus.Egyeb )
                            result.Add( tipus );
                    }
                }
            }
            else // Ha nincs leszerelt mérő, akkor a bizonyítéktípus listát feltöltjük úgy, hogy ne legyen benne Mérő, HKV vagy Áramváltó
            {
                if ( BizonyitekTipusok != null && BizonyitekTipusok.Count > 0 )
                    result = BizonyitekTipusok.Where( bt => bt.Azonosito != KoDobBizTipus.Mero && bt.Azonosito != KoDobBizTipus.HKV && bt.Azonosito != KoDobBizTipus.Aramvalto ).ToObservableCollection();
            }

            return result;
        }

        /// <summary>
        /// Visszaadja azokat a készülék gyári számokat, amik még nem lettek bedobozolva
        /// </summary>
        /// <param name="biz">Az új bizonyíték, aminek a gyáriszám listáját fel kell tölteni</param>
        /// <returns>A kiszűrt ISU azonosítók lista</returns>
        private ObservableCollection<string> GetLeszereltKeszISUAzon( Bizonyitek biz )
        {
            var result = new ObservableCollection<string>();

            if ( biz != null && Munkautasitas.TempLeszereltBerendezesek != null && Munkautasitas.TempLeszereltBerendezesek.Count > 0 )
            {
                if ( biz.KoTipusAzon == KoDobBizTipus.Mero )
                {
                    foreach( var keszulek in Munkautasitas.TempLeszereltBerendezesek )
                    {
                        var isAlreadyInDoboz = Munkautasitas.Jegyzokonyv.Dobozok.Any( d => d.Bizonyitekok.Any( b => b.IsuAzon == keszulek.ISUAzonosito ) );
                        if ( keszulek.Leszerelt && keszulek is Merokeszulek && !isAlreadyInDoboz || keszulek.ISUAzonosito == biz.IsuAzon )
                        {
                            biz.IsBerendezes = true;
                            result.Add( keszulek.ISUAzonosito );
                        }
                    }
                }
                else if ( biz.KoTipusAzon == KoDobBizTipus.HKV )
                {
                    biz.IsBerendezes = true;

                    // Mérő nélküli leszerelt HKV
                    foreach ( var keszulek in Munkautasitas.TempLeszereltBerendezesek )
                    {
                        if ( keszulek is HKV )
                        {
                            var isAlreadyInDoboz = Munkautasitas.Jegyzokonyv.Dobozok.Any( d => d.Bizonyitekok.Any( b => b.IsuAzon == keszulek.ISUAzonosito ) );
                            if ( keszulek.Leszerelt && keszulek is HKV && !isAlreadyInDoboz || keszulek.ISUAzonosito == biz.IsuAzon )
                            {
                                biz.IsBerendezes = true;
                                result.Add( keszulek.ISUAzonosito );
                            }
                        }
                    }

                    // Mérő alatti leszerelt HKV
                    foreach ( var keszulek in Munkautasitas.TempLeszereltBerendezesek )
                    {
                        if ( keszulek.Leszerelt )
                        {
                            foreach( var alKeszulek in keszulek.Berendezesek )
                            {
                                if ( alKeszulek is HKV )
                                {
                                    var isAlreadyInDoboz = Munkautasitas.Jegyzokonyv.Dobozok.Any( d => d.Bizonyitekok.Any( b => b.IsuAzon == alKeszulek.ISUAzonosito ) );
                                    if( alKeszulek.Leszerelt && alKeszulek is HKV && !isAlreadyInDoboz || alKeszulek.ISUAzonosito == biz.IsuAzon )
                                    {
                                        biz.IsBerendezes = true;
                                        result.Add( alKeszulek.ISUAzonosito );
                                    }
                                }
                            }
                        }
                    }
                }
                else if( biz.KoTipusAzon == KoDobBizTipus.Aramvalto )
                {
                    biz.IsBerendezes = true;

                    // Mérő nélküli leszerelt Áramváltó
                    foreach ( var keszulek in Munkautasitas.TempLeszereltBerendezesek )
                    {
                        if ( keszulek is Aramvalto )
                        {
                            var isAlreadyInDoboz = Munkautasitas.Jegyzokonyv.Dobozok.Any( d => d.Bizonyitekok.Any( b => b.IsuAzon == keszulek.ISUAzonosito ) );
                            if ( keszulek.Leszerelt && keszulek is Aramvalto && !isAlreadyInDoboz || keszulek.ISUAzonosito == biz.IsuAzon )
                            {
                                biz.IsBerendezes = true;
                                result.Add( keszulek.ISUAzonosito );
                            }
                        }
                    }

                    // Mérő alatti leszerelt Áramváltó
                    foreach ( var keszulek in Munkautasitas.TempLeszereltBerendezesek )
                    {
                        if( keszulek.Leszerelt )
                        {
                            foreach( var alKeszulek in keszulek.Berendezesek )
                            {
                                if( alKeszulek is Aramvalto )
                                {
                                    var isAlreadyInDoboz = Munkautasitas.Jegyzokonyv.Dobozok.Any( d => d.Bizonyitekok.Any( b => b.IsuAzon == alKeszulek.ISUAzonosito ) );
                                    if( alKeszulek.Leszerelt && alKeszulek is Aramvalto && !isAlreadyInDoboz || alKeszulek.ISUAzonosito == biz.IsuAzon )
                                    {
                                        biz.IsBerendezes = true;
                                        result.Add( alKeszulek.ISUAzonosito );
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Beállítja jegyzőkönyvhez tartozó aktuális doboz sorszámát a UI-on való kiíráshoz
        /// </summary>
        /// <param name="newDoboz">Az update-elni kívánt doboz</param>
        private void SetDobozSorszam( Doboz newDoboz )
        {
            StringBuilder sb = new StringBuilder();
            sb.Append( Munkautasitas.Jegyzokonyv.Dobozok.IndexOf( newDoboz ) + 1 );
            sb.Append( "." );
            newDoboz.Sorszam = sb.ToString();
        }

        /// <summary>
        /// Kódtáblákat kér le
        /// </summary>
        private void GetCodeTables()
        {
            var bizonyitekTipusok = StoredTableHelper.Instance.GetRecord<KoDobBizonyTipRecord>();
            if( bizonyitekTipusok != null )
                BizonyitekTipusok = bizonyitekTipusok.ToObservableCollection();
        }

        /// <summary>
        /// A dokumentumok felületről törlésre kerülő képek törlését intézi a jegyzőkönyvről
        /// </summary>
        /// <param name="message">A DocManager osztályból érkező üzenet</param>
        private void JegyzokonyvFenykepTorlesHandler( JegyzokonyvFenykepTorlesMessage message )
        {
            if ( message != null )
            {
                if( message.OldalSzam == 1 )
                {
                    Munkautasitas.Jegyzokonyv.Fenykep1   = null;
                    Munkautasitas.Jegyzokonyv.IsFenykep1 = false;
                }
                if( message.OldalSzam == 2 )
                {
                    Munkautasitas.Jegyzokonyv.Fenykep2   = null;
                    Munkautasitas.Jegyzokonyv.IsFenykep2 = false;
                }
                if( message.OldalSzam == 3 )
                {
                    Munkautasitas.Jegyzokonyv.Fenykep3   = null;
                    Munkautasitas.Jegyzokonyv.IsFenykep3 = false;
                }
                if( message.OldalSzam == 4 )
                {
                    Munkautasitas.Jegyzokonyv.Fenykep4   = null;
                    Munkautasitas.Jegyzokonyv.IsFenykep4 = false;
                }
                if ( message.OldalSzam == 5 )
                {
                    Munkautasitas.Jegyzokonyv.TajekoztatoLap           = null;
                    Munkautasitas.Jegyzokonyv.IsTajekoztatoLapCsatolva = false;
                }
            }
            RaisePropertyChanged( nameof( IsAnyFenykep ) );

            // Ha nincs minden oldalról csatolva a kép, megint kérdezzük meg van-e ellenőrzést megelőző tajákoztató lap
            if ( !Munkautasitas.Jegyzokonyv.IsAllOldalFenykepCsatolva && Munkautasitas.Jegyzokonyv.TajekoztatoLap == null )
                Munkautasitas.Jegyzokonyv.IsTajekoztatoLapKerdesNyugtazva = false;
        }

        /// <summary>
        /// Beállítja a doboz vonalkódjának szerkeszthetőségét az panel inicializálásakor
        /// </summary>
        /// <param name="doboz"></param>
        private void SetVonalkodIsReadOnly( Doboz doboz )
        {
            if( doboz != null && doboz.Vonalkod == null && doboz.Bizonyitekok != null && doboz.Bizonyitekok.Count > 0 )
                doboz.VonalkodIsVisible = doboz.Bizonyitekok.Any( b => !string.IsNullOrEmpty( b.IsuAzon ) );
        }

        /// <summary>
        /// Leiratkozik a dobozok vonalkód, valamint a bizonyítékok gyáriszám változását figyelő eventről
        /// </summary>
        private void IsuAzonAndVonalkodChangedEvent_Leiratkozas()
        {
            if ( Munkautasitas.Jegyzokonyv != null && Munkautasitas.Jegyzokonyv.Dobozok.Count > 0 )
            {
                foreach( var doboz in Munkautasitas.Jegyzokonyv.Dobozok )
                {
                    doboz.VonalkodChangedEvent            -= VonalkodChanged_Event;
                    doboz.IsNeedToWarnTheUserChangedEvent -= IsNeedToWarnTheUserChanged_Event;

                    if ( doboz.Bizonyitekok != null && doboz.Bizonyitekok.Count > 0 )
                    {
                        foreach( var bizonyitek in doboz.Bizonyitekok )
                        {
                            bizonyitek.IsuAzonChangedEvent -= doboz.ISUAzon_Changed;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// A vonalkóddal rendelkező objektum szövegmezőjébe beírt érték alapján keresi meg az értékhez tartozó raktári anyagot.
        /// </summary>
        /// <param name="sender">A vonalkóddal rendelkező objektum</param>
        /// <param name="e"></param>
        private void VonalkodChanged_Event( object sender, EventArgs e )
        {
            var doboz = sender as Doboz;
            if ( doboz != null )
            {
                StringBuilder sb = new StringBuilder();

                // Megkeressük van-e ilyen anyag a raktárban
                var foundMatrica = Matricak.Where( m => m.KomerohberAzonosito == MerohberTipus.DobozVonalkodMatrica
                                                     && m.GyariSzam == doboz.Vonalkod )
                                           .FirstOrDefault();

                // Megnézzük elvan-e már számolva a talált anyag
                var isElszamolva = Munkautasitas.ElszamoltAnyagok.Any( em => em.KomerohberAzonosito == MerohberTipus.DobozVonalkodMatrica
                                                                          && em.GyariSzam == doboz.Vonalkod );


                // Megnézzük, máshol el lett-e számolva a matrica, ha igen, az nem jó, itt nem engedjük felhasználni
                bool isMasholElszamolva = false;
                if ( foundMatrica != null && isElszamolva )
                {
                    var mindenMasDoboz                  = Munkautasitas.Jegyzokonyv.Dobozok.Where( d => d != doboz );
                    isMasholElszamolva                  = mindenMasDoboz.Any( d => d.Vonalkod == foundMatrica.GyariSzam );
                    doboz.IsHibasVonalkodMessageVisible = false;
                }
                    
                // Minden ok, el lehet számolni
                if( foundMatrica != null && !isElszamolva )
                {
                    doboz.IsKeziVonalkodHozzaadasVisible = false;
                    doboz.IsAutoVonalkodHozzaadasVisible = true;
                    doboz.IsVonalkodTorlesVisible        = false;
                    doboz.VonalkodIsAlreadyUsed          = false;
                    doboz.IsHibasVonalkodMessageVisible  = false;

                    sb.Append( foundMatrica.Cikkszam + "  " );
                    sb.Append( foundMatrica.Megnevezes + "  " );
                    sb.Append( foundMatrica.GyariSzam );
                    doboz.FoundRaktariAnyagTulajdonsagok = sb.ToString();
                }
                else if ( isMasholElszamolva )
                {
                    doboz.VonalkodIsAlreadyUsed = true;
                }
                else // Nincs ilyen matrica a raktárban
                {
                    sb.Append( "Ezzel a vonalkóddal nem található raktári anyag!" );
                    doboz.HibasVonalkodMessage = sb.ToString();

                    if ( string.IsNullOrEmpty( doboz.BizIsuAzonToVonalkod ) )
                        doboz.IsKeziVonalkodHozzaadasVisible = true;
                    doboz.IsAutoVonalkodHozzaadasVisible = false;
                    doboz.IsVonalkodTorlesVisible        = false;
                    doboz.IsElszamoltRaktariAnyagFound   = false;
                    doboz.VonalkodIsAlreadyUsed          = false;

                    if ( foundMatrica != null && doboz.IsHibasVonalkodMessageVisible == false )
                        return;
                    doboz.IsHibasVonalkodMessageVisible  = !string.IsNullOrEmpty( doboz.Vonalkod ) ? true : false;
                }
            }
        }

        /// <summary>
        /// Feldob egy figyelmeztető ablakot, ha úgy ad hozzá gyári számmal rendelkező mérőt a bizonyítékok közé
        /// hogy arra a dobozra már elszámolt egy matricát
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IsNeedToWarnTheUserChanged_Event( object sender, EventArgs e )
        {
            var doboz = sender as Doboz;
            if( doboz != null )
            {
                // Megkeressük valós matrica van-e a vonalkód mezőben
                var foundMatrica = Matricak.Where( m => m.KomerohberAzonosito == MerohberTipus.DobozVonalkodMatrica
                                                     && m.GyariSzam == doboz.Vonalkod )
                                           .FirstOrDefault();

                // Vagy ha van ilyen de már el lett számolva
                var isElszamolva = Munkautasitas.ElszamoltAnyagok.Any( em => em.KomerohberAzonosito == MerohberTipus.DobozVonalkodMatrica
                                                                          && em.GyariSzam == doboz.Vonalkod );

                // csak akkor jelenítjük meg az üzenetet, ha valós matrica szerepel a vonalkód mezőben
                if ( doboz.IsNeedToWarnTheUser && !doboz.WarnTheUserNyugtazva && ( foundMatrica != null || isElszamolva ) )
                {
                    MessageBoxWpf.Show( DisplayResources.JegyzkvElszamoltMatricaFelulirasWarning, DisplayResources.Figyelmeztetes, MessageBoxButton.OK, MessageBoxImage.Warning );
                    doboz.WarnTheUserNyugtazva = true;
                }
            }
        }

        /// <summary>
        /// Kiveszi a raktárból a felhasznált vonalkód matricát, majd elmenti az anyagelszámolások közé
        /// </summary>
        private void VonalkodMatricaElszamolasRaktarbol( Doboz doboz,  RaktariAnyagokExtender anyagToUse )
        {
            // Megnézzük elvan-e már számolva a talált anyag
            var isElszamolva = Munkautasitas.ElszamoltAnyagok.Any( em => em.KomerohberAzonosito == MerohberTipus.DobozVonalkodMatrica
                                                                      && em.GyariSzam == doboz.Vonalkod );

            if( isElszamolva )
                MessageBoxWpf.Show( DisplayResources.JegyzkvElszamoltAnyagWarning, DisplayResources.Figyelmeztetes, MessageBoxButton.OK, MessageBoxImage.Error );

            if ( !isElszamolva && anyagToUse.KeszletenGUI > 0 )
            {
                anyagToUse.FelhasznaltMennyiseg = 1;
                AddElszamoltAnyagToMunka( anyagToUse );

                doboz.IsVonalkodTorlesVisible        = true;
                doboz.IsKeziVonalkodHozzaadasVisible = false;
                doboz.IsAutoVonalkodHozzaadasVisible = false;
            }
        }

        /// <summary>
        /// Visszaadja a vonalkód matrica anyagokat a raktárkészletből egy ObservableCollection-be csomagolva.
        /// </summary>
        /// <returns>A megtalált vonalkód matrica anyagok egy collectionben</returns>
        private ObservableCollection<RaktariAnyagokExtender> GetMatricakFromRaktar()
        {
            ObservableCollection<RaktariAnyagokExtender> result = new ObservableCollection<RaktariAnyagokExtender>();

            foreach( var item in Storage.Instance.Session.RaktariAnyagok )
            {
                if ( item.KomerohberAzonosito == MerohberTipus.DobozVonalkodMatrica )
                {
                    var anyag = new RaktariAnyagokExtender
                    {
                        Azonosito                          = item.Azonosito,
                        Cikkszam                           = item.Cikkszam,
                        Megnevezes                         = item.Megnevezes,
                        Mertekegys                         = item.Mertekegys,
                        KaraktarhAzonosito                 = item.KaraktarhAzonosito,
                        ElemimunkaAzonosito                = Munkautasitas.ElemimunkaAzonosito,
                        Mennyiseg                          = (decimal)item.Mennyiseg,
                        GyariSzam                          = item.GyariSzam,
                        ErtekelesFajta                     = item.ErtekelesFajta,
                        Sarzs                              = item.Sarzs,
                        MennyisegMm                        = item.MennyisegMm,
                        KomerohberAzonosito                = item.KomerohberAzonosito,
                        HianyzoKeszlet                     = item.HianyzoKeszlet,
                        Minositve                          = item.Minositve,
                        Minositette                        = item.Minositette,
                        OsszFelhasznalhatoKiveveAJelenlegi = (decimal)item.Mennyiseg,
                        IsEnabledItem                      = true,
                        IsModosithato                      = true
                    };
                    if( anyag.KeszletenGUI > 0 )
                        result.Add( anyag );
                } 
            }
            // Betöltjük a Session Anyagelszámolásokba is
            Storage.Instance.Session.AnyagElszamolasok = new List<RaktariAnyagokExtender>( result );

            return result;
        }

        /// <summary>
        /// Hozzáadja az elszámolandó anyagot a Munkautasitas ElszamoltAnyagok collectionjéhez
        /// </summary>
        /// <param name="item">Az elszámolandó anyag</param>
        private void AddElszamoltAnyagToMunka( RaktariAnyagokExtender item )
        {
            Munkautasitas.ElszamoltAnyagok.Add( new RaktariAnyag
            {
                Azonosito           = item.Azonosito,
                Cikkszam            = item.Cikkszam,
                Megnevezes          = item.Megnevezes,
                Mertekegys          = item.Mertekegys,
                KaraktarhAzonosito  = item.KaraktarhAzonosito,
                Mennyiseg           = (double)item.FelhasznaltMennyiseg,
                GyariSzam           = item.GyariSzam,
                ErtekelesFajta      = item.ErtekelesFajta,
                Sarzs               = item.Sarzs,
                MennyisegMm         = item.MennyisegMm,
                KomerohberAzonosito = item.KomerohberAzonosito,
                HianyzoKeszlet      = item.HianyzoKeszlet,
                Minositve           = item.Minositve,
                Minositette         = item.Minositette,
                IsTorolhetoKulsoleg = item.IsModosithato
            } );
        }

        /// <summary>
        /// Megnézi lehet-e fényképet csatolni a jegyzőkönyvhöz
        /// </summary>
        /// <returns>bool, ami azt jelöli lehet-e fényképet csatolni</returns>
        private bool GetIsFenykepCsatolhato( ref string hibaUzenet )
        {
            // Ha nincs sorszáma a jegyzőkönyvnek akkor nem lehet fényképet csatolni
            if( string.IsNullOrEmpty( Munkautasitas.Jegyzokonyv.Sorszam ) )
            {
                hibaUzenet = DisplayResources.JegyzokonyvSorszamnincsWarning;
                return false;
            }

            // Ha kitöltötte, de nem teljesen, akkor is szólunk neki, hogy még nem jó
            if ( Munkautasitas.Jegyzokonyv.Sorszam.Length < 7 )
            {
                hibaUzenet = DisplayResources.JegyzkFenykepCsatolasWarningJegyzkSorszamHelytelenVagyHianyos;
                return false;
            }

            // Ha kitöltötte, de nem helyesen, akkor is szólunk neki, hogy így nem jó
            if( !Munkautasitas.Jegyzokonyv.Sorszam.All( char.IsDigit ) )
            {
                hibaUzenet = DisplayResources.JegyzkFenykepCsatolasWarningJegyzkSorszamHelytelenVagyHianyos;
                return false;
            }

            // Csak a legelső fénykép csatolásnál figyelmeztetjük ha doboz nélkül csatol fényképet
            if ( !Munkautasitas.Jegyzokonyv.IsFenykep1
                    && !Munkautasitas.Jegyzokonyv.IsFenykep2
                    && !Munkautasitas.Jegyzokonyv.IsFenykep3
                    && !Munkautasitas.Jegyzokonyv.IsFenykep4 )
            {
                // Ha nincs doboz akkor kérdezzük meg akar-e így fényképet csatolni
                if ( Munkautasitas.Jegyzokonyv.Dobozok == null || Munkautasitas.Jegyzokonyv.Dobozok.Count < 1 )
                {
                    if ( MessageBoxWpf.Show( DisplayResources.JegyzkFenykepCsatolasDobozWarning, Properties.DisplayResources.Figyelmeztetes, MessageBoxButton.YesNo, MessageBoxImage.Warning ) == MessageBoxResult.No )
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            
            return true;
        }

        /// <summary>
        /// Beállítja az anyagelszámoláshoz szükséges paramétereket
        /// </summary>
        /// <param name="szulo">Vonalkóddal rendelkező szülő berendezés</param>
        private void SetParametersForAnyagelszamView( IVonalkodos szulo )
        {
            BerendMuveletekHelper.Vonalkodos                        = szulo;
            BerendMuveletekHelper.IsJegyzokonyvMatrica              = true;
            BerendMuveletekHelper.IsBerendezes                      = false;
            BerendMuveletekHelper.IsBerendezesCsere                 = false;
            BerendMuveletekHelper.IsKismegszOsszFelszereles         = false;
            BerendMuveletekHelper.IsKismegszRFelszereles            = false;
            BerendMuveletekHelper.IsKismegszSFelszereles            = false;
            BerendMuveletekHelper.IsKismegszTFelszereles            = false;
            BerendMuveletekHelper.IsMerokeszulek                    = false;
            Controls.KeszletSpinTextBoxHelper.AnyagAddThroughCommon = false;
            Controls.KeszletSpinTextBoxHelper.KismegszOsszAdding    = false;
            Controls.KeszletSpinTextBoxHelper.KismegszRSTAdding     = false;
        }

        /// <summary>
        /// kinullázza a vonalkód értéket, valamint a matricához kapcsolódó értékeket
        /// </summary>
        /// <param name="doboz">A nullázandó doboz objektum</param>
        private void ClearDobozVonalkodProperties( Doboz doboz )
        {
            doboz.Vonalkod = null;
            doboz.IsAutoVonalkodHozzaadasVisible = false;
            doboz.IsElszamoltRaktariAnyagFound = false;
            doboz.IsAutoVonalkodHozzaadasVisible = false;
            doboz.FoundRaktariAnyagTulajdonsagok = null;
        }

        /// <summary>
        /// Az utolsó fénykép csatolása után megjelenít egy üzenetet, hogy készült-e ellenőrzést megelőző tájékoztató lap.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Dokumentumok_CollectionChanged( object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e )
        {
            if( Munkautasitas.Jegyzokonyv == null )
                return;

            switch( e.Action )
            {
                case NotifyCollectionChangedAction.Add:
                    IsEllenorzestMegelozoTajekoztatoLapHandler();
                    break;
                case NotifyCollectionChangedAction.Remove:
                    return;
                case NotifyCollectionChangedAction.Reset:
                    return;
                default:
                    break;
            }
        }

        private void IsEllenorzestMegelozoTajekoztatoLapHandler()
        {
            // Ha mind a 4 fényképet csatolta, akkor küldünk egy üzenetet a JegyzokonyvPanelModel-nek, hogy dobja fel a "Van Ellenőrzést megelőző tájékoztató lap?" ablakot
            if( Munkautasitas.Jegyzokonyv.IsAllOldalFenykepCsatolva && Munkautasitas.Jegyzokonyv.TajekoztatoLap == null && !Munkautasitas.Jegyzokonyv.IsTajekoztatoLapKerdesNyugtazva )
            {
                // Elsötétítjük a többi contentet
                ViewModelLocator.MainStatic.DarkenUnfocusedArea = true;

                if( MessageBoxWpf.Show( DisplayResources.JegyzkTajLapKerdes, DisplayResources.Figyelmeztetes, button: MessageBoxButton.YesNo, icon: MessageBoxImage.Question ) == MessageBoxResult.Yes )
                {
                    Munkautasitas.Jegyzokonyv.IsTajekoztatoLap = true;
                    App.Logger.Info( "Jegyzőkönyves ellenőrzést megelőző tájékoztató lap készült." );
                }
                else
                {
                    Munkautasitas.Jegyzokonyv.IsTajekoztatoLap = false;
                    App.Logger.Info( "Jegyzőkönyves ellenőrzést megelőző tájékoztató lap nem készült." );
                }

                // A tájékoztató lap kérdés nyugtázva
                Munkautasitas.Jegyzokonyv.IsTajekoztatoLapKerdesNyugtazva = true;
            }

            // Visszaállítjuk az elsötétített részt
            ViewModelLocator.MainStatic.DarkenUnfocusedArea = false;
        }

        #endregion


        public bool Close()
        {
            Messenger.Default.Unregister<JegyzokonyvFenykepTorlesMessage>( this, JegyzokonyvFenykepTorlesHandler );
            IsuAzonAndVonalkodChangedEvent_Leiratkozas();
            Munkautasitas.Dokumentumok.CollectionChanged -= Dokumentumok_CollectionChanged;

            return true;
        }
    }
}
