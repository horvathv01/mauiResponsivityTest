using Geometria.MirtuszMobil.Client.Properties;
using System.Windows.Controls;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    /// <summary>
    /// Interaction logic for DiszpecserUzenetPanel.xaml
    /// </summary>
    public partial class DiszpecserUzenetPanel : UserControl, IPanel
    {
        public DiszpecserUzenetPanel()
        {
            InitializeComponent();
        }

        #region IPanel
        public bool IsLathato => true;

        public long Sorrend => 30;

        public string Fejlec => DisplayResources.DiszpecserUzenet;

        public void Close()
        {
        }
        #endregion
    }
}
