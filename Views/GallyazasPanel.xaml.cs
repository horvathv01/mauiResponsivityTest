using GalaSoft.MvvmLight.Messaging;
using Geometria.GeoMobil.Client.Manager;
using Geometria.MirtuszMobil.Client.Messages;
using Geometria.MirtuszMobil.Client.Properties;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Geometria.MirtuszMobil.Client.HelperClasses;
using Geometria.GeoMobil.Client.UI.Dialogs;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    public partial class GallyazasPanel : UserControl, IPanel
    {
        ListView SelectedList;
        
        public GallyazasPanel( MirtuszService.MessageClasses.Munkautasitas munka )
        {
            InitializeComponent();
            Loaded += Loaded_Handler;
        }

        private void Loaded_Handler(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Send<GallyazasPanelLoadedMessage>( new GallyazasPanelLoadedMessage());
            Center.Instance.VisszaAFoAblakraEvent += VisszaAFoAblakraEvent_Handler;
            Loaded -= Loaded_Handler;
        }

        private void VisszaAFoAblakraEvent_Handler(object sender, ObjectEventArgs e )
        {
            App.MainWindow.Activate();

            var polygonGrafika = e.Object as GeoMobil.Client.Terkep.HelperClasses.TerkepPolygonGrafika;

            if( polygonGrafika != null )
            {
                var objectAzon = polygonGrafika.Azonosito;

                Messenger.Default.Send<GallyazasViewOpenMessage>( new GallyazasViewOpenMessage( objectAzon ) );
            }
        }

        #region IPanel
        public bool IsLathato => true;

        public long Sorrend => 70;
        public string Fejlec => DisplayResources.PanelGallyazas;

        public void Close()
        {
            Center.Instance.VisszaAFoAblakraEvent -= VisszaAFoAblakraEvent_Handler;
        }
        #endregion

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)e.OriginalSource;

            BindingOperations.GetBindingExpression(comboBox, ComboBox.SelectedValueProperty)
                             .UpdateTarget();
        }

        private void GallyFeladatList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            var senderList = sender as System.Windows.Controls.ListView;
            if (senderList.SelectedItem != null)
                SelectedList = senderList;
        }
    }
}
