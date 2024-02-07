using Geometria.MirtuszMobil.Client.Properties;
using System.Windows.Controls;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{

    public partial class Vedofelszerelesek : UserControl, IPanel
    {
        public Vedofelszerelesek()
        {
            InitializeComponent();
        }

        #region IPanel
        public bool IsLathato => true;

        public long Sorrend => 10;

        public string Fejlec => DisplayResources.PanelVedofelszerelesek;

        public void Close()
        {
        }
        #endregion

    }
}
