using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AttentionOfUser
{
    /// <summary>
    /// Interaction logic for PresentResult.xaml
    /// </summary>
    public partial class PresentResult : Window
    {
        public PresentResult()
        {
            InitializeComponent();
        }

        private void OK(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void REFUSE(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
