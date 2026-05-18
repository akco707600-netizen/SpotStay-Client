using System.Windows;

namespace 코빚노_프로젝트
{
    public partial class LoginWindow : Window
    {
        public string LoginUserId { get; private set; }
        public bool RememberLogin { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnDoLogin_Click(object sender, RoutedEventArgs e)
        {
            string id = TxtId.Text.Trim();
            string pw = TxtPw.Password.Trim();

            if (id.Length == 0)
            {
                MessageBox.Show("아이디를 입력하세요.");
                TxtId.Focus();
                return;
            }

            if (pw.Length == 0)
            {
                MessageBox.Show("비밀번호를 입력하세요.");
                TxtPw.Focus();
                return;
            }

            LoginUserId = id;
            RememberLogin = ChkRememberLogin.IsChecked == true;

            DialogResult = true;
        }
    }
}