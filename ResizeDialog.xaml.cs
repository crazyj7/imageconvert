using System.Windows;

namespace ImageEditor
{
    public partial class ResizeDialog : Window
    {
        public int NewWidth { get; private set; }
        public int NewHeight { get; private set; }
        private readonly double aspectRatio;

        public ResizeDialog(int currentWidth, int currentHeight)
        {
            InitializeComponent();
            
            NewWidth = currentWidth;
            NewHeight = currentHeight;
            aspectRatio = (double)currentWidth / currentHeight;

            txtWidth.Text = currentWidth.ToString();
            txtHeight.Text = currentHeight.ToString();

            txtWidth.TextChanged += TxtWidth_TextChanged;
            txtHeight.TextChanged += TxtHeight_TextChanged;
        }

        private void TxtWidth_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!int.TryParse(txtWidth.Text, out int width)) return;
            if (chkMaintainAspect.IsChecked == true)
            {
                txtHeight.TextChanged -= TxtHeight_TextChanged;
                txtHeight.Text = ((int)(width / aspectRatio)).ToString();
                txtHeight.TextChanged += TxtHeight_TextChanged;
            }
        }

        private void TxtHeight_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!int.TryParse(txtHeight.Text, out int height)) return;
            if (chkMaintainAspect.IsChecked == true)
            {
                txtWidth.TextChanged -= TxtWidth_TextChanged;
                txtWidth.Text = ((int)(height * aspectRatio)).ToString();
                txtWidth.TextChanged += TxtWidth_TextChanged;
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtWidth.Text, out int width) && 
                int.TryParse(txtHeight.Text, out int height))
            {
                NewWidth = width;
                NewHeight = height;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("올바른 숫자를 입력하세요.", "오류", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
} 