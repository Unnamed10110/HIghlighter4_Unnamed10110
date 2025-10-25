using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;

namespace Highlighter4
{
    public partial class HighlighterWindow : Window
    {
        public HighlighterWindow()
        {
            InitializeComponent();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Handle key events for the overlay window
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Handle mouse down events
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Handle mouse up events
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            // Handle mouse move events
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Handle mouse wheel events
        }

        private void TextEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                TextEditor.Visibility = Visibility.Collapsed;
                this.Focus();
            }
        }

        private void ZoomTextEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ZoomTextEditor.Visibility = Visibility.Collapsed;
                this.Focus();
            }
        }

        private void ZoomTextEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Handle text changes in zoom editor
        }

        // Public methods for external access
        public void ToggleHighlight()
        {
            // Toggle highlight mode
            if (OverlayBackground.Opacity > 0.5)
            {
                OverlayBackground.Opacity = 0.3;
            }
            else
            {
                OverlayBackground.Opacity = 0.6;
            }
        }

        public void HideHighlight()
        {
            OverlayBackground.Visibility = Visibility.Collapsed;
        }

        public void ShowHighlight()
        {
            OverlayBackground.Visibility = Visibility.Visible;
        }
    }
}
