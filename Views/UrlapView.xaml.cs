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
				button.BackgroundColor = Colors.Beige;
            }

			if(buttonPressed.BackgroundColor == Colors.Beige )
			{
				buttonPressed.BackgroundColor = Colors.LightGreen;
			} else
			{
                buttonPressed.BackgroundColor = Colors.Beige;
            }
		}
    }
}