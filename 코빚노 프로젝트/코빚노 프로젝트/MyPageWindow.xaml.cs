using System.Windows;

namespace 코빚노_프로젝트
{
    public partial class MyPageWindow : Window
    {
        private string _userId;

        public MyPageWindow(string userId)
        {
            InitializeComponent();

            _userId = userId;

            if (string.IsNullOrWhiteSpace(_userId))
                _userId = "user";

            TxtUserName.Text = _userId;
            TxtUserId.Text = "@" + _userId;
            TxtAvatar.Text = _userId.Substring(0, 1);
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("로그아웃은 메인 화면에서 처리됩니다.");
            Close();
        }
    }
}