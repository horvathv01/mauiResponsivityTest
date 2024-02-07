using Geometria.MirtuszMobil.Client.Properties;
using System.Windows.Controls;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    /// <summary>
    /// Interaction logic for IdoPanel.xaml
    /// </summary>
    public partial class IdoPanel : UserControl, IPanel
    {
        public IdoPanel()
        {
            InitializeComponent();
        }

        #region IPanel
        public bool IsLathato => true;

        public long Sorrend => 20;

        public string Fejlec => DisplayResources.Hataridok;

        public void Close()
        {
        }
        #endregion

    }
}
