using System.Windows;

namespace HRMS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnExit(ExitEventArgs e)
        {
            holders.TokenHolder.Clear();
            holders.AuthHolder.Clear();
            base.OnExit(e);
        }
    }
}
