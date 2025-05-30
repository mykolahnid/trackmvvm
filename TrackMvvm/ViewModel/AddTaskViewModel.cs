using CommunityToolkit.Mvvm.ComponentModel;

namespace TrackMvvm.ViewModel
{
    public partial class AddTaskViewModel : ObservableObject
    {
        [ObservableProperty]
        private string taskName = "";
    }
}