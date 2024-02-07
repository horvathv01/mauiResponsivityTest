using Geometria.MirtuszMobil.Client.HelperClasses;
using Geometria.MirtuszMobil.Client.Properties;
using Geometria.MirtuszService.MessageClasses;
using System.Windows;
using System.Windows.Controls;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    /// <summary>
    /// Interaction logic for JegyzokonyvPanel.xaml
    /// </summary>
    public partial class JegyzokonyvPanel : UserControl, IPanel
    {
        public JegyzokonyvPanel( Munkautasitas munka )
        {
            DataContext = new JegyzokonyvPanelModel( munka );
            InitializeComponent();
        }

        #region IPanel

        public bool IsLathato => true;

        public long Sorrend => 10;

        public string Fejlec => DisplayResources.Jegyzokonyv;

        public void Close()
        {
            if( DataContext is JegyzokonyvPanelModel model )
            {
                model.Close();
            }
        }

        #endregion

        private void AddBizonyitekButton_Click( object sender, System.Windows.RoutedEventArgs e )
        {
            DependencyObject dep_IconButton = (DependencyObject)e.OriginalSource;

            if ( dep_IconButton != null )
            {
                Expander parentExpander = ExtensionMethods.TryFindParent<Expander>( dep_IconButton );

                if ( parentExpander != null )
                    parentExpander.IsExpanded = true;
            }
        }
    }
}
