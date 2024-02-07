using Geometria.MirtuszMobil.Client.Properties;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    /// <summary>
    /// Interaction logic for FogyhelyPanel.xaml
    /// </summary>
    public partial class FogyhelyPanel : UserControl, IPanel
    {
        private MirtuszService.MessageClasses.Munkautasitas munkautasitas;
        public FogyhelyPanel( MirtuszService.MessageClasses.Munkautasitas munka )
        {
            munkautasitas = munka;
            DataContext = new FogyhelyPanelModel( munka );
            InitializeComponent();
        }

        private void SingleListView_MouseLeftButtonDown( object sender, System.Windows.Input.MouseButtonEventArgs e )
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            while( (dep != null) && !(dep is ListViewItem) )
            {
                dep = VisualTreeHelper.GetParent( dep );
            }

            if( dep == null )
                return;

            ListViewItem item = (ListViewItem)dep;

            if( item.IsSelected )

            {
                item.IsSelected = !item.IsSelected;
                e.Handled = true;
            }
        }

        #region IPanel
        public bool IsLathato
        {
            get
            {
                return munkautasitas != null && munkautasitas.FogyasztasiHelyAdatai != null;
            }
        }

        public long Sorrend => 30;
        public string Fejlec => DisplayResources.PanelFogyHely;

        public void Close()
        {
            if( DataContext is FogyhelyPanelModel model )
            {
                model.Close();
            }
        }
        #endregion

    }
}
