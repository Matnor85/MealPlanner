namespace MealPlanner.UI;

// Vi skriver det fullständiga namnet till MAUI-klassen
public partial class App : Microsoft.Maui.Controls.Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage()) { Title = "MealPlanner.UI" };
    }
}