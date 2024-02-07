using Geometria.MirtuszMobil.Client.Properties;
using System.Windows.Controls;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    /// <summary>
    /// Interaction logic for FeladatPanel.xaml
    /// </summary>
    public partial class FeladatPanel : UserControl, IPanel
    {
        public FeladatPanel()
        {
            InitializeComponent();
        }

        #region IPanel
        public bool IsLathato => true;

        public long Sorrend => 10;

        public string Fejlec => DisplayResources.MunkaLeiras;

        public void Close()
        {
        }
        #endregion

    }
}
