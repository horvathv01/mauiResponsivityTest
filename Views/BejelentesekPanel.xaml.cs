using Geometria.MirtuszMobil.Client.Properties;
using System.Windows.Controls;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    /// <summary>
    /// Interaction logic for BejelentesekPanel.xaml
    /// </summary>
    public partial class BejelentesekPanel : UserControl, IPanel
    {
        public BejelentesekPanel( MirtuszService.MessageClasses.Munkautasitas munka )
        {
            InitializeComponent();
        }

        #region IPanel
        public bool IsLathato => true;

        public long Sorrend => 30;

        public string Fejlec => DisplayResources.PanelBejelentesek;

        public void Close()
        {
        }
        #endregion
    }
}
