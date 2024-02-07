using Geometria.MirtuszMobil.Client.Properties;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    /// <summary>
    /// Interaction logic for Anyagelszamolas.xaml
    /// </summary>
    public partial class Anyagelszamolas : UserControl, IPanel
    {
        public Anyagelszamolas()
        {
            InitializeComponent();
        }

        private void SingleListView_PreviewMouseLeftButtonDown( object sender, System.Windows.Input.MouseButtonEventArgs e )
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
        public bool IsLathato => true;

        public long Sorrend => 80;

        public string Fejlec => DisplayResources.AnyagElszamolas;

        public void Close()
        {
        }
        #endregion
    }
}
