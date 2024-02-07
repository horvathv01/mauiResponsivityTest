using GalaSoft.MvvmLight.Messaging;
using Geometria.Common.GPS;
using Geometria.Common.KifejezesKiertekelo;
using Geometria.MirtuszMobil.Client.Controls;
using Geometria.MirtuszMobil.Client.HelperClasses;
using Geometria.MirtuszMobil.Client.Messages;
using Geometria.MirtuszMobil.Client.StatuszFSM;
using Geometria.MirtuszMobil.Client.Storages;
using Geometria.MirtuszMobil.Common.HelperClasses;
using Geometria.MirtuszService.MessageClasses;
using Geometria.MirtuszService.MessageClasses.CodeValues;
using Geometria.MirtuszService.MessageClasses.Tabla.Rekordok;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    /// <summary>
    /// Interaction logic for AltalanosUrlapPanel.xaml
    /// </summary>
    public partial class AltalanosUrlapPanel : UserControl
    {
        private KifejezesKiertekelo m_KifKiert = null;
        bool m_IsSzabalyrendszer = false;
        bool m_IsAttributumGeneralasFolyamatban = false;
        bool m_IsUrlapValtasFolyamatban = false;

        #region Konstruktor
        public AltalanosUrlapPanel( Urlap urlap)
        {
            IsEnabledUrlap = StatuszManager.Instance.IsMunkautasitasSzerkesztheto;
 
            Urlap = urlap;


            DC = new AltalanosUrlapHelperDC();

            InitializeComponent();

            // betöltődés utánmehet az init
            Loaded += UrlapView_Loaded;
            Unloaded += AltalanosUrlapView_Unloaded;

            KifejezesKiertekeloInit();

            Messenger.Default.Register<Messages.AltalanosUrlapClosed>( this, AltalanosUrlapClosedHandler );
            Messenger.Default.Register<Messages.UrlapAttributumChanged>( this, UrlapAttributumChangedHandler );
            Messenger.Default.Register<UrlapChanged>( this, UrlapChangedHandler );
            Messenger.Default.Register<AttributumStoredValueValtozott>( this, AttributumStoredValueValtozottHandler );
        }

        private void AttributumStoredValueValtozottHandler( AttributumStoredValueValtozott asvv )
        {
            KovetkezoAttributumFuggosegeinekBeallitasa( asvv.AktualisAttributum );
        }

        private void KovetkezoAttributumFuggosegeinekBeallitasa( UrlapAttributum aktualisAttributum )
        {
            var kovetkezoAttributum = NextAttributumItem( aktualisAttributum );
            var isFuggKovetkezoAttributum = IsFuggKovetkezoAttributum( aktualisAttributum, kovetkezoAttributum );

            if( isFuggKovetkezoAttributum )
            {
                var attributumFuggosegek = GetAttributumFuggosegek( aktualisAttributum, kovetkezoAttributum );
                bool IsKivalasztvaSzuloErtek = false;

                if( aktualisAttributum.ERTEK_LISTA == null )
                {
                    kovetkezoAttributum.ValaszthatoErtekek = null;
                }
                else
                {
                    ValaszthatoErtekekBeallitasaAzAttributumhoz( kovetkezoAttributum, attributumFuggosegek );
                    IsKivalasztvaSzuloErtek = true;
                }

                kovetkezoAttributum.ERTEK_LISTA = null;
                AttributePanels.Where( p => p.Attributum == kovetkezoAttributum ).FirstOrDefault().UpdatePanel( IsKivalasztvaSzuloErtek );
            }
        }

        private void UrlapChangedHandler( UrlapChanged uc )
        {
            m_IsUrlapValtasFolyamatban = true;

            pnl_Attributes.Items.Clear();

            Urlap = uc.Urlap;
            m_IsSzabalyrendszer = false;
            Init();

            m_IsUrlapValtasFolyamatban = false;
        }

        private void AltalanosUrlapClosedHandler( AltalanosUrlapClosed auc )
        {
            Messenger.Default.Unregister<Messages.UrlapAttributumChanged>( this, UrlapAttributumChangedHandler );
            Messenger.Default.Unregister<Messages.AltalanosUrlapClosed>( this, AltalanosUrlapClosedHandler );
            Messenger.Default.Unregister<UrlapChanged>( this, UrlapChangedHandler );
            Messenger.Default.Unregister<AttributumStoredValueValtozott>( this, AttributumStoredValueValtozottHandler );
        }

        private void UrlapAttributumChangedHandler( UrlapAttributumChanged uac )
        {
            SetEnabledSpecAttribute( uac );

            foreach( var item in AttributePanels )
            {
                item.UpdatePanel();
            }
        }

        private void SetEnabledSpecAttribute( UrlapAttributumChanged uac )
        {
            if( String.IsNullOrWhiteSpace( uac.RovidNev ) )
                return;

            if( uac.RovidNev == UrlapRovidNev.PlombaSzam1 ||
                 uac.RovidNev == UrlapRovidNev.PlombaSzam2 ||
                 uac.RovidNev == UrlapRovidNev.MeroOra1 ||
                 uac.RovidNev == UrlapRovidNev.MeroOra2 )
            {
                var panelItem = AttributePanels.Where( w => w.Attributum.ROVID_NEV == uac.RovidNev ).FirstOrDefault();

                if( panelItem != null && panelItem.Attributum.IsEmpty )
                    panelItem.IsEnabled = true;

                if( panelItem != null && !panelItem.Attributum.IsEmpty )
                    panelItem.IsEnabled = false;
            }
        }

        void AltalanosUrlapView_Unloaded( object sender, RoutedEventArgs e )
        {
            if( Urlap != null )
            {
                //Toroljuk az urlap aktivsagat jelzo flag-et.
                Urlap.Set_InActive();

                Save();
            }
        }

        void UrlapView_Loaded( object sender, System.Windows.RoutedEventArgs e )
        {
            Loaded -= UrlapView_Loaded;
            if( !System.ComponentModel.DesignerProperties.GetIsInDesignMode( this ) )
            {
                Init();
            }

        }

        private void KifejezesKiertekeloInit()
        {
            // kifejezés kiértékelő
            m_KifKiert = new KifejezesKiertekelo();
        }

        public void Init()
        {
            if( Urlap == null )
                return;

            //Beallitjuk az urap aktivsagat jelzo flag-et.
            Urlap.Set_Active();

            //Az urlap attributumainak lekerdezese
            //(A ket OrderBy sorrendje igy jo - bar nem foltetlen ezt varna az ember...! [a Dock=Top miatt])
            Attributumok = Urlap.Attributumok.OrderBy( A => A.MEGNEVEZES )
                                                .OrderBy( A => A.MEGJELENITESI_SORREND ).AsEnumerable();
            //Az urlap sajat attributumait megjelenito controlok folpakolasa
            Create_AttributePanels();

            SetControlsEnability();
        }
        #endregion

        #region Properties

        /// <summary>
        /// Adatbázis elérés
        /// </summary>
        public AltalanosUrlapHelperDC DC { get; set; }


        /// <summary>
        /// Az űrlapon szereplő attribútumok.
        /// </summary>
        internal IEnumerable<UrlapAttributum> Attributumok;


        /// <summary>
        /// Az űrlap attribútumait a form-on megjelenítő AttributePanel control-ok listája.
        /// </summary>
        internal List<AltalanosAttributePanelBase> AttributePanels=new List<AltalanosAttributePanelBase>();

        /// <summary>
        /// A form megnyitója számára jelzi, 
        /// hogy a form bezárását követően automatikusan meg lehet nyitni a következő dialógust.
        /// <para>Ha ez true, akkor a form bezáródásakor meghívódik az OpenNextDialog virtual metódus.</para>
        /// </summary>
        protected Boolean OpenNextDialog_WhenClosed;

        #endregion

        #region Metódusok

        /// <summary>
        /// Az attributumokat megjelenítő AttributePanel-ek létrehozása.
        /// </summary>
        private void Create_AttributePanels()
        {
            var urlapAttrFuggosegek = GetAllItemUrlapAttributumFuggosegekTable();

            foreach( var A in Attributumok )
            {
                if( urlapAttrFuggosegek.Any( x => x.UrlapSablonAzon == A.Sablon.URLAP_SABLON_AZONOSITO ) )
                {
                    Urlap.IsSzabalyrendszer = true;
                    m_IsSzabalyrendszer = true;
                    break;
                }
            }

            if( m_IsSzabalyrendszer )
            {
                foreach( var AttrItem in Attributumok )
                {
                    if( AttrItem.KiToltotteKi != UrlapAttributum.KitoltesTipusa.Mobil )
                    {
                        PanelAdd( AttrItem );
                    }
                    else
                        continue;
                }

            }
            else
            {
                foreach( var A in Attributumok )
                {
                    AttributumHozzaadasaAPanelhez( A );
                }
                undoButton.Visibility = Visibility.Collapsed;
            }

            foreach( var A in Attributumok )
            {
                KovetkezoAttributumFuggosegeinekBeallitasa( A );
            }
        }

        private List<UrlapAttributumFuggosegekRecord> GetAllItemUrlapAttributumFuggosegekTable()
        {
            return StoredTableHelper.Instance.GetRecord<UrlapAttributumFuggosegekRecord>().ToList();
        }

        private static void BeallitottAttributumErtekekTorlese( UrlapAttributum AttrItem )
        {
            AttrItem.KiToltotteKi = null;
            AttrItem.ERTEK_DATUM = null;
            AttrItem.ERTEK_LISTA = null;
            AttrItem.ERTEK_NUM = null;
            AttrItem.ERTEK_SZOVEG = "";
        }

        private void PanelAdd( UrlapAttributum attributum )
        {
            var panel = AltalanosAttributePanelBase.Get_Panel( attributum, m_IsSzabalyrendszer );

            AttributePanels.Add( panel );
            pnl_Attributes.Items.Add( panel );

            panel.ValueStored += AttributumPanelValueStored;

            pnl_Attributes.UpdateLayout();

            //if( !m_IsUrlapValtasFolyamatban )
            //    pnl_Attributes.ScrollIntoView( panel );
        }

        void AttributumPanelValueStored( object sender, AltalanosAttributePanelBase.ValueStoredEventArgs e )
        {
            App.Logger.Trace( "Attribútum hozzáadása esemény jött." );
            if( !m_IsAttributumGeneralasFolyamatban )
            {
                m_IsAttributumGeneralasFolyamatban = true;
                App.Logger.Trace( "Attribútum hozzáadása folyamatban true." );

                if( m_IsSzabalyrendszer )
                    AddItemToPanel( e );

                m_IsAttributumGeneralasFolyamatban = false;
                App.Logger.Trace( "Attribútum hozzáadása folyamatban false." );
            }
        }

        private void AddItemToPanel( AltalanosAttributePanelList.ValueStoredEventArgs e )
        {
            if( e.Attributum == null )
                return;
            try
            {
                Cursor = Cursors.Wait;
                var urlapAttrFuggosegek = GetAllItemUrlapAttributumFuggosegekTable();

                long urlapSablonAttributumAzonosito = e.Attributum.AZONOSITO;

                var aktualisAttributum = Attributumok.Where( x => x.AZONOSITO == urlapSablonAttributumAzonosito ).FirstOrDefault();

                bool isModosithatoAzAttributum;
                UrlapAttributum kovetkezoAttributum;

                var tempAktualisAttributum = aktualisAttributum;
                do
                {
                    kovetkezoAttributum = NextAttributumItem( tempAktualisAttributum );//KovetkezoMegjelenithetoAttributum( tempAktualisAttributum );

                    isModosithatoAzAttributum = IsFuggKovetkezoAttributum( aktualisAttributum, kovetkezoAttributum );

                    if( !isModosithatoAzAttributum )
                    {
                        var attributumFuggosegek = GetAttributumFuggosegek( aktualisAttributum, kovetkezoAttributum );
                        ValaszthatoErtekekBeallitasaAzAttributumhoz( kovetkezoAttributum, attributumFuggosegek );
                        NemModosithatoAttributumErtekBeallitasa( kovetkezoAttributum, attributumFuggosegek.FuggoErtek );

                        tempAktualisAttributum = kovetkezoAttributum;
                    }

                } while( !isModosithatoAzAttributum );

                if( kovetkezoAttributum == null )
                {
                    AttributumSzerkeszthetosegenekLetiltasa( aktualisAttributum );
                    return;
                }

                if( kovetkezoAttributum != null )
                    AttributumMegjelenitese( aktualisAttributum, kovetkezoAttributum, GetAttributumFuggosegek( aktualisAttributum, kovetkezoAttributum ) );
            }
            finally
            {
                Cursor = null;
                Save();
            }

        }

        private UrlapAttributum NextAttributumItem( UrlapAttributum ua )
        {
            int currentItemIndex = Attributumok.ToList().IndexOf( ua );
            if( currentItemIndex < Attributumok.Count() - 1 )
                return Attributumok.ElementAt( ++currentItemIndex );
            else
                return null;
        }

        private UrlapAttributum PrevAttributumItem( UrlapAttributum ua )
        {
            int currentItemIndex = Attributumok.ToList().IndexOf( ua );
            if( currentItemIndex > 0 )
                return Attributumok.ElementAt( --currentItemIndex );
            else
                return null;
        }

        private bool IsFuggKovetkezoAttributum( UrlapAttributum aktualisAttributum, UrlapAttributum kovetkezoAttributum )
        {
            var urlapAttrFuggosegek = GetAllItemUrlapAttributumFuggosegekTable();

            if( kovetkezoAttributum == null )
                return false;

            var aktualisAttributumValaszthatoElemek = UrlapLocalDataContext.Instance.GetKodListaItems( aktualisAttributum.LISTA_TIPUS.Value ).Select( e => e.Value ).ToList(); 

            var IsFugg = urlapAttrFuggosegek.Where( x => x.FuggoAttributumAzonosito == kovetkezoAttributum.ATTRIBUTUM_SABLON_AZONOSITO
                                                               && x.UrlapSablonAzon == kovetkezoAttributum.Sablon.URLAP_SABLON_AZONOSITO
                                                               && aktualisAttributumValaszthatoElemek.Contains( x.Azonosito ) )
                                                          .Any();

            return IsFugg;

        }

        private void ValaszthatoErtekekBeallitasaAzAttributumhoz( UrlapAttributum kovetkezoAttributum, UrlapAttributumFuggosegekRecord urlapAttrFuggosegItem )
        {
            if( urlapAttrFuggosegItem == null )
                return;

            switch( kovetkezoAttributum.Tipus )
            {
                case UrlapAttributum.TipusEnum.Label:
                    break;
                case UrlapAttributum.TipusEnum.Text:
                    break;
                case UrlapAttributum.TipusEnum.Number:

                    if( urlapAttrFuggosegItem.FuggoLehetsegesErtekek == null )
                        return;

                    string[] korlatokraSzabdaltString = StringFeldarabolasaPontosVesszoknel( urlapAttrFuggosegItem.FuggoLehetsegesErtekek );

                    foreach( var korlatokraSzabdaltStringItem in korlatokraSzabdaltString )
                    {

                        if( Regex.IsMatch( korlatokraSzabdaltStringItem, ">=" ) )
                        {
                            string korlat = FeleslegesKarakterekEltavolitasa( korlatokraSzabdaltStringItem, ">=" );
                            try
                            {
                                kovetkezoAttributum.FeltetelbolSzarmazoAlsoHatar = Decimal.Parse( korlat );
                            }
                            catch( Exception )
                            {
                                var fuggoAttributumErtek = Attributumok.Where( x => x.ROVID_NEV.Equals( korlat ) ).FirstOrDefault();
                                if( fuggoAttributumErtek != null )
                                {
                                    kovetkezoAttributum.FeltetelbolSzarmazoAlsoHatar = fuggoAttributumErtek.ERTEK_NUM;
                                }
                                else
                                {
                                    App.Logger.Error( "Hiba a korlát meghatározásakor!" );
                                }
                            }
                            continue;
                        }
                        if( Regex.IsMatch( korlatokraSzabdaltStringItem, "<=" ) )
                        {
                            string korlat = FeleslegesKarakterekEltavolitasa( korlatokraSzabdaltStringItem, "<=" );
                            try
                            {
                                kovetkezoAttributum.FeltetelbolSzarmazoFelsoHatar = Decimal.Parse( korlat );
                            }
                            catch( Exception )
                            {
                                var fuggoAttributumErtek = Attributumok.Where( x => x.ROVID_NEV.Equals( korlat ) ).FirstOrDefault();
                                if( fuggoAttributumErtek != null )
                                {
                                    kovetkezoAttributum.FeltetelbolSzarmazoFelsoHatar = fuggoAttributumErtek.ERTEK_NUM;
                                }
                                else
                                {
                                    App.Logger.Error( "Hiba a korlát meghatározásakor!" );
                                }
                            }
                            continue;
                        }
                        if( Regex.IsMatch( korlatokraSzabdaltStringItem, ">" ) )
                        {
                            string korlat = FeleslegesKarakterekEltavolitasa( korlatokraSzabdaltStringItem, ">" );
                            try
                            {
                                kovetkezoAttributum.FeltetelbolSzarmazoAlsoHatar = Decimal.Parse( korlat );
                            }
                            catch( Exception )
                            {
                                var fuggoAttributumErtek = Attributumok.Where( x => x.ROVID_NEV.Equals( korlat ) ).FirstOrDefault();
                                if( fuggoAttributumErtek != null )
                                {
                                    kovetkezoAttributum.FeltetelbolSzarmazoAlsoHatar = fuggoAttributumErtek.ERTEK_NUM;
                                }
                                else
                                {
                                    App.Logger.Error( "Hiba a korlát meghatározásakor!" );
                                }
                            }
                            continue;
                        }
                        if( Regex.IsMatch( korlatokraSzabdaltStringItem, "<" ) )
                        {
                            string korlat = FeleslegesKarakterekEltavolitasa( korlatokraSzabdaltStringItem, "<" );
                            try
                            {
                                kovetkezoAttributum.FeltetelbolSzarmazoFelsoHatar = Decimal.Parse( korlat );
                            }
                            catch( Exception )
                            {
                                var fuggoAttributumErtek = Attributumok.Where( x => x.ROVID_NEV.Equals( korlat ) ).FirstOrDefault();
                                if( fuggoAttributumErtek != null )
                                {
                                    kovetkezoAttributum.FeltetelbolSzarmazoFelsoHatar = fuggoAttributumErtek.ERTEK_NUM;
                                }
                                else
                                {
                                    App.Logger.Error( "Hiba a korlát meghatározásakor!" );
                                }
                            }
                            continue;
                        }

                    }

                    break;
                case UrlapAttributum.TipusEnum.Date:
                    break;
                case UrlapAttributum.TipusEnum.List:
                    if( urlapAttrFuggosegItem.FuggoLehetsegesErtekek != null )
                    {
                        kovetkezoAttributum.ValaszthatoErtekek = StringFeldolgozasaIntegerre( urlapAttrFuggosegItem.FuggoLehetsegesErtekek );
                    }
                    else
                    {
                        kovetkezoAttributum.ValaszthatoErtekek = null;
                    }

                    break;
                default:
                    break;
            }

        }

        private void NemModosithatoAttributumErtekBeallitasa( UrlapAttributum attributum, string attributumFuggoErtek )
        {
            attributum.KiToltotteKi = UrlapAttributum.KitoltesTipusa.Mobil;
            FuggoErtekekBeallitasaAzAttributumhoz( attributum, attributumFuggoErtek );

            if( attributum != null )
            {
                var panel = AltalanosAttributePanelBase.Get_Panel( attributum, m_IsSzabalyrendszer );
                if( !AttributePanels.Contains(panel) )
                {
                    AttributumHozzaadasaAPanelhez( attributum );
                }
                AttributumSzerkeszthetosegenekLetiltasa( attributum );
            }
        }

        private void AttributumSzerkeszthetosegenekLetiltasa( UrlapAttributum aktualisAttributum )
        {
            if( AttributePanels.Count() != 0 )
            {
                AttributePanels.Where( x => x.Attributum.AZONOSITO == aktualisAttributum.AZONOSITO ).FirstOrDefault().IsEnabled = false;
            }
        }

        private void AttributumMegjelenitese( UrlapAttributum aktualisAttributum, UrlapAttributum kovetkezoAttributum, UrlapAttributumFuggosegekRecord attributumFuggosegek )
        {
            ValaszthatoErtekekBeallitasaAzAttributumhoz( kovetkezoAttributum, attributumFuggosegek );
            AttributumSzerkeszthetosegenekLetiltasa( aktualisAttributum );
            AttributumHozzaadasaAPanelhez( kovetkezoAttributum );
        }

        private List<decimal> StringFeldolgozasaIntegerre( string s )
        {
            string[] darabolt = s.Split( new char[] { ';' } );
            List<decimal> result = new List<decimal>();

            foreach( var item in darabolt )
            {
                result.Add( Int64.Parse( item ) );
            }

            return result;
        }

        private string FeleslegesKarakterekEltavolitasa( string korlatokraSzabdaltStringItem, string eltavolitandoString )
        {
            string temp = korlatokraSzabdaltStringItem;
            temp = temp.Replace( eltavolitandoString, "" );
            temp = temp.Replace( " ", "" );
            return temp;
        }

        private void FuggoErtekekBeallitasaAzAttributumhoz( UrlapAttributum attributum, string fuggoErtek )
        {
            switch( attributum.Tipus )
            {
                case UrlapAttributum.TipusEnum.Label:
                    return;

                case UrlapAttributum.TipusEnum.Text:
                    try
                    {
                        attributum.ERTEK_SZOVEG = fuggoErtek;
                        return;
                    }
                    catch( Exception )
                    {
                        App.Logger.Error( "Nem sikerült a konvertálás!" );
                        return;
                    }

                case UrlapAttributum.TipusEnum.Number:
                    try
                    {
                        decimal konvertaltSzam = System.Convert.ToDecimal( fuggoErtek );
                        attributum.ERTEK_NUM = konvertaltSzam;
                        return;
                    }
                    catch( Exception )
                    {
                        App.Logger.Error( "Nem sikerült a konvertálás!" );
                        return;
                    }

                case UrlapAttributum.TipusEnum.Date:
                    try
                    {
                        DateTime konvertaltDatum = System.Convert.ToDateTime( fuggoErtek );
                        attributum.ERTEK_DATUM = konvertaltDatum;
                        return;
                    }
                    catch( Exception )
                    {
                        App.Logger.Error( "Nem sikerült a konvertálás!" );
                        return;
                    }

                case UrlapAttributum.TipusEnum.List:
                    try
                    {
                        var kodlistaItem = UrlapLocalDataContext.Instance.GetKodListaItems( attributum.ATTRIBUTUM_SABLON_AZONOSITO );
                        decimal konvertaltSorszam = System.Convert.ToDecimal( fuggoErtek );
                        var beallitandoElemAzonositoja = kodlistaItem.Where( i => i.SerialNumber == konvertaltSorszam ).Select( i => i.Value ).FirstOrDefault();
                        attributum.ERTEK_LISTA = beallitandoElemAzonositoja;
                        return;
                    }
                    catch( Exception )
                    {
                        // Megpróbáljuk értelmezni, mert lehet nem azonositó, hanem szöveg van benne
                        var kodlistaItem = UrlapLocalDataContext.Instance.GetKodListaItems( attributum.ATTRIBUTUM_SABLON_AZONOSITO );
                        foreach( var item in kodlistaItem )
                        {
                            if( item.DisplayValue.Equals( fuggoErtek ) )
                            {
                                attributum.ERTEK_LISTA = item.Value;
                                return;
                            }
                        }

                        App.Logger.Error( String.Format( "Nem sikerült a konvertálás! ({0})", fuggoErtek ) );
                        return;
                    }

                default:
                    return;
            }

        }

        private void AttributumHozzaadasaAPanelhez( UrlapAttributum attributum )
        {
            PanelAdd( attributum );

            UndoButtonEnabledChecking();
        }

        //private Urlap_Attributum KovetkezoMegjelenithetoAttributum( Urlap_Attributum aktualisAttributum )
        //{
        //    Urlap_Attributum kovetkezoAttributum = NextAttributumItem( aktualisAttributum );

        //    bool isKovetkezoAttributumMegjelenitheto = IsMegjelenithetoAzAttributum( kovetkezoAttributum );

        //    if ( !isKovetkezoAttributumMegjelenitheto )
        //    {
        //        do
        //        {
        //            kovetkezoAttributum = NextAttributumItem( kovetkezoAttributum );

        //            //ha nincs több elem megállunk
        //            if ( kovetkezoAttributum == null )
        //                break;

        //        } while ( !IsMegjelenithetoAzAttributum( kovetkezoAttributum ) );
        //    }

        //    return kovetkezoAttributum;
        //}


        //private bool IsMegjelenithetoAzAttributum( Urlap_Attributum kovetkezoAttributum )
        //{            
        //    var urlapAttrFuggosegek = GetAllItemUrlapAttributumFuggosegekTable();

        //    if ( kovetkezoAttributum == null )
        //        return true;

        //    // lekérdezzük az UrlapAttributumFuggusegek táblából az attributumhoz tartozó sort
        //    var urlapAttributumFeltetel = urlapAttrFuggosegek.Where( 
        //            x => x.FuggoAttributumAzonosito == kovetkezoAttributum.ATTRIBUTUM_SABLON_AZONOSITO && 
        //                 x.UrlapSablonAzon == kovetkezoAttributum.Sablon.URLAP_SABLON_AZONOSITO ).OrderBy( x => x.Azonosito ).ToList();

        //    // nincs függőség, megjeleníthető az attributum
        //    if ( urlapAttributumFeltetel == null || urlapAttributumFeltetel.Count == 0 )
        //        return true;

        //    foreach ( var urlapAttributumFeltetelItem in urlapAttributumFeltetel )
        //    {
        //        string kiertekelonekOsszeallitottString = urlapAttributumFeltetelItem.Feltetelek;

        //        // a replace miatt fordított sorrend kell, néha üres string is kerülhet bele, ezért kell a szűrés
        //        string[] numbers = Regex.Split( urlapAttributumFeltetelItem.Feltetelek, @"\D+" ).Where(x => !String.IsNullOrEmpty(x)).OrderByDescending( x => x ).ToArray();                

        //        foreach ( var item in numbers )
        //        {                    
        //            bool kicserelendoFeltetel = IsAttributumFeltetelCsoportIgaz( UInt32.Parse( item ) );

        //            kiertekelonekOsszeallitottString = kiertekelonekOsszeallitottString.Replace( item, " " + kicserelendoFeltetel + " " );
        //        }

        //        kifKiert.Kifejezes = kiertekelonekOsszeallitottString;

        //        if ( kifKiert.Kiertekeles().LogikaiErtek )
        //        {                    
        //            return true;
        //        }
        //    }


        //    // Mindig megjelenik az attributum, ha a feltelelek nem teljesulnek akkor is, 
        //    // csak akkor a lehetseges ertekek az osszes valasztasi lehetoseget megkapjak
        //    kovetkezoAttributum.ValaszthatoErtekek = null;
        //    return true;

        //}

        private UrlapAttributumFuggosegekRecord GetAttributumFuggosegek( UrlapAttributum aktualisAttributum, UrlapAttributum kovetkezoAttributum )
        {
            var urlapAttrFuggosegek = GetAllItemUrlapAttributumFuggosegekTable();

            if( kovetkezoAttributum == null )
                return null;
            // lekérdezzük az UrlapAttributumFuggusegek táblából az attributumhoz tartozó sort
            var AttributumFuggosegek = urlapAttrFuggosegek.Where(    x => x.FuggoAttributumAzonosito == kovetkezoAttributum.ATTRIBUTUM_SABLON_AZONOSITO 
                                                                  && x.UrlapSablonAzon == kovetkezoAttributum.Sablon.URLAP_SABLON_AZONOSITO
                                                                  && x.Azonosito == aktualisAttributum.ERTEK_LISTA )
                                                          .FirstOrDefault();

            // nincs függőség, nincs benne a táblában
            if( AttributumFuggosegek == null )
                return null;

            return AttributumFuggosegek;
        }

        private string[] StringFeldarabolasaPontosVesszoknel( string s )
        {
            return s.Split( new char[] { ';' } );
        }

        private void SetControlsEnability()
        {
            Urlap.IsEnabledUrlap = IsEnabledUrlap;
            bool isEnabledUrlap = Urlap.Statusz_Nyitott();

            if( !m_IsSzabalyrendszer && isEnabledUrlap )
            {
                foreach( var item in AttributePanels )
                {
                    item.IsEnabled = true;
                }
            }

            if( !isEnabledUrlap )
                AttributePanels.ForEach( AP => AP.IsEnabled = false );

            if( AttributePanels.Count > 1 && isEnabledUrlap )
                undoButton.IsEnabled = true;
            else
                undoButton.IsEnabled = false;

        }

        /// <summary>
        /// Az Attribútumok kitöltöttsége alapján megállapítja, hogy milyen státuszba kéne tenni az űrlapot.
        /// </summary>
        private Urlap.StatuszEnum Statusz_By_Attributes
        {
            get
            {
                //A kitoltott attributumok szamanak meghatarozasa
                if( NumOfFilledAttributes == 0 )
                    return Urlap.StatuszEnum.Ures;
                else
                    return Urlap.StatuszEnum.Reszben_Kitoltott;
            }
        }

        /// <summary>
        /// A kitöltött attribútumok száma.
        /// <para>A kitöltöttség eldöntése az AttributePanel-ek IsFilled tulajdonságára épül,
        /// így független attól, hogy az adatok vissza lettek-e már írva az Attributum példányokba.</para>
        /// </summary>
        private int NumOfFilledAttributes
        {
            get
            {
                return AttributePanels.Count( AP => AP.Attributum.Felmerendo && AP.IsFilled );
            }
        }

        /// <summary>
        /// Az adatok mentése
        /// </summary>
        public void Save()
        {
                //Az ertekek visszairasa az Attributum peldanyokba
            AttributePanels.ForEach( AP => AP.StoreValue() );


            //Az urlap statuszanak beallitasa az attributumok kitoltottsegenek megfeleloen
            if( Urlap.Statusz_Nyitott() )
            {
                Urlap.Statusz = Statusz_By_Attributes;
            }

            SetUrlapKitoltesiAdatok();

            DC.Update( Urlap );

            foreach( var item in Attributumok )
            {
                Storage.Instance.Session.ModifyFeltoltendoReszlegesAdatok( item );
            }

            Storage.Instance.Session.StoreMunkautasitasok();
        }

        private void SetUrlapKitoltesiAdatok()
        {
            double? x;
            double? y;
            string precision;

            GPSHelper.TheGPS.Store_To_Fields( out x, out y, out precision );

            Urlap.Set_KitoltesiAdatok( x, y, precision, Storage.Instance.Session.Dolgozok.First().Value.DolgozoId );
        }

        #endregion

        #region IsEnabledUrlap

        public Urlap Urlap { get; set; }

        public const string IsEnabledUrlapPropertyName = "IsEnabledUrlap";

        public bool IsEnabledUrlap
        {
            get
            {
                return (bool)GetValue( IsEnabledUrlapProperty );
            }
            set
            {
                SetValue( IsEnabledUrlapProperty, value );
                if( Urlap != null )
                {
                    Urlap.IsEnabledUrlap = value;
                    SetControlsEnability();
                }
            }
        }

        public static readonly DependencyProperty IsEnabledUrlapProperty = DependencyProperty.Register(
            IsEnabledUrlapPropertyName,
            typeof( bool ),
            typeof( AltalanosUrlapPanel ),
            new UIPropertyMetadata( ( S, E ) =>
            {
                var TBT = S as AltalanosUrlapPanel;
                TBT.IsEnabledUrlap = (bool)E.NewValue;
            } ) );

        #endregion

        #region UndoButton

        private void UndoButtonEnabledChecking()
        {
            if( AttributePanels.Count() > 1 )
                undoButton.IsEnabled = true;
            else
                undoButton.IsEnabled = false;
        }

        private void undoButton_Click( object sender, RoutedEventArgs e )
        {
            if( AttributePanels.Count() <= 1 )
                return;

            // Utolsó elemnél az undo csak engedelyézi az elemet
            if( IsMindenAttributumKiVanToltve() )
            {

                var modositandoAttributumPanelItem = AttributePanels.Last();
                modositandoAttributumPanelItem.IsEnabled = true;
                modositandoAttributumPanelItem.Attributum.ValaszthatoErtekek = null;

                AttributumResetUntilCurrentAttributum( modositandoAttributumPanelItem.Attributum );

                UndoButtonEnabledChecking();
                return;
            }

            UrlapAttributum torlendoAttributum;
            do
            {
                var torlendoAttributumPanelItem = AttributePanels.Last();
                PanelRemove( torlendoAttributumPanelItem );

                torlendoAttributum = Attributumok.Where( x => x.AZONOSITO == torlendoAttributumPanelItem.Attributum.AZONOSITO ).First();
                torlendoAttributum.ValaszthatoErtekek = null;

                AttributumResetUntilCurrentAttributum( torlendoAttributum );

                AttributePanels.Remove( torlendoAttributumPanelItem );
                pnl_Attributes.Items.Remove( torlendoAttributumPanelItem );


            } while( AttributePanels.Last().Attributum.KiToltotteKi == UrlapAttributum.KitoltesTipusa.Mobil );

            var prevAttrPanel = AttributePanels.Last();

            AttributumResetUntilCurrentAttributum( prevAttrPanel.Attributum );

            prevAttrPanel.IsEnabled = true;
            prevAttrPanel.ClearSelectedValue();

            UndoButtonEnabledChecking();

            Save();

        }

        private bool IsMindenAttributumKiVanToltve()
        {
            return Attributumok.Where( x => x.IsEmpty ).Count() == 0;
        }

        private void AttributumResetUntilCurrentAttributum( UrlapAttributum attributum )
        {
            // Az attributumok lista végétől a paraméterben kapottig minden attributum kiválasztott értékét töröljük
            foreach( var attributumItem in Attributumok.Reverse() )
            {
                attributumItem.ERTEK_DATUM = null;
                attributumItem.ERTEK_LISTA = null;
                attributumItem.ERTEK_NUM = null;
                attributumItem.ERTEK_SZOVEG = null;
                attributumItem.KiToltotteKi = null;

                if( attributumItem.AZONOSITO == attributum.AZONOSITO )
                    return;
            }
        }

        private void PanelRemove( AltalanosAttributePanelBase torlendoAttributumPanelItem )
        {
            System.Diagnostics.Trace.WriteLine( string.Format( "PanelRemove: {0} - {1}", torlendoAttributumPanelItem.Attributum.AZONOSITO, torlendoAttributumPanelItem.Attributum.MEGNEVEZES ) );
            torlendoAttributumPanelItem.ValueStored -= AttributumPanelValueStored;
        }

        #endregion

    }
}
