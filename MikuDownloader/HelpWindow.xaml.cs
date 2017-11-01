using System.Windows;

namespace MikuDownloader
{
    /// <summary>
    /// Interaction logic for HelpWindow.xaml
    /// </summary>
    public partial class HelpWindow : Window
    {
        bool isClosing = false;

        public HelpWindow()
        {
            InitializeComponent();
        }

        public HelpWindow(string helpText)
        {
            InitializeComponent();
            txtBlockInfo.Text = helpText;
        }

        private void Window_Deactivated(object sender, System.EventArgs e)
        {
            if (!isClosing)
            {
                Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            isClosing = true;
        }
    }
}