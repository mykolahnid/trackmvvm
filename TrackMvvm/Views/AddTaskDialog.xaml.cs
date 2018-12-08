using System.Windows;
using System.Windows.Input;

namespace TrackMvvm.Views
{
    /// <summary>
    /// Interaction logic for AddTaskDialog.xaml
    /// </summary>
    public partial class AddTaskDialog : Window
    {
        public AddTaskDialog()
        {
            InitializeComponent();
            FocusManager.SetFocusedElement(this, TaskNameTextBox);
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
