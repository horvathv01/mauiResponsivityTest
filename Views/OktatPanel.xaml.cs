using Geometria.MirtuszMobil.Client.Properties;
using Geometria.MirtuszService.MessageClasses;
using System.Windows.Controls;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    /// <summary>
    /// Interaction logic for OktatPanel.xaml
    /// </summary>
    public partial class OktatPanel : UserControl, IPanel
    {
        public OktatPanel( Munkautasitas munka )
        {
            InitializeComponent();
        }

        #region IPanel
        public bool IsLathato => true;

        public long Sorrend => 40;
        public string Fejlec => DisplayResources.PanelOktat;

        public void Close()
        {
        }
        #endregion
    }
}
