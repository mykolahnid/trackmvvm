using System.Windows;
using TrackMvvm.ViewModel;

namespace TrackMvvm.Views;

public partial class LoginDialog : Window
{
    public LoginViewModel ViewModel => (LoginViewModel)DataContext;

    public LoginDialog()
    {
        InitializeComponent();
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.Password = PasswordBox.Password;
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.DialogResult)
        {
            DialogResult = true;
            Close();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
