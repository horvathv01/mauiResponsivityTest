using Geometria.MirtuszMobil.Client.Properties;
using System.Windows.Controls;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    /// <summary>
    /// Interaction logic for SzereloiMegjegyzesPanel.xaml
    /// </summary>
    public partial class SzereloiMegjegyzesPanel : UserControl, IPanel
    {
        public SzereloiMegjegyzesPanel()
        {
            InitializeComponent();
        }

        #region IPanel
        public bool IsLathato => true;

        public long Sorrend => 90;

        public string Fejlec => DisplayResources.PanelSzereloiMegjegyzes;

        public void Close()
        {
        }
        #endregion
    }
}
