using GalaSoft.MvvmLight.Messaging;
using Geometria.GeoMobil.Client.Manager;
using Geometria.MirtuszMobil.Client.Messages;
using Geometria.MirtuszMobil.Client.Properties;
using Geometria.MirtuszService.MessageClasses;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    /// <summary>
    /// Interaction logic for TrafoValtoPanel.xaml
    /// </summary>
    public partial class TrafoValtoPanel : UserControl, IPanel
    {
        public TrafoValtoPanel( Munkautasitas munkautasitas )
        {
            InitializeComponent();
            //Center.Instance.SelectedTrafoEsVonalNeve += SelectedTrafoEsVonalNeve_Handler;
            Loaded += TrafoValtoPanel_Loaded;
            Unloaded += TrafoValtoPanel_Unloaded;
        }

        private bool m_EventLoaded = false;

        private void TrafoValtoPanel_Loaded( object sender, RoutedEventArgs e )
        {
            if ( !m_EventLoaded )
            {
                Center.Instance.SelectedTrafoEsVonalNeve += SelectedTrafoEsVonalNeve_Handler;
                m_EventLoaded = true;
            }
        }

        private void TrafoValtoPanel_Unloaded( object sender, System.Windows.RoutedEventArgs e )
        {
            if( m_EventLoaded )
            { 
                Center.Instance.SelectedTrafoEsVonalNeve -= SelectedTrafoEsVonalNeve_Handler;
                m_EventLoaded = false;
            }
        }

        private void SelectedTrafoEsVonalNeve_Handler( object sender, TrafoEsVonalNeveEventArgs e )
        {
            Messenger.Default.Send( new TrafoKivalasztvaResult( e.TrafoNamingId, e.VonalNev, e.Aramkorok, e.TrafoAzonosito, e.TrafoMegnevezes ) );
        }

        #region IPanel
        public bool IsLathato => true;

        public long Sorrend => 10;

        public string Fejlec => DisplayResources.TrafoAdatok;

        public void Close()
        {
        }
        #endregion

        }
    }
