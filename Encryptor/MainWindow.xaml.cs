using Encrypter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Encryptor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Windows.Forms.NotifyIcon ni;

        public MainWindow()
        {
            InitializeComponent();

            ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = new System.Drawing.Icon("password.ico");
            ni.Visible = true;
            ni.DoubleClick +=
                delegate (object? sender, EventArgs args)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };
            
            var contextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            contextMenuStrip.Items.Add("Close");
            
            contextMenuStrip.ItemClicked += (s,e)=> 
            {
                ni.Visible = false;
                Application.Current.Shutdown();
            };
            ni.ContextMenuStrip = contextMenuStrip;
            ni.Text = "Encryptor";
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
                this.Hide();

            base.OnStateChanged(e);
        }



        private void btnEncrypt_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(text.Text) && !string.IsNullOrWhiteSpace(passphrase.Text))
            {
                try
                {
                    output.Text = StringCipher.Encrypt(text.Text, passphrase.Text);
                }
                catch (Exception ex)
                {
                    output.Text = ex.Message;
                }
            }
        }

        private void btnDecrypt_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(text.Text) && !string.IsNullOrWhiteSpace(passphrase.Text))
            {
                try
                {
                    output.Text = StringCipher.Decrypt(text.Text, passphrase.Text);
                }
                catch (Exception ex)
                {
                    output.Text = ex.Message;
                }
            }
        }

        private void FontAwesome_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(output.Text);
        }

        private void close_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
            Clear();
        }

        private void delete_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Clear();
        }

        private void Clear()
        {
            text.Text = passphrase.Text = output.Text = null;
        }
    }
}
