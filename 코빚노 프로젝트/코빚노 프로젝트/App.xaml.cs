using System.Windows;

namespace 코빚노_프로젝트
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();

            bool? result = loginWindow.ShowDialog();

            if (result == true)
            {
                MainWindow mainWindow = new MainWindow();

                mainWindow.SetLoginUser(loginWindow.LoginUserId);

                Application.Current.MainWindow = mainWindow;

                // 이제부터는 메인창 닫히면 앱 종료
                Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

                mainWindow.Show();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }
    }
}