using Geometria.MirtuszMobil.Client.Properties;
using System.Windows.Controls;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    /// <summary>
    /// Interaction logic for UrlapokPanel.xaml
    /// </summary>
    public partial class UrlapokPanel : UserControl, IPanel
    {
        public UrlapokPanel()
        {
            InitializeComponent();
        }

        #region IPanel
        public bool IsLathato => true;

        public long Sorrend => 5;

        public string Fejlec => DisplayResources.Urlap2;

        public void Close()
        {
        }
        #endregion
    }
}
