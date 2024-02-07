using Geometria.MirtuszMobil.Client.Properties;
using System.Windows.Controls;

namespace Geometria.MirtuszMobil.Client.AdatlapViews
{
    /// <summary>
    /// Interaction logic for HibahelyekPanel.xaml
    /// </summary>
    public partial class HibahelyekPanel : UserControl, IPanel
    {
        private MirtuszService.MessageClasses.Munkautasitas munkautasitas;

        public HibahelyekPanel( MirtuszService.MessageClasses.Munkautasitas munka )
        {
            munkautasitas = munka;
            InitializeComponent();
        }

        #region IPanel
        public bool IsLathato
        {
            get
            {
                return munkautasitas != null && munkautasitas.Hibahely != null;
            }
        }

        public long Sorrend => 40;

        public string Fejlec => DisplayResources.PanelHibahely;

        public void Close()
        {
        }
        #endregion


    }
}
