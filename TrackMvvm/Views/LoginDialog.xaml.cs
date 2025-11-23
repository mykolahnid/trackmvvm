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
        // Validate
        if (string.IsNullOrWhiteSpace(ViewModel.Email))
        {
            ViewModel.ErrorMessage = "Please enter your email";
            return;
        }

        if (string.IsNullOrWhiteSpace(ViewModel.Password))
        {
            ViewModel.ErrorMessage = "Please enter your password";
            return;
        }

        // Close dialog with success
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
