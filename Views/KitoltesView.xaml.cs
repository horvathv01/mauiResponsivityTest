

namespace mauiResponsivityTest.Views;

public partial class KitoltesView : ContentView
{
	public KitoltesView()
	{
		InitializeComponent();
	}

    private void Button_Clicked( object sender, EventArgs e )
    {
		var width = this.Window.Width;
		if(width < 600 )
		{
			ResponsiveGrid.SetColumn( ActualQuantity, 0 );
			ResponsiveGrid.SetRow( ActualQuantity, 1);
			ResponsiveGrid.SetColumn( ClientStatus, 0 );
            ResponsiveGrid.SetRow( ClientStatus, 3 );
			Column2.Width = 0;
        } 
		else
		{
            ResponsiveGrid.SetColumn( ActualQuantity, 1 );
            ResponsiveGrid.SetRow( ActualQuantity, 0 );
            ResponsiveGrid.SetColumn( ClientStatus, 1 );
            ResponsiveGrid.SetRow( ActualQuantity, 0 );
            Column2.Width = Column1.Width;
        }
    }
}