using Geometria.MirtuszMobil.Client.Properties;
using System.Windows.Controls;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    /// <summary>
    /// Interaction logic for KivProjektPanel.xaml
    /// </summary>
    public partial class KivProjektPanel : UserControl, IPanel
    {
        public KivProjektPanel( MirtuszService.MessageClasses.Munkautasitas munka )
        {
            DataContext = new KivProjektPanelModel( munka );
            InitializeComponent();
        }

        #region IPanel
        public bool IsLathato => true;

        public long Sorrend => 60;

        public string Fejlec => DisplayResources.KivitelezesiProjekt;

        public void Close()
        {
            if( DataContext is KivProjektPanelModel model )
            {
                model.Close();
            }
        }
        #endregion

    }
}
