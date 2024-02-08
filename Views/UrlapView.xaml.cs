namespace mauiResponsivityTest.Views;

public partial class UrlapView : ContentView
{
	public UrlapView()
	{
		InitializeComponent();
	}

    private void Button_Clicked( object sender, EventArgs e )
    {
		if( sender is Button buttonPressed && buttonPressed.Parent is FlexLayout parent)
		{
			foreach( Button button in parent.Children )
			{
				button.BackgroundColor = Colors.WhiteSmoke;
            }

			if(buttonPressed.BackgroundColor == Colors.WhiteSmoke )
			{
				buttonPressed.BackgroundColor = Colors.LightGreen;
			} else
			{
                buttonPressed.BackgroundColor = Colors.WhiteSmoke;
            }
		}
    }
}