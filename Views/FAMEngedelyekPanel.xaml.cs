using Geometria.MirtuszMobil.Client.Properties;
using System.Windows.Controls;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    /// <summary>
    /// Interaction logic for FAMEngedelyekPanel.xaml
    /// </summary>
    public partial class FAMEngedelyekPanel : UserControl, IPanel
    {
        public FAMEngedelyekPanel( MirtuszService.MessageClasses.Munkautasitas munka )
        {
            InitializeComponent();
        }

        #region IPanel
        public bool IsLathato => true;

        public long Sorrend => 20;

        public string Fejlec => DisplayResources.PanelFAM;

        public void Close()
        {
        }
        #endregion

    }
}
