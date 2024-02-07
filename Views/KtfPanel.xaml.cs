using Geometria.MirtuszMobil.Client.Properties;
using System.Windows.Controls;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    /// <summary>
    /// Interaction logic for KtfPanel.xaml
    /// </summary>
    public partial class KtfPanel : UserControl, IPanel
    {
        public KtfPanel()
        {
            InitializeComponent();
        }

        #region IPanel
        public bool IsLathato => true;

        public long Sorrend => 20;

        public string Fejlec => DisplayResources.Ktf;

        public void Close()
        {
        }
        #endregion

    }
}
