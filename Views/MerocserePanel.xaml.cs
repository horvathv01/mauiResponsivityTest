using GalaSoft.MvvmLight.Messaging;
using Geometria.MirtuszMobil.Client.Messages;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Geometria.MirtuszMobil.Client.HelperClasses;
using Geometria.MirtuszService.MessageClasses;
using Geometria.MirtuszMobil.Client.Properties;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    /// <summary>
    /// Interaction logic for MerocserePanel.xaml
    /// </summary>
    public partial class MerocserePanel : UserControl, IPanel
    {
        public Munkautasitas Munkautasitas { get; set; }
        public MerocserePanel( Munkautasitas munka )
        {
            Munkautasitas = munka;
            DataContext = new MerocserePanelModel( munka );
            InitializeComponent();
            

            Messenger.Default.Register<BerendCollectionChanged>( this, BerendList_CollectionChanged );
            // Ha esetleg törlik a mérőhöz kapcsolódó dokumentumot, frissíteni kell a mérő kapcsolódó fényképek listát, ehhez fel kell iratkzozni erre az üzenetre
            Messenger.Default.Register<MeroFenykepTorlesMessage>( this, MeroFenykepekUpdateAfterDokuTorles );
            Messenger.Default.Register<SzinSemaValtozott>(this, SzinSemaValtozottMessage_Handler);


        }

        public void BerendList_CollectionChanged( BerendCollectionChanged parameter )
        {
            ListViewItem meroListViewitem = MerokeszulekList.ItemContainerGenerator.ContainerFromItem( parameter.SzuloBerendezes ) as ListViewItem;
            if( meroListViewitem == null )
                return;

            ExtensionMethods.ExpanderLenyit( meroListViewitem );

            var gyermekBerendezesekListView = ExtensionMethods.FrameWorkElementetKeres( meroListViewitem, "BerendList" ) as ListView;
            if( gyermekBerendezesekListView != null )
            {
                Berendezes ujBerendezes = parameter.UjBerendezes == null ? null : parameter.UjBerendezes;
                if ( ujBerendezes == null )
                {
                    if ( gyermekBerendezesekListView.Items.Count > 0)
                        ujBerendezes = gyermekBerendezesekListView.Items[gyermekBerendezesekListView.Items.Count - 1] as Berendezes;
                }

                gyermekBerendezesekListView.ScrollIntoView( parameter.UjBerendezes );

                ListViewItem szamlaloListViewitem = gyermekBerendezesekListView.ItemContainerGenerator.ContainerFromItem( ujBerendezes ) as ListViewItem;
                if( szamlaloListViewitem != null )
                {
                    ExtensionMethods.ExpanderLenyit( szamlaloListViewitem );

                    var animationStoryBoard = this.FindResource( "FlashNewListItem" ) as Storyboard;
                    Storyboard.SetTarget( animationStoryBoard, szamlaloListViewitem );
                    animationStoryBoard.Begin( szamlaloListViewitem, true );

                }
            }
        }

        private void MeroFenykepekUpdateAfterDokuTorles( MeroFenykepTorlesMessage meroAzon )
        {
            if( Munkautasitas.Merohely.Merokeszulekek != null && Munkautasitas.Merohely.Merokeszulekek.Count > 0 )
            {
                var meroToUpdate = Munkautasitas.Merohely.Merokeszulekek.Where( m => m.Azonosito == meroAzon.MerohBerAzonosito ).FirstOrDefault();
                MeroKapcsolodoFenykepek_CollectionChanged( meroToUpdate );
            }
        }

        private void MeroKapcsolodoFenykepek_CollectionChanged( Merokeszulek merokeszulek )
        {
            if( merokeszulek != null )
            {
                merokeszulek.KapcsolodoFenykepek = Munkautasitas.Dokumentumok != null ?
                                                   Munkautasitas.Dokumentumok.Where( f => f.MerohberAzonosito == merokeszulek.Azonosito ).ToObservableCollection() :
                                                   null;
            }
        }

        #region IPanel
        public bool IsLathato => true;

        public long Sorrend => 40;
        public string Fejlec => DisplayResources.PanelMerohely;

        public void Close()
        {
            Messenger.Default.Unregister<BerendCollectionChanged>( this, BerendList_CollectionChanged );
            Messenger.Default.Unregister<MeroFenykepTorlesMessage>( this, MeroFenykepekUpdateAfterDokuTorles );
            Messenger.Default.Unregister<SzinSemaValtozott>(this, SzinSemaValtozottMessage_Handler);

            if ( DataContext is MerocserePanelModel model)
            {
                model.Close();
            }
        }

        #endregion

        private void TextBox_PreviewTextInput( object sender, System.Windows.Input.TextCompositionEventArgs e )
        {
            e.Handled = !MeroBerendezesMuveletekHelper.IsTextAllowed(e.Text);
        }

        private void SzinSemaValtozottMessage_Handler(SzinSemaValtozott obj)
        {
            if( Munkautasitas.Merohely.Merokeszulekek == null )
                return;

            foreach( var mero in Munkautasitas.Merohely.Merokeszulekek)
            {
                mero.RaiseProperties();
                foreach( var keszulek in mero.Berendezesek )
                {
                    keszulek.RaiseProperties();
                }
            }
        }

        private void ComboBox_DropDownOpened( object sender, System.EventArgs e )
        {
            var cb = sender as ComboBox;
            if ( cb != null )
                cb.SelectedIndex = 0;
        }
    }
}
