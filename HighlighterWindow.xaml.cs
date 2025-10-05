using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace Highlighter4
{
    public partial class HighlighterWindow : Window
    {
        private static void LogToFile(string message)
        {
            try
            {
                string logFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Highlighter4_Debug.log");
                File.AppendAllText(logFile, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
            }
            catch { }
        }
        // State variables
        private bool isVisible = false;
        private bool isDrawingMode = false;
        private bool isDrawing = false;
        private bool isSelectingRegion = false;
        private bool isTextMode = false;
        
        // Drawing variables
        private System.Windows.Point startPoint;
        private System.Windows.Point endPoint;
        private List<System.Drawing.Rectangle> highlightAreas = new List<System.Drawing.Rectangle>();
         private List<System.Windows.Shapes.Shape> highlightShapes = new List<System.Windows.Shapes.Shape>();
         
         // Track visual elements for each region to enable selective removal
         private List<List<System.Windows.Shapes.Shape>> regionVisualElements = new List<List<System.Windows.Shapes.Shape>>();
         
        // Track dark overlay rectangles for each region
        private List<List<System.Windows.Shapes.Shape>> regionDarkOverlays = new List<List<System.Windows.Shapes.Shape>>();
        
        // Zoom functionality variables
        private bool isZoomed = false;
        private double zoomFactor = 1.0;
        private System.Drawing.Rectangle zoomedRegion;
        private double originalZoomX = 0;
        private double originalZoomY = 0;
        private System.Windows.Media.Imaging.BitmapSource cachedScreenContent;
        private System.Windows.Media.Imaging.BitmapSource fullScreenContent;
        private bool isZoomTextEditorVisible = false;
        
        // Text editing variables
        private string textContent = "";
        private bool showTextBox = false;
        private bool isEditingText = false;
        private int textRegionIndex = -1;
        private System.Drawing.Rectangle textBoxArea;
        
        // Image variables
        private List<System.Drawing.Image> clipboardImages = new List<System.Drawing.Image>();
        private List<System.Drawing.Point> imagePositions = new List<System.Drawing.Point>();
        
        // Drawing tools
        private bool isStraightLineMode = false;
        private bool isArrowMode = false;
        private bool isRectangleMode = false;
        private bool isHighlighterMode = false;
        
        // Arrow drawing variables
        private System.Windows.Point arrowStartPoint;
        private bool isDrawingArrow = false;
        private System.Windows.Shapes.Path previewArrow;
        
        // Line drawing variables
        private bool isLineMode = false;
        private System.Windows.Point lineStartPoint;
        private bool isDrawingLine = false;
        private System.Windows.Shapes.Line previewLine;
        
        // Rectangle drawing variables
        private bool isRectangleDrawingMode = false;
        private System.Windows.Point rectangleStartPoint;
        private bool isDrawingRectangle = false;
        private System.Windows.Shapes.Rectangle previewRectangle;
        
        // Highlight drawing variables
        private bool isHighlightMode = false;
        private System.Windows.Point highlightStartPoint;
        private bool isDrawingHighlight = false;
        private System.Windows.Shapes.Rectangle previewHighlight;
        
        // Blur drawing variables
        private bool isBlurMode = false;
        private System.Windows.Point blurStartPoint;
        private bool isDrawingBlur = false;
        private System.Windows.Shapes.Rectangle previewBlur;
        private System.Windows.Shapes.Rectangle? pixelationSelectionBorder;
        private System.Windows.Media.Animation.Storyboard? pixelationBorderAnimation;
        
        // Marching ants animations for preview elements
        private System.Windows.Media.Animation.Storyboard? arrowPreviewAnimation;
        private System.Windows.Media.Animation.Storyboard? linePreviewAnimation;
        private System.Windows.Media.Animation.Storyboard? rectanglePreviewAnimation;
        private System.Windows.Media.Animation.Storyboard? highlightPreviewAnimation;
        
        // Speech balloons
        private List<SpeechBalloon> speechBalloons = new List<SpeechBalloon>();
        private bool isSpeechBalloonMode = false;
        private bool isStretchingTail = false;
        private SpeechBalloon? currentStretchingBalloon = null;
        private System.Windows.Point tailStartPoint;
        private SpeechBalloon? draggedSpeechBalloon = null;
        private System.Windows.Point lastDragPosition;
        
        
        // Undo functionality
        private Stack<List<System.Drawing.Rectangle>> undoStack = new Stack<List<System.Drawing.Rectangle>>();
         private Stack<List<System.Windows.Shapes.Shape>> shapeUndoStack = new Stack<List<System.Windows.Shapes.Shape>>();
        
        public HighlighterWindow()
        {
            InitializeComponent();
            this.Loaded += HighlighterWindow_Loaded;
        }

        private void HighlighterWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Set window to cover entire screen
            this.Left = 0;
            this.Top = 0;
            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;
        }

        public void ToggleHighlight()
        {
            if (isVisible)
            {
                HideHighlight();
            }
            else
            {
                ShowHighlight();
            }
        }

         private void ShowHighlight()
         {
             LogToFile("ShowHighlight called");
             isVisible = true;
             
            // Clear any previous drawings
            ClearAllDrawings();
            
            // Clear zoom text editor content when starting overlay
            if (ZoomTextEditor != null)
            {
                ZoomTextEditor.Document.Blocks.Clear();
                isZoomTextEditorVisible = false;
                ZoomTextEditor.Visibility = Visibility.Collapsed;
            }
            
            // Capture full screen content BEFORE showing overlay to avoid capturing the overlay itself
            fullScreenContent = CaptureFullScreen();
            LogToFile($"Full screen content captured: {fullScreenContent != null}");
             
             // Ensure window is properly positioned and sized
             this.Left = 0;
             this.Top = 0;
             this.Width = SystemParameters.PrimaryScreenWidth;
             this.Height = SystemParameters.PrimaryScreenHeight;
             this.WindowState = WindowState.Normal; // Ensure it's not minimized
             
             System.Diagnostics.Debug.WriteLine($"Window dimensions: {this.Width}x{this.Height} at ({this.Left}, {this.Top})");
             
             // Don't automatically enable region selection - let user click to start
             isSelectingRegion = false;
             this.Cursor = Cursors.Arrow;
             
             // Disable canvas hit testing initially so region selection works
             MainCanvas.IsHitTestVisible = false;
             
             this.Show();
             this.Activate();
             this.Focus();
             
             System.Diagnostics.Debug.WriteLine("Highlighter window shown - click to start region selection");
             
             // Capture screen content for all regions
             CaptureScreenContent();
         }

        public void HideHighlight()
        {
            System.Diagnostics.Debug.WriteLine("HideHighlight called");
            isVisible = false;
            this.Hide();
            
            // Clear all drawings
            ClearAllDrawings();
        }

        private void CaptureScreenContent()
        {
            // Implementation will be added - this captures the screen content
            // for the overlay to display
        }

        private void ClearAllDrawings()
        {
            MainCanvas.Children.Clear();
            highlightAreas.Clear();
            highlightShapes.Clear();
            regionVisualElements.Clear();
            regionDarkOverlays.Clear();
            speechBalloons.Clear();
            clipboardImages.Clear();
            imagePositions.Clear();
            textContent = "";
            showTextBox = false;
            isEditingText = false;
            
            // Disable canvas hit testing when clearing all drawings
            MainCanvas.IsHitTestVisible = false;
            
            // Restore the original overlay background
            OverlayBackground.Visibility = Visibility.Visible;
        }

         private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
         {
             LogToFile($"Key pressed: {e.Key}, Modifiers: {e.KeyboardDevice.Modifiers}");
             
             // Check for C key to activate capture mode when overlay is active
             if (e.Key == Key.C)
             {
                 LogToFile("C key pressed - activating capture mode");
                 // Activate capture mode with overlay content
                 if (App.mainWindow != null)
                 {
                     App.mainWindow.TriggerCaptureModeWithOverlay(this);
                 }
                 return;
             }
             
             if (e.Key == Key.Escape)
             {
                 if (isArrowMode)
                 {
                     // Exit arrow drawing mode
                     isArrowMode = false;
                     isDrawingArrow = false;
                     this.Cursor = Cursors.Arrow;
                     
                     // Clean up preview arrow
                     if (previewArrow != null)
                     {
                         MainCanvas.Children.Remove(previewArrow);
                         if (arrowPreviewAnimation != null)
                         {
                             arrowPreviewAnimation.Stop();
                             arrowPreviewAnimation = null;
                         }
                         previewArrow = null;
                     }
                     
                     LogToFile("Exited arrow drawing mode");
                 }
                 else if (isLineMode)
                 {
                     // Exit line drawing mode
                     isLineMode = false;
                     isDrawingLine = false;
                     this.Cursor = Cursors.Arrow;
                     
                     // Clean up preview line
                     if (previewLine != null)
                     {
                         MainCanvas.Children.Remove(previewLine);
                         if (linePreviewAnimation != null)
                         {
                             linePreviewAnimation.Stop();
                             linePreviewAnimation = null;
                         }
                         previewLine = null;
                     }
                     
                     LogToFile("Exited line drawing mode");
                 }
                 else if (isRectangleDrawingMode)
                 {
                     // Exit rectangle drawing mode
                     isRectangleDrawingMode = false;
                     isDrawingRectangle = false;
                     this.Cursor = Cursors.Arrow;
                     
                     // Clean up preview rectangle
                     if (previewRectangle != null)
                     {
                         MainCanvas.Children.Remove(previewRectangle);
                         if (rectanglePreviewAnimation != null)
                         {
                             rectanglePreviewAnimation.Stop();
                             rectanglePreviewAnimation = null;
                         }
                         previewRectangle = null;
                     }
                     
                     LogToFile("Exited rectangle drawing mode");
                 }
                 else if (isHighlightMode)
                 {
                     // Exit highlight drawing mode
                     isHighlightMode = false;
                     isDrawingHighlight = false;
                     this.Cursor = Cursors.Arrow;
                     
                     // Clean up preview highlight
                     if (previewHighlight != null)
                     {
                         MainCanvas.Children.Remove(previewHighlight);
                         if (highlightPreviewAnimation != null)
                         {
                             highlightPreviewAnimation.Stop();
                             highlightPreviewAnimation = null;
                         }
                         previewHighlight = null;
                     }
                     
                     LogToFile("Exited highlight drawing mode");
                 }
                 else if (isBlurMode)
                 {
                     // Exit blur drawing mode
                     isBlurMode = false;
                     isDrawingBlur = false;
                     this.Cursor = Cursors.Arrow;
                     
                     // Clean up preview blur
                     if (previewBlur != null)
                     {
                         MainCanvas.Children.Remove(previewBlur);
                         previewBlur = null;
                     }
                     
                     // Clean up selection border
                     if (pixelationSelectionBorder != null)
                     {
                         MainCanvas.Children.Remove(pixelationSelectionBorder);
                         pixelationSelectionBorder = null;
                     }
                     
                     // Stop selection border animation
                     if (pixelationBorderAnimation != null)
                     {
                         pixelationBorderAnimation.Stop();
                         pixelationBorderAnimation = null;
                     }
                     
                     LogToFile("Exited blur drawing mode");
                 }
                 else if (isZoomed && isZoomTextEditorVisible)
                 {
                     // Hide zoom text editor
                     HideZoomTextEditor();
                     isZoomTextEditorVisible = false;
                 }
                 else if (isZoomed)
                 {
                     // Exit zoom mode
                     ExitZoomMode();
                 }
                 else if (isSelectingRegion)
                 {
                     // Exit region selection mode
                     isSelectingRegion = false;
                     this.Cursor = Cursors.Arrow;
                 }
                 else
                 {
                     HideHighlight();
                 }
             }
             else if (e.Key == Key.Enter)
             {
                 // Exit region selection mode after making selections
                 if (isSelectingRegion)
                 {
                     isSelectingRegion = false;
                     this.Cursor = Cursors.Arrow;
                 }
             }
              else if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
              {
                  UndoLastSelection();
              }
              else if (e.Key == Key.T && Keyboard.Modifiers == ModifierKeys.Control)
              {
                  LogToFile($"Ctrl+T pressed. isZoomed: {isZoomed}");
                  if (isZoomed)
                  {
                      LogToFile("Calling ToggleZoomTextEditor()");
                      ToggleZoomTextEditor();
                  }
                  else
                  {
                      LogToFile("Calling ToggleTextBox()");
                      ToggleTextBox();
                  }
              }
             else if (e.Key == Key.F1)
             {
                 ToggleArrowMode();
             }
             else if (e.Key == Key.F2)
             {
                 ToggleLineMode();
             }
             else if (e.Key == Key.F3)
             {
                 ToggleRectangleMode();
             }
             else if (e.Key == Key.F4)
             {
                 ToggleHighlightMode();
             }
            else if (e.Key == Key.F5)
            {
                ToggleBlurMode();
            }
            else if (e.Key == Key.F6)
            {
                ToggleSpeechBalloonMode();
            }
             else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
             {
                 CopySelectedRegion();
             }
             else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
             {
                 SaveCurrentCapture();
             }
             // Add more key handlers as needed
         }

        private void ToggleTextBox()
        {
            if (highlightAreas.Count == 0) return;
            
            showTextBox = !showTextBox;
            isEditingText = showTextBox;
            
            if (showTextBox)
            {
                ShowTextBox();
            }
            else
            {
                HideTextBox();
            }
        }

        private void ShowTextBox()
        {
            if (highlightAreas.Count == 0) return;
            
            int lastIndex = highlightAreas.Count - 1;
            System.Drawing.Rectangle region = highlightAreas[lastIndex];
            
            // Calculate textbox position and size (80% of screen width)
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double textBoxWidth = screenWidth * 0.8;
            double textBoxHeight = 200; // Default height
            
            double textBoxX = region.X + (region.Width - textBoxWidth) / 2;
            double textBoxY = region.Bottom + 10;
            
            // Position and show the text editor
            Canvas.SetLeft(TextEditor, textBoxX);
            Canvas.SetTop(TextEditor, textBoxY);
            TextEditor.Width = textBoxWidth;
            TextEditor.Height = textBoxHeight;
            TextEditor.Visibility = Visibility.Visible;
            TextEditor.Focus();
            
            textRegionIndex = lastIndex;
            textBoxArea = new System.Drawing.Rectangle((int)textBoxX, (int)textBoxY, (int)textBoxWidth, (int)textBoxHeight);
        }

        private void HideTextBox()
        {
            TextEditor.Visibility = Visibility.Collapsed;
            textContent = new TextRange(TextEditor.Document.ContentStart, TextEditor.Document.ContentEnd).Text;
            showTextBox = false;
            isEditingText = false;
            textRegionIndex = -1;
            textBoxArea = System.Drawing.Rectangle.Empty;
        }

        private void ToggleZoomTextEditor()
        {
            LogToFile($"ToggleZoomTextEditor called. isZoomed: {isZoomed}");
            if (!isZoomed) return;
            
            isZoomTextEditorVisible = !isZoomTextEditorVisible;
            LogToFile($"isZoomTextEditorVisible toggled to: {isZoomTextEditorVisible}");
            
            if (isZoomTextEditorVisible)
            {
                LogToFile("Showing zoom text editor");
                ShowZoomTextEditor();
            }
            else
            {
                LogToFile("Hiding zoom text editor");
                HideZoomTextEditor();
            }
        }

        private void ShowZoomTextEditor()
        {
            LogToFile($"ShowZoomTextEditor called. isZoomed: {isZoomed}");
            if (!isZoomed) return;
            
            // Calculate position under the zoomed region
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            
            // Get the zoomed region dimensions
            var zoomedWidth = zoomedRegion.Width * zoomFactor;
            var zoomedHeight = zoomedRegion.Height * zoomFactor;
            
            // Calculate the zoomed region position (10% higher from center)
            var centerX = (screenWidth - zoomedWidth) / 2;
            var centerY = (screenHeight - zoomedHeight) / 2 - (screenHeight * 0.1);
            
            // Position text editor with top right under zoomed region and bottom near screen bottom
            var textBoxWidth = CalculateOptimalTextEditorWidth(zoomedWidth, screenWidth);
            var textBoxX = centerX + (zoomedWidth - textBoxWidth) / 2;
            var textBoxTopY = centerY + zoomedHeight + 10; // 10 pixels below the region
            var textBoxBottomY = screenHeight - 20; // 20 pixels from bottom of screen
            var textBoxHeight = textBoxBottomY - textBoxTopY; // Fill available space
            var textBoxY = textBoxTopY;
            
            LogToFile($"Text editor position: X={textBoxX}, Y={textBoxY}, Width={textBoxWidth}, Height={textBoxHeight}");
            LogToFile($"Screen size: {screenWidth}x{screenHeight}, Zoomed region: {zoomedWidth}x{zoomedHeight} at ({centerX}, {centerY})");
            
            // Ensure text editor is in the canvas
            if (!MainCanvas.Children.Contains(ZoomTextEditor))
            {
                MainCanvas.Children.Add(ZoomTextEditor);
            }
            
            // Set text editor position and size
            Canvas.SetLeft(ZoomTextEditor, textBoxX);
            Canvas.SetTop(ZoomTextEditor, textBoxY);
            ZoomTextEditor.Width = textBoxWidth;
            ZoomTextEditor.Height = textBoxHeight;
            ZoomTextEditor.Visibility = Visibility.Visible;
            
            // Enable hit testing for the text editor (override canvas setting)
            ZoomTextEditor.IsHitTestVisible = true;
            
            // Ensure text editor is on top of other elements
            Panel.SetZIndex(ZoomTextEditor, 1000);
            
            // Force update layout
            ZoomTextEditor.UpdateLayout();
            MainCanvas.UpdateLayout();
            
            ZoomTextEditor.Focus();
            
            LogToFile($"Text editor visibility set to: {ZoomTextEditor.Visibility}");
            LogToFile($"Text editor actual position: Canvas.Left={Canvas.GetLeft(ZoomTextEditor)}, Canvas.Top={Canvas.GetTop(ZoomTextEditor)}");
            LogToFile($"Text editor Z-index: {Panel.GetZIndex(ZoomTextEditor)}");
        }

        private void HideZoomTextEditor()
        {
            ZoomTextEditor.Visibility = Visibility.Collapsed;
        }

        private double CalculateOptimalTextEditorWidth(double zoomedWidth, double screenWidth)
        {
            // Default width is 80% of screen width
            var defaultWidth = screenWidth * 0.8;
            var maxWidth = screenWidth * 0.8; // Maximum 80% of screen width
            
            // If text editor has content, calculate width based on content
            if (ZoomTextEditor.Document != null && ZoomTextEditor.Document.Blocks.Count > 0)
            {
                var contentWidth = CalculateContentWidth(ZoomTextEditor);
                if (contentWidth > 0)
                {
                    // Use content width but respect max bounds, with minimum of default width
                    return Math.Max(defaultWidth, Math.Min(maxWidth, contentWidth + 40)); // Add padding
                }
            }
            
            // Default to 80% of screen width
            return defaultWidth;
        }

        private double CalculateContentWidth(RichTextBox textBox)
        {
            try
            {
                var maxWidth = 0.0;
                
                // Create a temporary text block to measure content
                var textRange = new TextRange(textBox.Document.ContentStart, textBox.Document.ContentEnd);
                
                // Create a temporary TextBlock to measure text
                var tempTextBlock = new System.Windows.Controls.TextBlock
                {
                    FontFamily = textBox.FontFamily,
                    FontSize = textBox.FontSize,
                    FontWeight = textBox.FontWeight,
                    FontStyle = textBox.FontStyle
                };
                
                // Split content into lines and measure each
                var text = textRange.Text;
                if (!string.IsNullOrEmpty(text))
                {
                    var lines = text.Split('\n');
                    foreach (var line in lines)
                    {
                        tempTextBlock.Text = line;
                        tempTextBlock.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                        maxWidth = Math.Max(maxWidth, tempTextBlock.DesiredSize.Width);
                    }
                }
                
                // Also check for inline images in the document
                foreach (var block in textBox.Document.Blocks)
                {
                    if (block is System.Windows.Documents.Paragraph paragraph)
                    {
                        foreach (var inline in paragraph.Inlines)
                        {
                            if (inline is System.Windows.Documents.InlineUIContainer container)
                            {
                                if (container.Child is System.Windows.Controls.Image image)
                                {
                                    // Use both ActualWidth and Source.Width for better image width detection
                                    var imageWidth = image.ActualWidth > 0 ? image.ActualWidth : 
                                                   (image.Source?.Width ?? 0);
                                    maxWidth = Math.Max(maxWidth, imageWidth);
                                }
                            }
                        }
                    }
                }
                
                return maxWidth;
            }
            catch
            {
                return 0;
            }
        }

        private void UpdateZoomTextEditorPosition(double centerX, double centerY, double zoomedWidth, double zoomedHeight)
        {
            if (!isZoomTextEditorVisible) return;
            
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            
            // Position text editor with top right under zoomed region and bottom near screen bottom
            var textBoxWidth = CalculateOptimalTextEditorWidth(zoomedWidth, screenWidth);
            var textBoxX = centerX + (zoomedWidth - textBoxWidth) / 2;
            var textBoxTopY = centerY + zoomedHeight + 10; // 10 pixels below the region
            var textBoxBottomY = SystemParameters.PrimaryScreenHeight - 20; // 20 pixels from bottom of screen
            var textBoxHeight = textBoxBottomY - textBoxTopY; // Fill available space
            var textBoxY = textBoxTopY;
            
            // Update text editor position and size
            Canvas.SetLeft(ZoomTextEditor, textBoxX);
            Canvas.SetTop(ZoomTextEditor, textBoxY);
            ZoomTextEditor.Width = textBoxWidth;
            ZoomTextEditor.Height = textBoxHeight;
        }

        private void ToggleDrawingMode()
        {
            isDrawingMode = !isDrawingMode;
            if (isDrawingMode)
            {
                this.Cursor = Cursors.Cross;
            }
            else
            {
                this.Cursor = Cursors.Arrow;
            }
        }

        private void DeactivateAllDrawingTools()
        {
            // Deactivate all drawing modes
            isArrowMode = false;
            isLineMode = false;
            isRectangleDrawingMode = false;
            isHighlightMode = false;
            isBlurMode = false;
            isSpeechBalloonMode = false;
            
            // Stop any active drawing
            isDrawingArrow = false;
            isDrawingLine = false;
            isDrawingRectangle = false;
            isDrawingHighlight = false;
            isDrawingBlur = false;
            
            // Clean up all preview elements
            if (previewArrow != null)
            {
                MainCanvas.Children.Remove(previewArrow);
                if (arrowPreviewAnimation != null)
                {
                    arrowPreviewAnimation.Stop();
                    arrowPreviewAnimation = null;
                }
                previewArrow = null;
            }
            if (previewLine != null)
            {
                MainCanvas.Children.Remove(previewLine);
                if (linePreviewAnimation != null)
                {
                    linePreviewAnimation.Stop();
                    linePreviewAnimation = null;
                }
                previewLine = null;
            }
            if (previewRectangle != null)
            {
                MainCanvas.Children.Remove(previewRectangle);
                if (rectanglePreviewAnimation != null)
                {
                    rectanglePreviewAnimation.Stop();
                    rectanglePreviewAnimation = null;
                }
                previewRectangle = null;
            }
            if (previewHighlight != null)
            {
                MainCanvas.Children.Remove(previewHighlight);
                if (highlightPreviewAnimation != null)
                {
                    highlightPreviewAnimation.Stop();
                    highlightPreviewAnimation = null;
                }
                previewHighlight = null;
            }
            if (previewBlur != null)
            {
                MainCanvas.Children.Remove(previewBlur);
                previewBlur = null;
            }
            if (pixelationSelectionBorder != null)
            {
                MainCanvas.Children.Remove(pixelationSelectionBorder);
                pixelationSelectionBorder = null;
            }
            if (pixelationBorderAnimation != null)
            {
                pixelationBorderAnimation.Stop();
                pixelationBorderAnimation = null;
            }
            
            // Reset cursor
            this.Cursor = Cursors.Arrow;
            
            LogToFile("Deactivated all drawing tools");
        }

        private void ToggleArrowMode()
        {
            if (!isArrowMode)
            {
                // Deactivate all other tools first
                DeactivateAllDrawingTools();
                
                // Disable region selection when entering arrow mode
                isSelectingRegion = false;
                
                // Enable arrow mode
                isArrowMode = true;
                MainCanvas.IsHitTestVisible = false; // Keep false so mouse events reach window handler
                this.Cursor = Cursors.Cross;
                LogToFile("Entered arrow drawing mode");
                LogToFile($"MainCanvas.IsHitTestVisible = {MainCanvas.IsHitTestVisible}");
            }
            else
            {
                // Disable arrow mode
                isArrowMode = false;
                MainCanvas.IsHitTestVisible = false;
                this.Cursor = Cursors.Arrow;
                isDrawingArrow = false;
                
                // Clean up preview arrow
                if (previewArrow != null)
                {
                    MainCanvas.Children.Remove(previewArrow);
                    previewArrow = null;
                }
                
                LogToFile("Exited arrow drawing mode");
            }
        }

        private void ToggleLineMode()
        {
            if (!isLineMode)
            {
                // Deactivate all other tools first
                DeactivateAllDrawingTools();
                
                // Disable region selection when entering line mode
                isSelectingRegion = false;
                
                // Enable line mode
                isLineMode = true;
                MainCanvas.IsHitTestVisible = false; // Keep false so mouse events reach window handler
                this.Cursor = Cursors.Cross;
                LogToFile("Entered line drawing mode");
            }
            else
            {
                // Disable line mode
                isLineMode = false;
                MainCanvas.IsHitTestVisible = false;
                this.Cursor = Cursors.Arrow;
                isDrawingLine = false;
                
                // Clean up preview line
                if (previewLine != null)
                {
                    MainCanvas.Children.Remove(previewLine);
                    previewLine = null;
                }
                
                LogToFile("Exited line drawing mode");
            }
        }

        private void ToggleRectangleMode()
        {
            if (!isRectangleDrawingMode)
            {
                // Deactivate all other tools first
                DeactivateAllDrawingTools();
                
                // Disable region selection when entering rectangle mode
                isSelectingRegion = false;
                
                // Enable rectangle mode
                isRectangleDrawingMode = true;
                MainCanvas.IsHitTestVisible = false; // Keep false so mouse events reach window handler
                this.Cursor = Cursors.Cross;
                LogToFile("Entered rectangle drawing mode");
            }
            else
            {
                // Disable rectangle mode
                isRectangleDrawingMode = false;
                MainCanvas.IsHitTestVisible = false;
                this.Cursor = Cursors.Arrow;
                isDrawingRectangle = false;
                
                // Clean up preview rectangle
                if (previewRectangle != null)
                {
                    MainCanvas.Children.Remove(previewRectangle);
                    previewRectangle = null;
                }
                
                LogToFile("Exited rectangle drawing mode");
            }
        }

        private void ToggleHighlightMode()
        {
            if (!isHighlightMode)
            {
                // Deactivate all other tools first
                DeactivateAllDrawingTools();
                
                // Disable region selection when entering highlight mode
                isSelectingRegion = false;
                
                // Enable highlight mode
                isHighlightMode = true;
                MainCanvas.IsHitTestVisible = false; // Keep false so mouse events reach window handler
                this.Cursor = Cursors.Cross;
                LogToFile("Entered highlight drawing mode");
            }
            else
            {
                // Disable highlight mode
                isHighlightMode = false;
                MainCanvas.IsHitTestVisible = false;
                this.Cursor = Cursors.Arrow;
                isDrawingHighlight = false;
                
                // Clean up preview highlight
                if (previewHighlight != null)
                {
                    MainCanvas.Children.Remove(previewHighlight);
                    previewHighlight = null;
                }
                
                LogToFile("Exited highlight drawing mode");
            }
        }

        private void ToggleBlurMode()
        {
            if (!isBlurMode)
            {
                // Deactivate all other tools first
                DeactivateAllDrawingTools();
                
                // Disable region selection when entering blur mode
                isSelectingRegion = false;
                
                // Enable blur mode
                isBlurMode = true;
                MainCanvas.IsHitTestVisible = false; // Keep false so mouse events reach window handler
                this.Cursor = Cursors.Cross;
                LogToFile("Entered blur drawing mode");
            }
            else
            {
                // Disable blur mode
                isBlurMode = false;
                MainCanvas.IsHitTestVisible = false;
                this.Cursor = Cursors.Arrow;
                isDrawingBlur = false;
                
                // Clean up preview blur
                if (previewBlur != null)
                {
                    MainCanvas.Children.Remove(previewBlur);
                    previewBlur = null;
                }
                
                // Clean up selection border
                if (pixelationSelectionBorder != null)
                {
                    MainCanvas.Children.Remove(pixelationSelectionBorder);
                    pixelationSelectionBorder = null;
                }
                
                // Stop selection border animation
                if (pixelationBorderAnimation != null)
                {
                    pixelationBorderAnimation.Stop();
                    pixelationBorderAnimation = null;
                }
                
                LogToFile("Exited blur drawing mode");
            }
        }

        private void ToggleSpeechBalloonMode()
        {
            if (!isSpeechBalloonMode)
            {
                // Deactivate all other tools first
                DeactivateAllDrawingTools();
                
                // Enable speech balloon mode
                isSpeechBalloonMode = true;
                MainCanvas.IsHitTestVisible = true;
                this.Cursor = Cursors.Hand;
                LogToFile("Entered speech balloon mode");
            }
            else
            {
                // Disable speech balloon mode
                isSpeechBalloonMode = false;
                MainCanvas.IsHitTestVisible = false;
                this.Cursor = Cursors.Arrow;
                LogToFile("Exited speech balloon mode");
            }
        }

        private void CreateSpeechBalloon(System.Windows.Point position)
        {
            // Enable canvas hit testing for speech balloon creation
            MainCanvas.IsHitTestVisible = true;
            
            // Create a simple input dialog using WPF
            var inputDialog = new System.Windows.Window
            {
                Title = "Speech Balloon Text",
                Width = 300,
                Height = 200,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                ResizeMode = System.Windows.ResizeMode.NoResize,
                Topmost = true,
                WindowStyle = System.Windows.WindowStyle.ToolWindow,
                Background = System.Windows.Media.Brushes.Black,
                Foreground = System.Windows.Media.Brushes.White
            };

            var stackPanel = new System.Windows.Controls.StackPanel
            {
                Margin = new System.Windows.Thickness(10)
            };

            var textBox = new System.Windows.Controls.TextBox
            {
                Margin = new System.Windows.Thickness(0, 10, 0, 10),
                Height = 60,
                AcceptsReturn = true,
                TextWrapping = System.Windows.TextWrapping.Wrap,
                Background = System.Windows.Media.Brushes.Black,
                Foreground = System.Windows.Media.Brushes.White,
                BorderBrush = System.Windows.Media.Brushes.Gray
            };

            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new System.Windows.Thickness(0, 10, 0, 0)
            };

            var okButton = new System.Windows.Controls.Button
            {
                Content = "OK",
                Width = 75,
                Height = 25,
                Margin = new System.Windows.Thickness(5, 0, 5, 0),
                IsDefault = true,
                Background = System.Windows.Media.Brushes.DarkGray,
                Foreground = System.Windows.Media.Brushes.White,
                BorderBrush = System.Windows.Media.Brushes.Gray
            };

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Width = 75,
                Height = 25,
                Margin = new System.Windows.Thickness(5, 0, 0, 0),
                IsCancel = true,
                Background = System.Windows.Media.Brushes.DarkGray,
                Foreground = System.Windows.Media.Brushes.White,
                BorderBrush = System.Windows.Media.Brushes.Gray
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            var label = new System.Windows.Controls.Label 
            { 
                Content = "Enter text for speech balloon:",
                Foreground = System.Windows.Media.Brushes.White
            };
            stackPanel.Children.Add(label);
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(buttonPanel);

            inputDialog.Content = stackPanel;

            string enteredText = "";
            bool dialogResult = false;

            // Add key handler for Enter and Shift+Enter
            textBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    if (Keyboard.Modifiers == ModifierKeys.Shift)
                    {
                        // Shift+Enter: Insert line break at cursor position
                        int caretIndex = textBox.CaretIndex;
                        textBox.Text = textBox.Text.Insert(caretIndex, "\n");
                        textBox.CaretIndex = caretIndex + 1;
                        e.Handled = true;
                    }
                    else
                    {
                        // Enter: Close popup and accept text
                        enteredText = textBox.Text;
                        dialogResult = true;
                        inputDialog.Close();
                        e.Handled = true;
                    }
                }
            };

            okButton.Click += (s, e) =>
            {
                enteredText = textBox.Text;
                dialogResult = true;
                inputDialog.Close();
            };

            cancelButton.Click += (s, e) =>
            {
                dialogResult = false;
                // Disable canvas hit testing when canceling
                MainCanvas.IsHitTestVisible = false;
                inputDialog.Close();
            };

            inputDialog.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    enteredText = textBox.Text;
                    dialogResult = true;
                    inputDialog.Close();
                }
                else if (e.Key == System.Windows.Input.Key.Escape)
                {
                    dialogResult = false;
                    // Disable canvas hit testing when canceling
                    MainCanvas.IsHitTestVisible = false;
                    inputDialog.Close();
                }
            };

            // Focus on text box
            textBox.Focus();
            textBox.SelectAll();

            inputDialog.ShowDialog();
            
            if (dialogResult && !string.IsNullOrWhiteSpace(enteredText))
            {
                // Create speech balloon with the entered text
                var speechBalloon = new SpeechBalloon(enteredText, position);
                speechBalloons.Add(speechBalloon);
                
                // Add to canvas
                MainCanvas.Children.Add(speechBalloon.BalloonShape);
                MainCanvas.Children.Add(speechBalloon.TextBlock);
                
                // Make it draggable and add tail stretching capability
                MakeDraggable(speechBalloon);
                MakeTailStretchable(speechBalloon);
                
                LogToFile($"Created speech balloon with text: '{enteredText}' at position: {position}");
                
                // Auto-deactivate speech balloon mode after creation
                isSpeechBalloonMode = false;
                this.Cursor = Cursors.Arrow;
                LogToFile("Auto-deactivated speech balloon mode after text insertion");
            }
            else
            {
                // Disable canvas hit testing if no speech balloon was created
                MainCanvas.IsHitTestVisible = false;
            }
        }


        private void HandleSpeechBalloonDrag(System.Windows.Point position, System.Windows.FrameworkElement clickedElement)
        {
            LogToFile($"HandleSpeechBalloonDrag called at position: {position}");
            
            // Find the speech balloon that contains this element
            SpeechBalloon? targetBalloon = null;
            foreach (var balloon in speechBalloons)
            {
                if (balloon.BalloonShape == clickedElement || 
                    balloon.TextBlock == clickedElement || 
                    balloon.DragArea == clickedElement)
                {
                    targetBalloon = balloon;
                    break;
                }
            }
            
            if (targetBalloon != null)
            {
                LogToFile($"Found target speech balloon for dragging");
                draggedSpeechBalloon = targetBalloon;
                lastDragPosition = position;
                this.Cursor = Cursors.Hand;
                LogToFile($"Started dragging speech balloon at: {position}");
                    }
                    else
                    {
                LogToFile($"No speech balloon found for clicked element");
            }
        }

        private void MakeDraggable(SpeechBalloon speechBalloon)
        {
            LogToFile("MakeDraggable called - creating drag area");
            
            // Create a transparent rectangle overlay for reliable mouse capture
            var dragArea = new System.Windows.Shapes.Rectangle
            {
                Width = speechBalloon.BalloonWidth,
                Height = speechBalloon.BalloonHeight,
                Fill = System.Windows.Media.Brushes.Transparent,
                Stroke = System.Windows.Media.Brushes.Transparent,
                Tag = "SpeechBalloonDragArea",
                IsHitTestVisible = true, // Ensure it can receive mouse events
                Cursor = Cursors.Hand // Show hand cursor when hovering
            };

            // Position the drag area over the balloon
            Canvas.SetLeft(dragArea, Canvas.GetLeft(speechBalloon.BalloonShape));
            Canvas.SetTop(dragArea, Canvas.GetTop(speechBalloon.BalloonShape));
            Panel.SetZIndex(dragArea, 1002); // Above balloon but below text

            MainCanvas.Children.Add(dragArea);
            LogToFile($"Drag area created and added at position: {Canvas.GetLeft(dragArea)}, {Canvas.GetTop(dragArea)}");

            // Store reference for cleanup
            speechBalloon.DragArea = dragArea;

            // Create shared dragging state variables
            bool isDragging = false;
            System.Windows.Point lastPosition = new System.Windows.Point();

            // Add mouse enter/leave events for debugging
            dragArea.MouseEnter += (s, e) =>
            {
                LogToFile("DRAG AREA MOUSE ENTER - element is receiving mouse events");
            };

            dragArea.MouseLeave += (s, e) =>
            {
                LogToFile("DRAG AREA MOUSE LEAVE");
            };

            // Mouse down on drag area
            dragArea.MouseLeftButtonDown += (s, e) =>
            {
                // Check if any drawing tool is active - if so, don't handle this event
                if (isArrowMode || isLineMode || isRectangleDrawingMode || isHighlightMode || isBlurMode)
                {
                    LogToFile("DRAG AREA MOUSE DOWN - ignored because drawing tool is active");
                    return; // Let the event bubble up to the window handler
                }
                
                LogToFile("DRAG AREA MOUSE DOWN - starting drag");
                isDragging = true;
                lastPosition = e.GetPosition(MainCanvas);
                dragArea.CaptureMouse();
                LogToFile($"Started dragging speech balloon at: {lastPosition}");
                e.Handled = true;
            };

            // Mouse move - handle dragging
            dragArea.MouseMove += (s, e) =>
            {
                if (isDragging)
                {
                    var currentPosition = e.GetPosition(MainCanvas);
                    var deltaX = currentPosition.X - lastPosition.X;
                    var deltaY = currentPosition.Y - lastPosition.Y;

                    // Update balloon position
                    Canvas.SetLeft(speechBalloon.BalloonShape, Canvas.GetLeft(speechBalloon.BalloonShape) + deltaX);
                    Canvas.SetTop(speechBalloon.BalloonShape, Canvas.GetTop(speechBalloon.BalloonShape) + deltaY);

                    // Update text position
                    Canvas.SetLeft(speechBalloon.TextBlock, Canvas.GetLeft(speechBalloon.TextBlock) + deltaX);
                    Canvas.SetTop(speechBalloon.TextBlock, Canvas.GetTop(speechBalloon.TextBlock) + deltaY);

                    // Update drag area position
                    Canvas.SetLeft(dragArea, Canvas.GetLeft(dragArea) + deltaX);
                    Canvas.SetTop(dragArea, Canvas.GetTop(dragArea) + deltaY);

                    // Update tail tip position
                    if (speechBalloon.TailTip != null)
                    {
                        Canvas.SetLeft(speechBalloon.TailTip, Canvas.GetLeft(speechBalloon.TailTip) + deltaX);
                        Canvas.SetTop(speechBalloon.TailTip, Canvas.GetTop(speechBalloon.TailTip) + deltaY);
                    }

                    lastPosition = currentPosition;
                    e.Handled = true;
                }
            };

            // Mouse up - stop dragging
            dragArea.MouseLeftButtonUp += (s, e) =>
            {
                // Check if any drawing tool is active - if so, don't handle this event
                if (isArrowMode || isLineMode || isRectangleDrawingMode || isHighlightMode || isBlurMode)
                {
                    LogToFile("DRAG AREA MOUSE UP - ignored because drawing tool is active");
                    return; // Let the event bubble up to the window handler
                }
                
                if (isDragging)
                {
                    isDragging = false;
                    dragArea.ReleaseMouseCapture();
                    LogToFile("Stopped dragging speech balloon");
                    e.Handled = true;
                }
            };

            // Also make text draggable as backup
            speechBalloon.TextBlock.MouseLeftButtonDown += (s, e) =>
            {
                // Check if any drawing tool is active - if so, don't handle this event
                if (isArrowMode || isLineMode || isRectangleDrawingMode || isHighlightMode || isBlurMode)
                {
                    LogToFile("TEXT MOUSE DOWN - ignored because drawing tool is active");
                    return; // Let the event bubble up to the window handler
                }
                
                LogToFile("TEXT MOUSE DOWN - starting drag");
                isDragging = true;
                lastPosition = e.GetPosition(MainCanvas);
                speechBalloon.TextBlock.CaptureMouse();
                LogToFile($"Started dragging speech balloon via text at: {lastPosition}");
                e.Handled = true;
            };

            speechBalloon.TextBlock.MouseMove += (s, e) =>
            {
                if (isDragging)
                {
                    var currentPosition = e.GetPosition(MainCanvas);
                    var deltaX = currentPosition.X - lastPosition.X;
                    var deltaY = currentPosition.Y - lastPosition.Y;

                    // Update balloon position
                    Canvas.SetLeft(speechBalloon.BalloonShape, Canvas.GetLeft(speechBalloon.BalloonShape) + deltaX);
                    Canvas.SetTop(speechBalloon.BalloonShape, Canvas.GetTop(speechBalloon.BalloonShape) + deltaY);

                    // Update text position
                    Canvas.SetLeft(speechBalloon.TextBlock, Canvas.GetLeft(speechBalloon.TextBlock) + deltaX);
                    Canvas.SetTop(speechBalloon.TextBlock, Canvas.GetTop(speechBalloon.TextBlock) + deltaY);

                    // Update drag area position
                    Canvas.SetLeft(dragArea, Canvas.GetLeft(dragArea) + deltaX);
                    Canvas.SetTop(dragArea, Canvas.GetTop(dragArea) + deltaY);

                    // Update tail tip position
                    if (speechBalloon.TailTip != null)
                    {
                        Canvas.SetLeft(speechBalloon.TailTip, Canvas.GetLeft(speechBalloon.TailTip) + deltaX);
                        Canvas.SetTop(speechBalloon.TailTip, Canvas.GetTop(speechBalloon.TailTip) + deltaY);
                    }

                    lastPosition = currentPosition;
                    e.Handled = true;
                }
            };

            speechBalloon.TextBlock.MouseLeftButtonUp += (s, e) =>
            {
                // Check if any drawing tool is active - if so, don't handle this event
                if (isArrowMode || isLineMode || isRectangleDrawingMode || isHighlightMode || isBlurMode)
                {
                    LogToFile("TEXT BLOCK MOUSE UP - ignored because drawing tool is active");
                    return; // Let the event bubble up to the window handler
                }
                
                if (isDragging)
                {
                    isDragging = false;
                    speechBalloon.TextBlock.ReleaseMouseCapture();
                    LogToFile("Stopped dragging speech balloon via text");
                    e.Handled = true;
                }
            };

            // Also make the balloon shape itself draggable
            speechBalloon.BalloonShape.MouseEnter += (s, e) =>
            {
                LogToFile("BALLOON SHAPE MOUSE ENTER - element is receiving mouse events");
            };

            speechBalloon.BalloonShape.MouseLeftButtonDown += (s, e) =>
            {
                // Check if any drawing tool is active - if so, don't handle this event
                if (isArrowMode || isLineMode || isRectangleDrawingMode || isHighlightMode || isBlurMode)
                {
                    LogToFile("BALLOON SHAPE MOUSE DOWN - ignored because drawing tool is active");
                    return; // Let the event bubble up to the window handler
                }
                
                LogToFile("BALLOON SHAPE MOUSE DOWN - starting drag");
                isDragging = true;
                lastPosition = e.GetPosition(MainCanvas);
                speechBalloon.BalloonShape.CaptureMouse();
                LogToFile($"Started dragging speech balloon via balloon shape at: {lastPosition}");
                e.Handled = true;
            };

            speechBalloon.BalloonShape.MouseMove += (s, e) =>
            {
                if (isDragging)
                {
                    var currentPosition = e.GetPosition(MainCanvas);
                    var deltaX = currentPosition.X - lastPosition.X;
                    var deltaY = currentPosition.Y - lastPosition.Y;

                    // Update balloon position
                    Canvas.SetLeft(speechBalloon.BalloonShape, Canvas.GetLeft(speechBalloon.BalloonShape) + deltaX);
                    Canvas.SetTop(speechBalloon.BalloonShape, Canvas.GetTop(speechBalloon.BalloonShape) + deltaY);

                    // Update text position
                    Canvas.SetLeft(speechBalloon.TextBlock, Canvas.GetLeft(speechBalloon.TextBlock) + deltaX);
                    Canvas.SetTop(speechBalloon.TextBlock, Canvas.GetTop(speechBalloon.TextBlock) + deltaY);

                    // Update drag area position
                    Canvas.SetLeft(dragArea, Canvas.GetLeft(dragArea) + deltaX);
                    Canvas.SetTop(dragArea, Canvas.GetTop(dragArea) + deltaY);

                    // Update tail tip position
                    if (speechBalloon.TailTip != null)
                    {
                        Canvas.SetLeft(speechBalloon.TailTip, Canvas.GetLeft(speechBalloon.TailTip) + deltaX);
                        Canvas.SetTop(speechBalloon.TailTip, Canvas.GetTop(speechBalloon.TailTip) + deltaY);
                    }

                    lastPosition = currentPosition;
                    e.Handled = true;
                }
            };

            speechBalloon.BalloonShape.MouseLeftButtonUp += (s, e) =>
            {
                // Check if any drawing tool is active - if so, don't handle this event
                if (isArrowMode || isLineMode || isRectangleDrawingMode || isHighlightMode || isBlurMode)
                {
                    LogToFile("BALLOON SHAPE MOUSE UP - ignored because drawing tool is active");
                    return; // Let the event bubble up to the window handler
                }
                
                if (isDragging)
                {
                    isDragging = false;
                    speechBalloon.BalloonShape.ReleaseMouseCapture();
                    LogToFile("Stopped dragging speech balloon via balloon shape");
                    e.Handled = true;
                }
            };
        }

        private void MakeTailStretchable(SpeechBalloon speechBalloon)
        {
            // Add a visible red dot at the tip of the tail for stretching
            var tailTip = new System.Windows.Shapes.Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = System.Windows.Media.Brushes.Red,
                Stroke = System.Windows.Media.Brushes.DarkRed,
                StrokeThickness = 1,
                Tag = "TailTip"
            };

            // Position at the tip of the triangle tail (using actual balloon dimensions)
            Canvas.SetLeft(tailTip, Canvas.GetLeft(speechBalloon.BalloonShape) + speechBalloon.BalloonWidth * 0.35 - 5);
            Canvas.SetTop(tailTip, Canvas.GetTop(speechBalloon.BalloonShape) + speechBalloon.BalloonHeight + 20 - 5);
            Panel.SetZIndex(tailTip, 1002);

            MainCanvas.Children.Add(tailTip);

            bool isStretching = false;
            System.Windows.Point stretchStartPoint;

            tailTip.MouseLeftButtonDown += (s, e) =>
            {
                // Check if any drawing tool is active - if so, don't handle this event
                if (isArrowMode || isLineMode || isRectangleDrawingMode || isHighlightMode || isBlurMode)
                {
                    LogToFile("TAIL TIP MOUSE DOWN - ignored because drawing tool is active");
                    return; // Let the event bubble up to the window handler
                }
                
                isStretching = true;
                stretchStartPoint = e.GetPosition(MainCanvas);
                tailTip.CaptureMouse();
                isStretchingTail = true;
                currentStretchingBalloon = speechBalloon;
                // Set tailStartPoint to the balloon's tail connection point, not mouse position
                tailStartPoint = new System.Windows.Point(
                    Canvas.GetLeft(speechBalloon.BalloonShape) + speechBalloon.BalloonWidth * 0.35,
                    Canvas.GetTop(speechBalloon.BalloonShape) + speechBalloon.BalloonHeight
                );
                this.Cursor = Cursors.Cross;
                e.Handled = true;
            };

            tailTip.MouseMove += (s, e) =>
            {
                if (isStretching)
                {
                    var currentPoint = e.GetPosition(MainCanvas);
                    speechBalloon.UpdateTail(tailStartPoint, currentPoint);
                }
            };

            tailTip.MouseLeftButtonUp += (s, e) =>
            {
                // Check if any drawing tool is active - if so, don't handle this event
                if (isArrowMode || isLineMode || isRectangleDrawingMode || isHighlightMode || isBlurMode)
                {
                    LogToFile("TAIL TIP MOUSE UP - ignored because drawing tool is active");
                    return; // Let the event bubble up to the window handler
                }
                
                isStretching = false;
                tailTip.ReleaseMouseCapture();
                isStretchingTail = false;
                currentStretchingBalloon = null;
                this.Cursor = Cursors.Arrow;
                e.Handled = true;
            };

            // Add hover effect to show it's interactive
            tailTip.MouseEnter += (s, e) =>
            {
                tailTip.Fill = System.Windows.Media.Brushes.DarkRed;
                this.Cursor = Cursors.Cross;
            };

            tailTip.MouseLeave += (s, e) =>
            {
                if (!isStretching)
                {
                    tailTip.Fill = System.Windows.Media.Brushes.Red;
                    this.Cursor = Cursors.Arrow;
                }
            };

            // Store reference to tail tip for cleanup
            speechBalloon.TailTip = tailTip;
        }

        private void CreateArrow(System.Windows.Point start, System.Windows.Point end)
        {
            var arrow = CreateArrowPath(start, end);
            arrow.Stroke = System.Windows.Media.Brushes.Red;
            arrow.StrokeThickness = 1;
            arrow.Tag = "Arrow";
            
            // Ensure arrow is always on top of text elements (higher than ZoomTextEditor's 1000)
            Panel.SetZIndex(arrow, 1001);
            
            MainCanvas.Children.Add(arrow);
            LogToFile($"Created arrow from {start} to {end}");
            LogToFile($"Arrow added to canvas. Canvas children count: {MainCanvas.Children.Count}");
            LogToFile($"Arrow stroke: {arrow.Stroke}, thickness: {arrow.StrokeThickness}");
        }

        private void UpdateArrowPreview(System.Windows.Point start, System.Windows.Point current)
        {
            LogToFile($"UPDATE ARROW PREVIEW: Called with start={start}, current={current}");
            
            // Remove previous preview arrow
            if (previewArrow != null)
            {
                MainCanvas.Children.Remove(previewArrow);
                LogToFile($"UPDATE ARROW PREVIEW: Removed previous preview arrow");
            }

            // Create new preview arrow
            previewArrow = CreateArrowPath(start, current);
            previewArrow.Stroke = System.Windows.Media.Brushes.White;
            previewArrow.StrokeThickness = 1;
            previewArrow.StrokeDashArray = new System.Windows.Media.DoubleCollection { 5, 5 };
            previewArrow.Tag = "PreviewArrow";
            
            // Add marching ants animation
            if (arrowPreviewAnimation != null)
            {
                arrowPreviewAnimation.Stop();
            }
            arrowPreviewAnimation = CreateMarchingAntsAnimation(previewArrow);
            arrowPreviewAnimation.Begin();
            
            LogToFile($"UPDATE ARROW PREVIEW: Created new preview arrow");
            
            // Ensure preview arrow is always on top (higher than text editor and final arrows)
            Panel.SetZIndex(previewArrow, 1002);
            
            MainCanvas.Children.Add(previewArrow);
        }

        private void CreateLine(System.Windows.Point start, System.Windows.Point end)
        {
            var line = new System.Windows.Shapes.Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y,
                Stroke = System.Windows.Media.Brushes.Red,
                StrokeThickness = 1,
                Tag = "Line"
            };
            
            // Ensure line is always on top of text elements (higher than ZoomTextEditor's 1000)
            Panel.SetZIndex(line, 1001);
            
            MainCanvas.Children.Add(line);
            LogToFile($"Created line from {start} to {end}");
        }

        private void UpdateLinePreview(System.Windows.Point start, System.Windows.Point current)
        {
            // Remove previous preview line
            if (previewLine != null)
            {
                MainCanvas.Children.Remove(previewLine);
            }

            // Create new preview line
            previewLine = new System.Windows.Shapes.Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = current.X,
                Y2 = current.Y,
                Stroke = System.Windows.Media.Brushes.White,
                StrokeThickness = 1,
                StrokeDashArray = new System.Windows.Media.DoubleCollection { 5, 5 },
                Tag = "PreviewLine"
            };
            
            // Add marching ants animation
            if (linePreviewAnimation != null)
            {
                linePreviewAnimation.Stop();
            }
            linePreviewAnimation = CreateMarchingAntsAnimation(previewLine);
            linePreviewAnimation.Begin();
            
            // Ensure preview line is always on top (higher than text editor and final lines)
            Panel.SetZIndex(previewLine, 1002);
            
            MainCanvas.Children.Add(previewLine);
        }

        private void CreateRectangle(System.Windows.Point start, System.Windows.Point end)
        {
            var left = Math.Min(start.X, end.X);
            var top = Math.Min(start.Y, end.Y);
            var width = Math.Abs(end.X - start.X);
            var height = Math.Abs(end.Y - start.Y);
            
            var rectangle = new System.Windows.Shapes.Rectangle
            {
                Width = width,
                Height = height,
                Stroke = System.Windows.Media.Brushes.Red,
                StrokeThickness = 1,
                Fill = System.Windows.Media.Brushes.Transparent,
                Tag = "Rectangle"
            };
            
            // Set position using attached properties
            Canvas.SetLeft(rectangle, left);
            Canvas.SetTop(rectangle, top);
            
            // Ensure rectangle is always on top of text elements (higher than ZoomTextEditor's 1000)
            Panel.SetZIndex(rectangle, 1001);
            
            MainCanvas.Children.Add(rectangle);
            LogToFile($"Created rectangle from {start} to {end} (L:{left}, T:{top}, W:{width}, H:{height})");
        }

        private void UpdateRectanglePreview(System.Windows.Point start, System.Windows.Point current)
        {
            // Remove previous preview rectangle
            if (previewRectangle != null)
            {
                MainCanvas.Children.Remove(previewRectangle);
            }

            var left = Math.Min(start.X, current.X);
            var top = Math.Min(start.Y, current.Y);
            var width = Math.Abs(current.X - start.X);
            var height = Math.Abs(current.Y - start.Y);

            // Create new preview rectangle
            previewRectangle = new System.Windows.Shapes.Rectangle
            {
                Width = width,
                Height = height,
                Stroke = System.Windows.Media.Brushes.White,
                StrokeThickness = 1,
                StrokeDashArray = new System.Windows.Media.DoubleCollection { 5, 5 },
                Fill = System.Windows.Media.Brushes.Transparent,
                Tag = "PreviewRectangle"
            };
            
            // Add marching ants animation
            if (rectanglePreviewAnimation != null)
            {
                rectanglePreviewAnimation.Stop();
            }
            rectanglePreviewAnimation = CreateMarchingAntsAnimation(previewRectangle);
            rectanglePreviewAnimation.Begin();
            
            // Set position using attached properties
            Canvas.SetLeft(previewRectangle, left);
            Canvas.SetTop(previewRectangle, top);
            
            // Ensure preview rectangle is always on top (higher than text editor and final rectangles)
            Panel.SetZIndex(previewRectangle, 1002);
            
            MainCanvas.Children.Add(previewRectangle);
        }

        private void CreateHighlight(System.Windows.Point start, System.Windows.Point end)
        {
            var left = Math.Min(start.X, end.X);
            var top = Math.Min(start.Y, end.Y);
            var width = Math.Abs(end.X - start.X);
            var height = Math.Abs(end.Y - start.Y);
            
            var highlight = new System.Windows.Shapes.Rectangle
            {
                Width = width,
                Height = height,
                Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 255, 255, 0)), // More transparent yellow
                Stroke = null,
                Tag = "Highlight"
            };
            
            // Set position using attached properties
            Canvas.SetLeft(highlight, left);
            Canvas.SetTop(highlight, top);
            
            // Ensure highlight is always on top of text elements (higher than ZoomTextEditor's 1000)
            Panel.SetZIndex(highlight, 1001);
            
            MainCanvas.Children.Add(highlight);
            LogToFile($"Created highlight from {start} to {end} (L:{left}, T:{top}, W:{width}, H:{height})");
        }

        private void UpdateHighlightPreview(System.Windows.Point start, System.Windows.Point current)
        {
            // Remove previous preview highlight
            if (previewHighlight != null)
            {
                MainCanvas.Children.Remove(previewHighlight);
            }

            var left = Math.Min(start.X, current.X);
            var top = Math.Min(start.Y, current.Y);
            var width = Math.Abs(current.X - start.X);
            var height = Math.Abs(current.Y - start.Y);

            // Create new preview highlight
            previewHighlight = new System.Windows.Shapes.Rectangle
            {
                Width = width,
                Height = height,
                Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 255, 255, 0)), // Very transparent yellow for preview
                Stroke = System.Windows.Media.Brushes.White,
                StrokeThickness = 1,
                StrokeDashArray = new System.Windows.Media.DoubleCollection { 5, 5 },
                Tag = "PreviewHighlight"
            };
            
            // Add marching ants animation
            if (highlightPreviewAnimation != null)
            {
                highlightPreviewAnimation.Stop();
            }
            highlightPreviewAnimation = CreateMarchingAntsAnimation(previewHighlight);
            highlightPreviewAnimation.Begin();
            
            // Set position using attached properties
            Canvas.SetLeft(previewHighlight, left);
            Canvas.SetTop(previewHighlight, top);
            
            // Ensure preview highlight is always on top (higher than text editor and final highlights)
            Panel.SetZIndex(previewHighlight, 1002);
            
            MainCanvas.Children.Add(previewHighlight);
        }

        private void CreateBlur(System.Windows.Point start, System.Windows.Point end)
        {
            var left = Math.Min(start.X, end.X);
            var top = Math.Min(start.Y, end.Y);
            var width = Math.Abs(end.X - start.X);
            var height = Math.Abs(end.Y - start.Y);
            
            // Create an image element to display the pixelated content
            var pixelatedImage = new System.Windows.Controls.Image
            {
                Width = width,
                Height = height,
                Tag = "Pixelated"
            };
            
            // Capture the current visual content underneath the blur region
            try
            {
                // Temporarily hide the window to capture the current screen state
                this.Visibility = System.Windows.Visibility.Hidden;
                this.UpdateLayout();
                
                // Give the system time to hide the window
                System.Threading.Thread.Sleep(50);
                
                // Capture the current screen content for the region
                var regionBitmap = CaptureScreenRegion(new System.Drawing.Rectangle((int)left, (int)top, (int)width, (int)height));
                
                // Apply pixelation effect to the captured content
                var pixelatedBitmap = ApplyPixelation(regionBitmap, 8); // 8x8 pixel blocks
                pixelatedImage.Source = pixelatedBitmap;
            }
            catch (Exception ex)
            {
                LogToFile($"Error creating pixelation: {ex.Message}");
                // Fallback to semi-transparent overlay if capture fails
                var pixelatedRectangle = new System.Windows.Shapes.Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(150, 0, 0, 0)),
                    Stroke = System.Windows.Media.Brushes.Blue,
                    StrokeThickness = 1,
                    StrokeDashArray = new System.Windows.Media.DoubleCollection { 3, 3 },
                    Tag = "Pixelated"
                };
                
                Canvas.SetLeft(pixelatedRectangle, left);
                Canvas.SetTop(pixelatedRectangle, top);
                Panel.SetZIndex(pixelatedRectangle, 1001);
                MainCanvas.Children.Add(pixelatedRectangle);
                return;
            }
            finally
            {
                // Restore window visibility
                this.Visibility = System.Windows.Visibility.Visible;
                this.UpdateLayout();
            }
            
            // Set position using attached properties
            Canvas.SetLeft(pixelatedImage, left);
            Canvas.SetTop(pixelatedImage, top);
            
            // Ensure pixelated region is always on top of text elements (higher than ZoomTextEditor's 1000)
            Panel.SetZIndex(pixelatedImage, 1001);
            
            MainCanvas.Children.Add(pixelatedImage);
            LogToFile($"Created pixelation from {start} to {end} (L:{left}, T:{top}, W:{width}, H:{height})");
        }

        private void UpdateBlurPreview(System.Windows.Point start, System.Windows.Point current)
        {
            // Remove any existing preview to prevent flickering
            if (previewBlur != null)
            {
                MainCanvas.Children.Remove(previewBlur);
                previewBlur = null;
            }

            // No preview shown during pixelation selection to eliminate flickering
            // The pixelation will only appear when the user releases the mouse button
        }

        private void UpdatePixelationSelectionBorder(System.Windows.Point start, System.Windows.Point current)
        {
            // Remove previous selection border
            if (pixelationSelectionBorder != null)
            {
                MainCanvas.Children.Remove(pixelationSelectionBorder);
                pixelationSelectionBorder = null;
            }

            // Stop any existing animation
            if (pixelationBorderAnimation != null)
            {
                pixelationBorderAnimation.Stop();
                pixelationBorderAnimation = null;
            }

            // Calculate dimensions
            var left = Math.Min(start.X, current.X);
            var top = Math.Min(start.Y, current.Y);
            var width = Math.Abs(current.X - start.X);
            var height = Math.Abs(current.Y - start.Y);

            // Only show border if there's a meaningful size
            if (width > 5 && height > 5)
            {
                // Create animated selection border
                pixelationSelectionBorder = new System.Windows.Shapes.Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = System.Windows.Media.Brushes.Transparent,
                    Stroke = System.Windows.Media.Brushes.White,
                    StrokeThickness = 1,
                    Tag = "PixelationSelectionBorder"
                };

                Canvas.SetLeft(pixelationSelectionBorder, left);
                Canvas.SetTop(pixelationSelectionBorder, top);
                Panel.SetZIndex(pixelationSelectionBorder, 1002);

                // Create animated dashed border effect
                var dashArray = new System.Windows.Media.DoubleCollection { 8, 4 };
                pixelationSelectionBorder.StrokeDashArray = dashArray;

                // Create animation for moving dashes
                pixelationBorderAnimation = new System.Windows.Media.Animation.Storyboard();
                
                var dashOffsetAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = 12, // Total of dash + gap (8 + 4)
                    Duration = new System.Windows.Duration(System.TimeSpan.FromMilliseconds(800)),
                    RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
                };

                System.Windows.Media.Animation.Storyboard.SetTarget(dashOffsetAnimation, pixelationSelectionBorder);
                System.Windows.Media.Animation.Storyboard.SetTargetProperty(dashOffsetAnimation, new System.Windows.PropertyPath("(Shape.StrokeDashOffset)"));
                
                pixelationBorderAnimation.Children.Add(dashOffsetAnimation);
                pixelationBorderAnimation.Begin();

                MainCanvas.Children.Add(pixelationSelectionBorder);
            }
        }
        
        private System.Windows.Media.Animation.Storyboard CreateMarchingAntsAnimation(System.Windows.Shapes.Shape shape)
        {
            var storyboard = new System.Windows.Media.Animation.Storyboard();
            
            var dashOffsetAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 10, // Total of dash + gap (5 + 5)
                Duration = new System.Windows.Duration(System.TimeSpan.FromMilliseconds(500)),
                RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
            };

            System.Windows.Media.Animation.Storyboard.SetTarget(dashOffsetAnimation, shape);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(dashOffsetAnimation, new System.Windows.PropertyPath("(Shape.StrokeDashOffset)"));
            
            storyboard.Children.Add(dashOffsetAnimation);
            return storyboard;
        }

        private System.Windows.Shapes.Path CreateArrowPath(System.Windows.Point start, System.Windows.Point end)
        {
            var pathGeometry = new System.Windows.Media.PathGeometry();
            
            // Calculate arrowhead points
            var direction = new System.Windows.Vector(end.X - start.X, end.Y - start.Y);
            var length = direction.Length;
            
            if (length > 0)
            {
                direction.Normalize();
                
                var arrowHeadLength = Math.Min(15, length * 0.3); // 15px or 30% of line length, whichever is smaller
                var arrowHeadAngle = Math.PI / 6; // 30 degrees
                
                // Calculate perpendicular vector for even arrowhead
                var perpendicular = new System.Windows.Vector(-direction.Y, direction.X);
                
                var arrowHead1 = new System.Windows.Point(
                    end.X - direction.X * arrowHeadLength + perpendicular.X * arrowHeadLength * Math.Tan(arrowHeadAngle),
                    end.Y - direction.Y * arrowHeadLength + perpendicular.Y * arrowHeadLength * Math.Tan(arrowHeadAngle)
                );
                
                var arrowHead2 = new System.Windows.Point(
                    end.X - direction.X * arrowHeadLength - perpendicular.X * arrowHeadLength * Math.Tan(arrowHeadAngle),
                    end.Y - direction.Y * arrowHeadLength - perpendicular.Y * arrowHeadLength * Math.Tan(arrowHeadAngle)
                );
                
                // Create main line path
                var mainLineFigure = new System.Windows.Media.PathFigure();
                mainLineFigure.StartPoint = start;
                mainLineFigure.Segments.Add(new System.Windows.Media.LineSegment(end, true));
                pathGeometry.Figures.Add(mainLineFigure);
                
                // Create arrowhead path
                var arrowHeadFigure = new System.Windows.Media.PathFigure();
                arrowHeadFigure.StartPoint = end;
                arrowHeadFigure.Segments.Add(new System.Windows.Media.LineSegment(arrowHead1, true));
                arrowHeadFigure.Segments.Add(new System.Windows.Media.LineSegment(end, true));
                arrowHeadFigure.Segments.Add(new System.Windows.Media.LineSegment(arrowHead2, true));
                pathGeometry.Figures.Add(arrowHeadFigure);
            }
            
            return new System.Windows.Shapes.Path
            {
                Data = pathGeometry,
                Fill = null,
                StrokeThickness = 1
            };
        }

        private void UndoLastAction()
        {
            if (highlightAreas.Count > 0 && undoStack.Count > 0)
            {
                highlightAreas = undoStack.Pop();
                highlightShapes = shapeUndoStack.Pop();
                RefreshCanvas();
            }
        }

        private void CopySelectedRegion()
        {
            // Implementation for copying selected region to clipboard
        }

        private void SaveCurrentCapture()
        {
            // Implementation for saving current capture
        }

        private void RefreshCanvas()
        {
            // Clear and redraw all shapes on the canvas
            MainCanvas.Children.Clear();
            
            foreach (var shape in highlightShapes)
            {
                MainCanvas.Children.Add(shape);
            }
        }

         private void Window_MouseDown(object sender, MouseButtonEventArgs e)
         {
             if (e.LeftButton == MouseButtonState.Pressed)
             {
                 startPoint = e.GetPosition(this);
                 System.Diagnostics.Debug.WriteLine($"Mouse down at: {startPoint}");
                 LogToFile($"MOUSE DOWN: startPoint={startPoint}, isArrowMode={isArrowMode}, isSelectingRegion={isSelectingRegion}, MainCanvas.IsHitTestVisible={MainCanvas.IsHitTestVisible}");
                 
                 // PRIORITY: Check for active drawing tools first - these take precedence over existing elements
                if (isArrowMode)
                {
                    // Start arrow drawing - ignore any existing elements
                    arrowStartPoint = e.GetPosition(this);
                    isDrawingArrow = true;
                    LogToFile($"ARROW MOUSE DOWN: Started arrow drawing at: {arrowStartPoint}");
                    LogToFile($"ARROW MOUSE DOWN: isDrawingArrow = {isDrawingArrow}");
                    return;
                }
                 else if (isLineMode)
                 {
                     // Start line drawing - ignore any existing elements
                     lineStartPoint = e.GetPosition(this);
                     isDrawingLine = true;
                     LogToFile($"Started line drawing at: {lineStartPoint}");
                     return;
                 }
                 else if (isRectangleDrawingMode)
                 {
                     // Start rectangle drawing - ignore any existing elements
                     rectangleStartPoint = e.GetPosition(this);
                     isDrawingRectangle = true;
                     LogToFile($"Started rectangle drawing at: {rectangleStartPoint}");
                     return;
                 }
                 else if (isHighlightMode)
                 {
                     // Start highlight drawing - ignore any existing elements
                     highlightStartPoint = e.GetPosition(this);
                     isDrawingHighlight = true;
                     LogToFile($"Started highlight drawing at: {highlightStartPoint}");
                     return;
                 }
                 else if (isBlurMode)
                 {
                     // Start blur drawing - ignore any existing elements
                     blurStartPoint = e.GetPosition(this);
                     isDrawingBlur = true;
                     LogToFile($"Started blur drawing at: {blurStartPoint}");
                     return;
                 }
                 
                 // SECONDARY: Check for specific interactive elements (only when no drawing tools are active)
                 var clickedElement = e.OriginalSource as System.Windows.FrameworkElement;
                 
                 // Check if clicking on a speech balloon element
                 if (clickedElement != null && 
                     (clickedElement.Tag?.ToString() == "SpeechBalloon" || 
                      clickedElement.Tag?.ToString() == "SpeechBalloonText" ||
                      clickedElement.Tag?.ToString() == "SpeechBalloonDragArea" ||
                      clickedElement.Tag?.ToString() == "TailTip"))
                 {
                     // Don't start region selection when clicking on speech balloon elements
                     LogToFile($"MAIN WINDOW: Clicked on speech balloon element with tag: {clickedElement.Tag?.ToString()}");
                     
                     // Handle speech balloon dragging directly here
                     HandleSpeechBalloonDrag(e.GetPosition(this), clickedElement);
                     return;
                 }
                 
                 // FALLBACK: Region selection and other modes (when no drawing tools or special elements are active)
                 else if (isSpeechBalloonMode)
                 {
                     // Create speech balloon at click location
                     var clickPoint = e.GetPosition(this);
                     CreateSpeechBalloon(clickPoint);
                 }
                 else if (isDrawingMode)
                 {
                     StartDrawing();
                 }
                 else
                 {
                     // Start region selection (either continuing or starting new)
                     LogToFile($"MOUSE DOWN: Starting region selection, isSelectingRegion={isSelectingRegion}");
                     if (!isSelectingRegion)
                     {
                         isSelectingRegion = true;
                         this.Cursor = Cursors.Cross;
                         LogToFile($"MOUSE DOWN: Set isSelectingRegion=true");
                     }
                     StartRegionSelection();
                 }
             }
         }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                endPoint = e.GetPosition(this);
                System.Diagnostics.Debug.WriteLine($"Mouse up at: {endPoint}");
                LogToFile($"MOUSE UP: Mouse released at {endPoint}");
                
                // Stop speech balloon dragging
                if (draggedSpeechBalloon != null)
                {
                    LogToFile("Stopped dragging speech balloon");
                    draggedSpeechBalloon = null;
                    this.Cursor = Cursors.Arrow;
                    return;
                }
                
                // PRIORITY: Check for active drawing tools first - these take precedence over existing elements
                if (isDrawingArrow)
                {
                    // Finish arrow drawing
                    var arrowEndPoint = e.GetPosition(this);
                    LogToFile($"ARROW MOUSE UP: About to create arrow from {arrowStartPoint} to {arrowEndPoint}");
                    LogToFile($"ARROW MOUSE UP: isDrawingArrow = {isDrawingArrow}");
                    
                    CreateArrow(arrowStartPoint, arrowEndPoint);
                    LogToFile($"ARROW MOUSE UP: CreateArrow method completed");
                    
                    isDrawingArrow = false;
                    LogToFile($"ARROW MOUSE UP: isDrawingArrow set to {isDrawingArrow}");
                    
                    // Clean up preview arrow
                    if (previewArrow != null)
                    {
                        MainCanvas.Children.Remove(previewArrow);
                        previewArrow = null;
                        LogToFile($"ARROW MOUSE UP: Preview arrow cleaned up");
                    }
                    
                    LogToFile($"ARROW MOUSE UP: Finished arrow drawing from {arrowStartPoint} to {arrowEndPoint}");
                }
                else if (isDrawingLine)
                {
                    // Finish line drawing
                    var lineEndPoint = e.GetPosition(this);
                    CreateLine(lineStartPoint, lineEndPoint);
                    isDrawingLine = false;
                    
                    // Clean up preview line
                    if (previewLine != null)
                    {
                        MainCanvas.Children.Remove(previewLine);
                        previewLine = null;
                    }
                    
                    LogToFile($"Finished line drawing from {lineStartPoint} to {lineEndPoint}");
                }
                else if (isDrawingRectangle)
                {
                    // Finish rectangle drawing
                    var rectangleEndPoint = e.GetPosition(this);
                    CreateRectangle(rectangleStartPoint, rectangleEndPoint);
                    isDrawingRectangle = false;
                    
                    // Clean up preview rectangle
                    if (previewRectangle != null)
                    {
                        MainCanvas.Children.Remove(previewRectangle);
                        previewRectangle = null;
                    }
                    
                    LogToFile($"Finished rectangle drawing from {rectangleStartPoint} to {rectangleEndPoint}");
                }
                else if (isDrawingHighlight)
                {
                    // Finish highlight drawing
                    var highlightEndPoint = e.GetPosition(this);
                    CreateHighlight(highlightStartPoint, highlightEndPoint);
                    isDrawingHighlight = false;
                    
                    // Clean up preview highlight
                    if (previewHighlight != null)
                    {
                        MainCanvas.Children.Remove(previewHighlight);
                        previewHighlight = null;
                    }
                    
                    LogToFile($"Finished highlight drawing from {highlightStartPoint} to {highlightEndPoint}");
                }
                else if (isDrawingBlur)
                {
                    // Finish blur drawing
                    var blurEndPoint = e.GetPosition(this);
                    CreateBlur(blurStartPoint, blurEndPoint);
                    isDrawingBlur = false;
                    
                    // Clean up preview blur
                    if (previewBlur != null)
                    {
                        MainCanvas.Children.Remove(previewBlur);
                        previewBlur = null;
                    }
                    
                    // Clean up selection border
                    if (pixelationSelectionBorder != null)
                    {
                        MainCanvas.Children.Remove(pixelationSelectionBorder);
                        pixelationSelectionBorder = null;
                    }
                    
                    // Stop selection border animation
                    if (pixelationBorderAnimation != null)
                    {
                        pixelationBorderAnimation.Stop();
                        pixelationBorderAnimation = null;
                    }
                    
                    LogToFile($"Finished blur drawing from {blurStartPoint} to {blurEndPoint}");
                }
                
                // SECONDARY: Check for speech balloon elements (only when no drawing tools are active)
                var releasedElement = e.OriginalSource as System.Windows.FrameworkElement;
                if (releasedElement != null && 
                    (releasedElement.Tag?.ToString() == "SpeechBalloon" || 
                     releasedElement.Tag?.ToString() == "SpeechBalloonText" ||
                     releasedElement.Tag?.ToString() == "SpeechBalloonDragArea" ||
                     releasedElement.Tag?.ToString() == "TailTip"))
                {
                    // Don't finish region selection when releasing on speech balloon elements
                    return;
                }
                
                // FALLBACK: Other modes
                else if (isDrawing)
                {
                    FinishDrawing();
                }
                else if (isSelectingRegion)
                {
                    FinishRegionSelection();
                }
            }
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                endPoint = e.GetPosition(this);
                
                // Handle dragging (speech balloon or test rectangle)
                if (draggedSpeechBalloon != null)
                {
                    var currentPosition = e.GetPosition(this);
                    var deltaX = currentPosition.X - lastDragPosition.X;
                    var deltaY = currentPosition.Y - lastDragPosition.Y;

                    LogToFile($"Dragging element: deltaX={deltaX}, deltaY={deltaY}");

                    // Handle speech balloon dragging
                    // Update balloon position
                    Canvas.SetLeft(draggedSpeechBalloon.BalloonShape, Canvas.GetLeft(draggedSpeechBalloon.BalloonShape) + deltaX);
                    Canvas.SetTop(draggedSpeechBalloon.BalloonShape, Canvas.GetTop(draggedSpeechBalloon.BalloonShape) + deltaY);

                    // Update text position
                    Canvas.SetLeft(draggedSpeechBalloon.TextBlock, Canvas.GetLeft(draggedSpeechBalloon.TextBlock) + deltaX);
                    Canvas.SetTop(draggedSpeechBalloon.TextBlock, Canvas.GetTop(draggedSpeechBalloon.TextBlock) + deltaY);

                    // Update drag area position
                    if (draggedSpeechBalloon.DragArea != null)
                    {
                        Canvas.SetLeft(draggedSpeechBalloon.DragArea, Canvas.GetLeft(draggedSpeechBalloon.DragArea) + deltaX);
                        Canvas.SetTop(draggedSpeechBalloon.DragArea, Canvas.GetTop(draggedSpeechBalloon.DragArea) + deltaY);
                    }

                    // Update tail tip position
                    if (draggedSpeechBalloon.TailTip != null)
                    {
                        Canvas.SetLeft(draggedSpeechBalloon.TailTip, Canvas.GetLeft(draggedSpeechBalloon.TailTip) + deltaX);
                        Canvas.SetTop(draggedSpeechBalloon.TailTip, Canvas.GetTop(draggedSpeechBalloon.TailTip) + deltaY);
                    }

                    // Update speech balloon location
                    draggedSpeechBalloon.Location = new System.Windows.Point(
                        Canvas.GetLeft(draggedSpeechBalloon.BalloonShape),
                        Canvas.GetTop(draggedSpeechBalloon.BalloonShape)
                    );

                    lastDragPosition = currentPosition;
                    return;
                }
                
                // Check if we're dragging a speech balloon element
                var sourceElement = e.OriginalSource as System.Windows.FrameworkElement;
                if (sourceElement != null && 
                    (sourceElement.Tag?.ToString() == "SpeechBalloon" || 
                     sourceElement.Tag?.ToString() == "SpeechBalloonText" ||
                     sourceElement.Tag?.ToString() == "SpeechBalloonDragArea" ||
                     sourceElement.Tag?.ToString() == "TailTip"))
                {
                    // Don't update region selection when dragging speech balloon elements
                    return;
                }
                
                
                if (isDrawingArrow)
                {
                    var currentPos = e.GetPosition(this);
                    UpdateArrowPreview(arrowStartPoint, currentPos);
                    LogToFile($"ARROW MOUSE MOVE: Updating arrow preview from {arrowStartPoint} to {currentPos}");
                }
                else if (isDrawingLine)
                {
                    UpdateLinePreview(lineStartPoint, e.GetPosition(this));
                }
                else if (isDrawingRectangle)
                {
                    UpdateRectanglePreview(rectangleStartPoint, e.GetPosition(this));
                }
                else if (isDrawingHighlight)
                {
                    UpdateHighlightPreview(highlightStartPoint, e.GetPosition(this));
                }
                else if (isDrawingBlur)
                {
                    UpdatePixelationSelectionBorder(blurStartPoint, e.GetPosition(this));
                }
                else if (isDrawing)
                {
                    UpdateDrawing();
                }
                else if (isSelectingRegion)
                {
                    UpdateRegionSelection();
                }
            }
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (showTextBox && isEditingText)
            {
                // Handle text editing mouse wheel events if needed
                return;
            }
            
            // Handle zoom functionality for the last selected region
            if (highlightAreas.Count > 0)
            {
                var lastRegion = highlightAreas[highlightAreas.Count - 1];
                
                // Zoom in/out based on mouse wheel direction
                if (e.Delta > 0) // Zoom in
                {
                    zoomFactor *= 1.1;
                }
                else // Zoom out
                {
                    zoomFactor /= 1.1;
                }
                
                // Limit zoom factor (allow zoom out to 0.1x, zoom in to 5.0x)
                zoomFactor = Math.Max(0.1, Math.Min(zoomFactor, 5.0));
                
                // Apply zoom to the last region
                ApplyZoomToRegion(lastRegion, zoomFactor);
            }
        }
        
        private void ApplyZoomToRegion(System.Drawing.Rectangle region, double zoom)
        {
            if (zoom <= 0.1)
            {
                // If zoom is too small (0.1x or less), don't go smaller
                zoom = 0.1;
            }
            
            // Enter zoom mode
            isZoomed = true;
            zoomedRegion = region;
            
            // Calculate screen dimensions
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            
            // Calculate the zoomed region dimensions
            var zoomedWidth = region.Width * zoom;
            var zoomedHeight = region.Height * zoom;
            
            // Center the zoomed region on screen (10% higher from center)
            var centerX = (screenWidth - zoomedWidth) / 2;
            var centerY = (screenHeight - zoomedHeight) / 2 - (screenHeight * 0.1);
            
            // Clear all existing visual elements
            MainCanvas.Children.Clear();
            highlightShapes.Clear();
            regionVisualElements.Clear();
            regionDarkOverlays.Clear();
            
            // Hide the original overlay background
            OverlayBackground.Visibility = Visibility.Collapsed;
            
            // Create a black background that covers the entire screen
            var blackBackground = new System.Windows.Shapes.Rectangle
            {
                Width = screenWidth,
                Height = screenHeight,
                Fill = System.Windows.Media.Brushes.Black,
                Tag = "ZoomBackground"
            };
            MainCanvas.Children.Add(blackBackground);
            
            // Scale the cached screen content for the region
            var scaledContent = ScaleRegionContent(zoom);
            LogToFile($"scaledContent created: {scaledContent != null}");
            if (scaledContent != null)
            {
                LogToFile($"Adding scaled content image: {scaledContent.Width}x{scaledContent.Height} at ({centerX}, {centerY})");
                
                // Create an Image control to display the scaled content
                var contentImage = new System.Windows.Controls.Image
                {
                    Width = zoomedWidth,
                    Height = zoomedHeight,
                    Source = scaledContent,
                    Tag = "ZoomedContent"
                };
                Canvas.SetLeft(contentImage, centerX);
                Canvas.SetTop(contentImage, centerY);
                MainCanvas.Children.Add(contentImage);
                LogToFile("Scaled content image added to canvas");
            }
            else
            {
                LogToFile("scaledContent is null - cannot create zoomed image");
            }
            
            // Create the zoomed region border
            var zoomedRegionRect = new System.Windows.Shapes.Rectangle
            {
                Width = zoomedWidth,
                Height = zoomedHeight,
                Fill = System.Windows.Media.Brushes.Transparent,
                Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 68)), // #00ff44
                StrokeThickness = 1,
                Tag = "ZoomedRegion"
            };
            Canvas.SetLeft(zoomedRegionRect, centerX);
            Canvas.SetTop(zoomedRegionRect, centerY);
            MainCanvas.Children.Add(zoomedRegionRect);
            
            // Add corner squares to the zoomed region
            AddCornerSquares(centerX, centerY, zoomedWidth, zoomedHeight, "ZoomedRegion");
            
            // Reposition zoom text editor if it's visible
            if (isZoomTextEditorVisible)
            {
                UpdateZoomTextEditorPosition(centerX, centerY, zoomedWidth, zoomedHeight);
            }
            
            LogToFile($"Applied zoom {zoom}x to region at ({centerX}, {centerY}) with size {zoomedWidth}x{zoomedHeight}");
        }
        
        private System.Windows.Media.Imaging.BitmapSource CaptureFullScreen()
        {
            try
            {
                var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
                var screenHeight = (int)SystemParameters.PrimaryScreenHeight;
                
                LogToFile($"Capturing full screen: {screenWidth}x{screenHeight}");
                
                // Capture the full screen content
                using (var bitmap = new System.Drawing.Bitmap(screenWidth, screenHeight))
                {
                    using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        // Copy the current screen content to the bitmap
                        graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(screenWidth, screenHeight));
                    }
                    
                    // Convert System.Drawing.Bitmap to WPF BitmapSource
                    var bitmapData = bitmap.LockBits(
                        new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        System.Drawing.Imaging.ImageLockMode.ReadOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    
                    try
                    {
                        var bitmapSource = System.Windows.Media.Imaging.BitmapSource.Create(
                            bitmap.Width, bitmap.Height, 96, 96,
                            System.Windows.Media.PixelFormats.Bgra32,
                            null,
                            bitmapData.Scan0,
                            bitmapData.Stride * bitmapData.Height,
                            bitmapData.Stride);
                        
                        LogToFile($"BitmapSource created successfully: {bitmapSource.Width}x{bitmapSource.Height}");
                        return bitmapSource;
                    }
                    finally
                    {
                        bitmap.UnlockBits(bitmapData);
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error capturing full screen: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error capturing full screen: {ex.Message}");
                return null;
            }
        }
        
        private System.Windows.Media.Imaging.BitmapSource CaptureRegionContent(System.Drawing.Rectangle region)
        {
            if (fullScreenContent == null) 
            {
                LogToFile("CaptureRegionContent: fullScreenContent is null");
                return null;
            }
            
            try
            {
                LogToFile($"CaptureRegionContent: Cropping region {region} from fullScreenContent {fullScreenContent.Width}x{fullScreenContent.Height}");
                
                // Ensure region is within bounds
                var clampedRegion = new System.Windows.Int32Rect(
                    Math.Max(0, region.X),
                    Math.Max(0, region.Y),
                    Math.Min(region.Width, (int)fullScreenContent.Width - Math.Max(0, region.X)),
                    Math.Min(region.Height, (int)fullScreenContent.Height - Math.Max(0, region.Y))
                );
                
                LogToFile($"CaptureRegionContent: Clamped region to {clampedRegion}");
                
                // Crop the region from the full screen content
                var croppedBitmap = new System.Windows.Media.Imaging.CroppedBitmap(
                    fullScreenContent,
                    clampedRegion);
                
                LogToFile($"CaptureRegionContent: Successfully cropped to {croppedBitmap.Width}x{croppedBitmap.Height}");
                return croppedBitmap;
            }
            catch (Exception ex)
            {
                LogToFile($"Error cropping region: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error cropping region: {ex.Message}");
                return null;
            }
        }
        
        private System.Windows.Media.Imaging.BitmapSource ScaleRegionContent(double zoom)
        {
            if (cachedScreenContent == null) 
            {
                LogToFile("ScaleRegionContent: cachedScreenContent is null");
                return null;
            }
            
            try
            {
                LogToFile($"ScaleRegionContent: Scaling {cachedScreenContent.Width}x{cachedScreenContent.Height} by {zoom}x");
                
                // Scale the cached bitmap source based on zoom factor
                var scaledBitmap = new System.Windows.Media.Imaging.TransformedBitmap(
                    cachedScreenContent,
                    new System.Windows.Media.ScaleTransform(zoom, zoom));
                
                LogToFile($"ScaleRegionContent: Successfully scaled to {scaledBitmap.Width}x{scaledBitmap.Height}");
                return scaledBitmap;
            }
            catch (Exception ex)
            {
                LogToFile($"Error scaling region content: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error scaling region: {ex.Message}");
                return null;
            }
        }

        private System.Windows.Media.Imaging.BitmapSource CaptureScreenRegion(System.Drawing.Rectangle region)
        {
            try
            {
                // Create a bitmap for the region
                using (var bitmap = new System.Drawing.Bitmap(region.Width, region.Height))
                {
                    using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        // Copy the screen region to the bitmap
                        graphics.CopyFromScreen(region.X, region.Y, 0, 0, new System.Drawing.Size(region.Width, region.Height));
                    }
                    
                    // Convert to WPF BitmapSource
                    return ConvertBitmapToBitmapSource(bitmap);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error capturing screen region: {ex.Message}");
                return null;
            }
        }

        private System.Windows.Media.Imaging.BitmapSource ApplyPixelation(System.Windows.Media.Imaging.BitmapSource source, int pixelSize)
        {
            var width = source.PixelWidth;
            var height = source.PixelHeight;
            
            // Create a temporary bitmap for the scaled down version
            var tempBitmap = new System.Drawing.Bitmap(width / pixelSize, height / pixelSize);
            var pixelatedBitmap = new System.Drawing.Bitmap(width, height);
            
            // First pass: scale down the image
            using (var tempGraphics = System.Drawing.Graphics.FromImage(tempBitmap))
            {
                tempGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                tempGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                
                // Draw the source image scaled down
                tempGraphics.DrawImage(ConvertBitmapSourceToBitmap(source), 0, 0, width / pixelSize, height / pixelSize);
            }
            
            // Second pass: scale back up to original size
            using (var finalGraphics = System.Drawing.Graphics.FromImage(pixelatedBitmap))
            {
                finalGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                finalGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                
                // Draw the scaled down image back to original size
                finalGraphics.DrawImage(tempBitmap, 0, 0, width, height);
            }
            
            return ConvertBitmapToBitmapSource(pixelatedBitmap);
        }

        private System.Drawing.Bitmap ConvertBitmapSourceToBitmap(System.Windows.Media.Imaging.BitmapSource source)
        {
            var width = source.PixelWidth;
            var height = source.PixelHeight;
            var stride = width * ((source.Format.BitsPerPixel + 7) / 8);
            var pixelBuffer = new byte[stride * height];
            
            source.CopyPixels(pixelBuffer, stride, 0);
            
            var bitmap = new System.Drawing.Bitmap(width, height);
            var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height), 
                System.Drawing.Imaging.ImageLockMode.WriteOnly, 
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            
            System.Runtime.InteropServices.Marshal.Copy(pixelBuffer, 0, bitmapData.Scan0, pixelBuffer.Length);
            bitmap.UnlockBits(bitmapData);
            
            return bitmap;
        }

        private System.Windows.Media.Imaging.BitmapSource ConvertBitmapToBitmapSource(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            
            try
            {
                var bitmapSource = System.Windows.Media.Imaging.BitmapSource.Create(
                    bitmap.Width, bitmap.Height, 96, 96,
                    System.Windows.Media.PixelFormats.Bgra32,
                    null,
                    bitmapData.Scan0,
                    bitmapData.Stride * bitmapData.Height,
                    bitmapData.Stride);
                
                return bitmapSource;
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }
        
        private void ExitZoomMode()
        {
            if (!isZoomed) return;
            
            isZoomed = false;
            zoomFactor = 1.0;
            
            // Hide zoom text editor
            HideZoomTextEditor();
            isZoomTextEditorVisible = false;
            
            // Clear all visual elements
            MainCanvas.Children.Clear();
            highlightShapes.Clear();
            regionVisualElements.Clear();
            regionDarkOverlays.Clear();
            
            // Restore the original overlay background
            OverlayBackground.Visibility = Visibility.Visible;
            
            // Recreate all highlights
            if (highlightAreas.Count > 0)
            {
                foreach (var area in highlightAreas)
                {
                    CreateHighlightShape(area);
                }
            }
            
            System.Diagnostics.Debug.WriteLine("Exited zoom mode and restored normal view");
        }

        private void StartDrawing()
        {
            isDrawing = true;
            // Save current state for undo
             undoStack.Push(new List<System.Drawing.Rectangle>(highlightAreas));
             shapeUndoStack.Push(new List<System.Windows.Shapes.Shape>(highlightShapes));
        }

        private void UpdateDrawing()
        {
            if (!isDrawing) return;
            
            // Update drawing preview based on current mode
            // This would update a temporary shape being drawn
        }

        private void FinishDrawing()
        {
            if (!isDrawing) return;
            
            isDrawing = false;
            
            // Create final shape based on drawing mode
            CreateFinalShape();
        }

        private void CreateFinalShape()
        {
            // Create the appropriate shape based on current mode
            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = Math.Abs(endPoint.X - startPoint.X),
                Height = Math.Abs(endPoint.Y - startPoint.Y),
                Stroke = System.Windows.Media.Brushes.Red,
                StrokeThickness = 1,
                Fill = System.Windows.Media.Brushes.Transparent
            };
            
            Canvas.SetLeft(rect, Math.Min(startPoint.X, endPoint.X));
            Canvas.SetTop(rect, Math.Min(startPoint.Y, endPoint.Y));
            
            MainCanvas.Children.Add(rect);
            highlightShapes.Add(rect);
            
            // Add to highlight areas
            System.Drawing.Rectangle area = new System.Drawing.Rectangle(
                (int)Math.Min(startPoint.X, endPoint.X),
                (int)Math.Min(startPoint.Y, endPoint.Y),
                (int)Math.Abs(endPoint.X - startPoint.X),
                (int)Math.Abs(endPoint.Y - startPoint.Y)
            );
            highlightAreas.Add(area);
        }

        private void StartRegionSelection()
        {
            System.Diagnostics.Debug.WriteLine("Starting region selection");
        }

         private void UpdateRegionSelection()
         {
             if (!isSelectingRegion) return;
             
             // Remove all temporary preview elements (rectangles and corner squares)
             var existingPreviews = MainCanvas.Children.OfType<System.Windows.Shapes.Rectangle>()
                 .Where(r => r.Tag?.ToString() == "Preview").ToList();
             foreach (var preview in existingPreviews)
             {
                 MainCanvas.Children.Remove(preview);
             }
             
             // Remove all temporary corner squares
             var existingCorners = MainCanvas.Children.OfType<System.Windows.Shapes.Rectangle>()
                 .Where(r => r.Tag?.ToString() == "Corner").ToList();
             foreach (var corner in existingCorners)
             {
                 MainCanvas.Children.Remove(corner);
             }
             
             double left = Math.Min(startPoint.X, endPoint.X);
             double top = Math.Min(startPoint.Y, endPoint.Y);
             double width = Math.Abs(endPoint.X - startPoint.X);
             double height = Math.Abs(endPoint.Y - startPoint.Y);
             
             // Only create preview if the selection is large enough
             if (width > 5 && height > 5)
             {
                 // Create selection rectangle preview with green border
                 var selectionRect = new System.Windows.Shapes.Rectangle
                 {
                     Width = width,
                     Height = height,
                     Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 68)), // #00ff44
                     StrokeThickness = 1,
                     Fill = System.Windows.Media.Brushes.Transparent,
                     Tag = "Preview" // Mark as preview for easy removal
                 };
                 
                 Canvas.SetLeft(selectionRect, left);
                 Canvas.SetTop(selectionRect, top);
                 
                 MainCanvas.Children.Add(selectionRect);
                 
                // Add corner squares with preview tag
                var previewCorners = AddCornerSquares(left, top, width, height, "Preview");
             }
         }
        
         private List<System.Windows.Shapes.Shape> AddCornerSquares(double left, double top, double width, double height, string tag = "Final")
         {
             const double cornerSize = 8;
             var cornerBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 68)); // #00ff44
             var cornerElements = new List<System.Windows.Shapes.Shape>();
             
             // Top-left corner
             var topLeft = new System.Windows.Shapes.Rectangle
             {
                 Width = cornerSize,
                 Height = cornerSize,
                 Fill = cornerBrush,
                 Tag = tag == "Preview" ? "Corner" : tag // Use "Corner" tag for preview corners to match cleanup logic
             };
             Canvas.SetLeft(topLeft, left - cornerSize / 2);
             Canvas.SetTop(topLeft, top - cornerSize / 2);
             MainCanvas.Children.Add(topLeft);
             cornerElements.Add(topLeft);
             
             // Top-right corner
             var topRight = new System.Windows.Shapes.Rectangle
             {
                 Width = cornerSize,
                 Height = cornerSize,
                 Fill = cornerBrush,
                 Tag = tag == "Preview" ? "Corner" : tag
             };
             Canvas.SetLeft(topRight, left + width - cornerSize / 2);
             Canvas.SetTop(topRight, top - cornerSize / 2);
             MainCanvas.Children.Add(topRight);
             cornerElements.Add(topRight);
             
             // Bottom-left corner
             var bottomLeft = new System.Windows.Shapes.Rectangle
             {
                 Width = cornerSize,
                 Height = cornerSize,
                 Fill = cornerBrush,
                 Tag = tag == "Preview" ? "Corner" : tag
             };
             Canvas.SetLeft(bottomLeft, left - cornerSize / 2);
             Canvas.SetTop(bottomLeft, top + height - cornerSize / 2);
             MainCanvas.Children.Add(bottomLeft);
             cornerElements.Add(bottomLeft);
             
             // Bottom-right corner
             var bottomRight = new System.Windows.Shapes.Rectangle
             {
                 Width = cornerSize,
                 Height = cornerSize,
                 Fill = cornerBrush,
                 Tag = tag == "Preview" ? "Corner" : tag
             };
             Canvas.SetLeft(bottomRight, left + width - cornerSize / 2);
             Canvas.SetTop(bottomRight, top + height - cornerSize / 2);
             MainCanvas.Children.Add(bottomRight);
             cornerElements.Add(bottomRight);
             
             return cornerElements;
         }

         private void FinishRegionSelection()
         {
             if (!isSelectingRegion) return;
             
             System.Diagnostics.Debug.WriteLine($"Finishing region selection: {startPoint} to {endPoint}");
             
             // Create region
             System.Drawing.Rectangle region = new System.Drawing.Rectangle(
                 (int)Math.Min(startPoint.X, endPoint.X),
                 (int)Math.Min(startPoint.Y, endPoint.Y),
                 (int)Math.Abs(endPoint.X - startPoint.X),
                 (int)Math.Abs(endPoint.Y - startPoint.Y)
             );
             
             if (region.Width > 10 && region.Height > 10)
             {
                 System.Diagnostics.Debug.WriteLine($"Region created: {region}");
                 LogToFile($"Region created: {region}");
                 
                 // Capture screen content BEFORE adding to highlightAreas or creating visual elements
                 cachedScreenContent = CaptureRegionContent(region);
                 LogToFile($"cachedScreenContent captured: {cachedScreenContent != null}");
                 
                 highlightAreas.Add(region);
                 CreateHighlightShape(region);
                 
                 // Keep region selection mode active for multiple selections
                 // User can continue clicking to make more selections
                 
                 // Show text editor for this region (optional - you might want to remove this for multiple selections)
                 // ShowTextBox();
             }
             else
             {
                 System.Diagnostics.Debug.WriteLine("Region too small, ignoring");
             }
         }

         private void CreateHighlightShape(System.Drawing.Rectangle region)
         {
             // Create a list to track visual elements for this region
             var regionElements = new List<System.Windows.Shapes.Shape>();
             
             // Create the main highlight rectangle
             var rect = new System.Windows.Shapes.Rectangle
             {
                 Width = region.Width,
                 Height = region.Height,
                 Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 68)), // #00ff44
                 StrokeThickness = 1,
                 Fill = System.Windows.Media.Brushes.Transparent,
                 Tag = "Final"
             };
             
             Canvas.SetLeft(rect, region.X);
             Canvas.SetTop(rect, region.Y);
             
             MainCanvas.Children.Add(rect);
             highlightShapes.Add(rect);
             regionElements.Add(rect);
             
             // Add corner squares to the highlight with "Final" tag
             var cornerElements = AddCornerSquares(region.X, region.Y, region.Width, region.Height, "Final");
             regionElements.AddRange(cornerElements);
             
             // Store the visual elements for this region
             regionVisualElements.Add(regionElements);
             
             // Create transparent cut-out for the selected region
             CreateTransparentCutout(region);
         }
         
        private void CreateTransparentCutout(System.Drawing.Rectangle region)
        {
            // Remove ALL existing overlay elements
            var existingOverlays = highlightShapes.Where(s => s.Tag?.ToString() == "DarkOverlay").ToList();
            foreach (var overlay in existingOverlays)
            {
                MainCanvas.Children.Remove(overlay);
                highlightShapes.Remove(overlay);
            }
            
            // Hide the original overlay background
            OverlayBackground.Visibility = Visibility.Collapsed;
            
            System.Diagnostics.Debug.WriteLine($"Creating individual dark rectangles avoiding {highlightAreas.Count} regions");
            
            // Create a list of rectangles that need to be darkened
            var darkRectangles = new List<System.Drawing.Rectangle>();
            
            // Start with the full screen
            var fullScreen = new System.Drawing.Rectangle(0, 0, (int)this.Width, (int)this.Height);
            darkRectangles.Add(fullScreen);
            
            // For each selected region, subtract it from the dark rectangles
            foreach (var selectedArea in highlightAreas)
            {
                System.Diagnostics.Debug.WriteLine($"Subtracting selected area: {selectedArea}");
                
                var newDarkRectangles = new List<System.Drawing.Rectangle>();
                
                foreach (var darkRect in darkRectangles)
                {
                    // If the dark rectangle doesn't intersect with the selected area, keep it
                    if (!darkRect.IntersectsWith(selectedArea))
                    {
                        newDarkRectangles.Add(darkRect);
                    }
                    else
                    {
                        // Split the dark rectangle around the selected area
                        var splits = SplitRectangle(darkRect, selectedArea);
                        newDarkRectangles.AddRange(splits);
                    }
                }
                
                darkRectangles = newDarkRectangles;
            }
            
            // Create visual rectangles for each dark area
            foreach (var darkRect in darkRectangles)
            {
                System.Diagnostics.Debug.WriteLine($"Creating dark rectangle: {darkRect}");
                
                var visualRect = new System.Windows.Shapes.Rectangle
                {
                    Width = darkRect.Width,
                    Height = darkRect.Height,
                    Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(153, 0, 0, 0)),
                    Stroke = System.Windows.Media.Brushes.Transparent,
                    StrokeThickness = 0,
                    Tag = "DarkOverlay"
                };
                
                Canvas.SetLeft(visualRect, darkRect.X);
                Canvas.SetTop(visualRect, darkRect.Y);
                MainCanvas.Children.Add(visualRect);
                highlightShapes.Add(visualRect);
            }
            
            System.Diagnostics.Debug.WriteLine($"Created {darkRectangles.Count} dark rectangles");
        }
        
        private List<System.Drawing.Rectangle> SplitRectangle(System.Drawing.Rectangle original, System.Drawing.Rectangle hole)
        {
            var result = new List<System.Drawing.Rectangle>();
            
            // Left piece
            if (original.X < hole.X)
            {
                result.Add(new System.Drawing.Rectangle(original.X, original.Y, hole.X - original.X, original.Height));
            }
            
            // Right piece
            if (original.Right > hole.Right)
            {
                result.Add(new System.Drawing.Rectangle(hole.Right, original.Y, original.Right - hole.Right, original.Height));
            }
            
            // Top piece
            if (original.Y < hole.Y)
            {
                int left = Math.Max(original.X, hole.X);
                int right = Math.Min(original.Right, hole.Right);
                if (left < right)
                {
                    result.Add(new System.Drawing.Rectangle(left, original.Y, right - left, hole.Y - original.Y));
                }
            }
            
            // Bottom piece
            if (original.Bottom > hole.Bottom)
            {
                int left = Math.Max(original.X, hole.X);
                int right = Math.Min(original.Right, hole.Right);
                if (left < right)
                {
                    result.Add(new System.Drawing.Rectangle(left, hole.Bottom, right - left, original.Bottom - hole.Bottom));
                }
            }
            
            return result;
        }

        private void UndoLastSelection()
        {
            if (highlightAreas.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Undoing last selection. Had {highlightAreas.Count} regions");
                
                // Remove the last selected region data
                highlightAreas.RemoveAt(highlightAreas.Count - 1);
                
                System.Diagnostics.Debug.WriteLine($"Now have {highlightAreas.Count} regions");
                
                // Clear all existing visual elements
                MainCanvas.Children.Clear();
                highlightShapes.Clear();
                regionVisualElements.Clear();
                regionDarkOverlays.Clear();
                
                // Restore the original overlay background
                OverlayBackground.Visibility = Visibility.Visible;
                
                // Recreate highlights for all remaining regions
                if (highlightAreas.Count > 0)
                {
                    foreach (var area in highlightAreas)
                    {
                        CreateHighlightShape(area);
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("Undo completed - recreated overlay with remaining regions");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No regions to undo");
            }
        }

        private void TextEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                HideTextBox();
            }
            else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                HideTextBox();
            }
            else if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                HandlePasteInTextBox();
            }
        }

        private void ZoomTextEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                HideZoomTextEditor();
                isZoomTextEditorVisible = false;
            }
            else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                HideZoomTextEditor();
                isZoomTextEditorVisible = false;
            }
            else if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                HandlePasteInZoomTextEditor();
            }
        }

        private void HandlePasteInTextBox()
        {
            if (System.Windows.Clipboard.ContainsImage())
            {
                // Handle image paste
                var bitmap = System.Windows.Clipboard.GetImage();
                if (bitmap != null)
                {
                    // Convert WPF BitmapSource to System.Drawing.Image
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    
                    using (var stream = new MemoryStream())
                    {
                        encoder.Save(stream);
                        var image = System.Drawing.Image.FromStream(stream);
                        clipboardImages.Add(image);
                        
                        // Add image marker to text
                        var selectionStart = TextEditor.Selection.Start;
                        TextEditor.Selection.Text = "[IMG]";
                        
                        // Store image position
                        imagePositions.Add(new System.Drawing.Point(selectionStart.GetOffsetToPosition(TextEditor.Document.ContentStart), 0));
                    }
                }
            }
            else if (System.Windows.Clipboard.ContainsText())
            {
                // Handle text paste - RichTextBox handles this automatically
            }
        }

        private void HandlePasteInZoomTextEditor()
        {
            // Get clipboard data to preserve formatting
            var clipboardData = Clipboard.GetDataObject();
            
            if (clipboardData != null)
            {
                // Check if clipboard contains RTF (rich text format)
                if (clipboardData.GetDataPresent(DataFormats.Rtf))
                {
                    var rtfData = clipboardData.GetData(DataFormats.Rtf) as string;
                    if (!string.IsNullOrEmpty(rtfData))
                    {
                        // Insert RTF data to preserve formatting
                        var selection = ZoomTextEditor.Selection;
                        selection.Text = "";
                        
                        // Create a new paragraph with the RTF content
                        var paragraph = new System.Windows.Documents.Paragraph();
                        paragraph.TextAlignment = System.Windows.TextAlignment.Center;
                        
                        // Parse and insert RTF content
                        var range = new System.Windows.Documents.TextRange(paragraph.ContentStart, paragraph.ContentEnd);
                        range.Load(StreamFromString(rtfData), DataFormats.Rtf);
                        
                        // Insert the paragraph
                        var currentBlock = ZoomTextEditor.Document.Blocks.FirstOrDefault(b => b.ContentStart.CompareTo(selection.Start) <= 0 && b.ContentEnd.CompareTo(selection.Start) >= 0);
                        if (currentBlock != null)
                        {
                            var index = ZoomTextEditor.Document.Blocks.ToList().IndexOf(currentBlock);
                            ZoomTextEditor.Document.Blocks.InsertAfter(currentBlock, paragraph);
                        }
                        else
                        {
                            ZoomTextEditor.Document.Blocks.Add(paragraph);
                        }
                    }
                }
                // Check if clipboard contains HTML
                else if (clipboardData.GetDataPresent(DataFormats.Html))
                {
                    var htmlData = clipboardData.GetData(DataFormats.Html) as string;
                    if (!string.IsNullOrEmpty(htmlData))
                    {
                        // Insert HTML data to preserve formatting
                        var selection = ZoomTextEditor.Selection;
                        selection.Text = "";
                        
                        var paragraph = new System.Windows.Documents.Paragraph();
                        paragraph.TextAlignment = System.Windows.TextAlignment.Center;
                        
                        var range = new System.Windows.Documents.TextRange(paragraph.ContentStart, paragraph.ContentEnd);
                        range.Load(StreamFromString(htmlData), DataFormats.Html);
                        
                        var currentBlock = ZoomTextEditor.Document.Blocks.FirstOrDefault(b => b.ContentStart.CompareTo(selection.Start) <= 0 && b.ContentEnd.CompareTo(selection.Start) >= 0);
                        if (currentBlock != null)
                        {
                            ZoomTextEditor.Document.Blocks.InsertAfter(currentBlock, paragraph);
                        }
                        else
                        {
                            ZoomTextEditor.Document.Blocks.Add(paragraph);
                        }
                    }
                }
                // Fallback to regular paste for plain text and images
                else
                {
                    ZoomTextEditor.Paste();
                }
            }
            else
            {
                // Fallback to regular paste
                ZoomTextEditor.Paste();
            }
            
            // Force layout update and then update width after pasting content
            ZoomTextEditor.UpdateLayout();
            
            // Use a timer to ensure content is fully processed before width calculation
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                UpdateZoomTextEditorWidth();
            };
            timer.Start();
        }
        
        private Stream StreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private void UpdateZoomTextEditorWidth()
        {
            if (!isZoomTextEditorVisible || !isZoomed) return;
            
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var zoomedWidth = zoomedRegion.Width * zoomFactor;
            var newWidth = CalculateOptimalTextEditorWidth(zoomedWidth, screenWidth);
            
            // Update the width and reposition
            ZoomTextEditor.Width = newWidth;
            
            // Recalculate position with top under zoomed region and bottom near screen bottom
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var centerX = (screenWidth - zoomedWidth) / 2;
            var centerY = (screenHeight - (zoomedRegion.Height * zoomFactor)) / 2 - (screenHeight * 0.1);
            var textBoxX = centerX + (zoomedWidth - newWidth) / 2;
            var textBoxTopY = centerY + (zoomedRegion.Height * zoomFactor) + 10; // 10 pixels below the region
            var textBoxBottomY = screenHeight - 20; // 20 pixels from bottom of screen
            var textBoxHeight = textBoxBottomY - textBoxTopY; // Fill available space
            
            Canvas.SetLeft(ZoomTextEditor, textBoxX);
            Canvas.SetTop(ZoomTextEditor, textBoxTopY);
            ZoomTextEditor.Height = textBoxHeight;
            
            LogToFile($"Updated zoom text editor width to: {newWidth}");
        }

        private void ZoomTextEditor_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Ensure all paragraphs are center-aligned
            foreach (var block in ZoomTextEditor.Document.Blocks)
            {
                if (block is System.Windows.Documents.Paragraph paragraph)
                {
                    paragraph.TextAlignment = System.Windows.TextAlignment.Center;
                }
            }
            
            // Update width when text content changes (with a small delay to avoid too frequent updates)
            if (isZoomTextEditorVisible && isZoomed)
            {
                // Use DispatcherTimer to delay the width update
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    UpdateZoomTextEditorWidth();
                };
                timer.Start();
            }
        }
    }

    // Speech Balloon class
    public class SpeechBalloon
    {
        public System.Windows.Point Location { get; set; }
        public string Text { get; set; }
        public System.Windows.Shapes.Path BalloonShape { get; set; }
        public System.Windows.Controls.TextBlock TextBlock { get; set; }
        public System.Windows.Shapes.Ellipse? TailTip { get; set; }
        public System.Windows.Shapes.Rectangle? DragArea { get; set; }
        public double BalloonWidth { get; set; }
        public double BalloonHeight { get; set; }
        
        public SpeechBalloon(string text, System.Windows.Point location)
        {
            Location = location;
            Text = text;
            
            // Create the balloon shape with tail
            CreateBalloonShape();
            CreateTextBlock();
        }
        
        private void CreateBalloonShape()
        {
            // Measure the text to determine balloon size
            var formattedText = new System.Windows.Media.FormattedText(
                Text,
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                new System.Windows.Media.Typeface("Arial"),
                12,
                System.Windows.Media.Brushes.Black,
                VisualTreeHelper.GetDpi(new System.Windows.Controls.TextBlock()).PixelsPerDip);

            // Calculate balloon dimensions with padding
            BalloonWidth = Math.Max(formattedText.Width + 30, 100); // Minimum 100px width, 15px padding each side
            BalloonHeight = Math.Max(formattedText.Height + 20, 40); // Minimum 40px height, 10px padding top/bottom
            
            // Create the balloon shape as a rectangle with rounded corners
            var balloonBody = new System.Windows.Media.RectangleGeometry(
                new System.Windows.Rect(0, 0, BalloonWidth, BalloonHeight), 10, 10); // 10px corner radius
            
            // Create the tail as a triangle shape with pointed tip
            var tailGeometry = new System.Windows.Media.PathGeometry();
            var tailFigure = new System.Windows.Media.PathFigure();
            
            // Triangle tail points - base connects to balloon, tip points outward
            var baseLeft = new System.Windows.Point(BalloonWidth * 0.3, BalloonHeight);
            var baseRight = new System.Windows.Point(BalloonWidth * 0.4, BalloonHeight);
            var tipPoint = new System.Windows.Point(BalloonWidth * 0.35, BalloonHeight + 20); // Triangle tip pointing outward
            
            // Create triangle shape
            tailFigure.StartPoint = baseLeft;
            tailFigure.Segments.Add(new System.Windows.Media.LineSegment(baseRight, true));
            tailFigure.Segments.Add(new System.Windows.Media.LineSegment(tipPoint, true));
            tailFigure.Segments.Add(new System.Windows.Media.LineSegment(baseLeft, true)); // Close triangle
            tailFigure.IsClosed = true;
            
            tailGeometry.Figures.Add(tailFigure);
            
            // Combine balloon body and tail
            var combinedGeometry = new System.Windows.Media.GeometryGroup();
            combinedGeometry.Children.Add(balloonBody);
            combinedGeometry.Children.Add(tailGeometry);
            
            BalloonShape = new System.Windows.Shapes.Path
            {
                Data = combinedGeometry,
                Fill = System.Windows.Media.Brushes.White,
                Stroke = null, // No outline
                StrokeThickness = 0,
                Tag = "SpeechBalloon",
                IsHitTestVisible = true, // Ensure it can receive mouse events
                Cursor = Cursors.Hand // Show hand cursor when hovering
            };
            
            // Position the balloon so that the tail tip (red dot) appears at the clicked location
            // Tail tip position = BalloonX + (BalloonWidth * 0.35 - 5), BalloonY + (BalloonHeight + 20 - 5)
            // So if click position is Location.X, Location.Y, then:
            // BalloonX = Location.X - (BalloonWidth * 0.35 - 5)
            // BalloonY = Location.Y - (BalloonHeight + 20 - 5)
            var balloonX = Location.X - (BalloonWidth * 0.35 - 5);
            var balloonY = Location.Y - (BalloonHeight + 20 - 5);
            
            Canvas.SetLeft(BalloonShape, balloonX);
            Canvas.SetTop(BalloonShape, balloonY);
            Panel.SetZIndex(BalloonShape, 1000);
            
            // Update Location property to reflect the actual balloon position (not click position)
            Location = new System.Windows.Point(balloonX, balloonY);
        }
        
        private void CreateTextBlock()
        {
            TextBlock = new System.Windows.Controls.TextBlock
            {
                Text = Text,
                FontFamily = new System.Windows.Media.FontFamily("Arial"),
                FontSize = 12,
                FontWeight = System.Windows.FontWeights.Normal,
                Foreground = System.Windows.Media.Brushes.Black,
                TextWrapping = System.Windows.TextWrapping.Wrap,
                MaxWidth = BalloonWidth - 20, // Leave 10px padding on each side
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                TextAlignment = System.Windows.TextAlignment.Center,
                Tag = "SpeechBalloonText",
                IsHitTestVisible = true, // Ensure it can receive mouse events
                Cursor = Cursors.Hand // Show hand cursor when hovering
            };
            
            // Position the text in the center of the balloon (use the updated Location from CreateBalloonShape)
            Canvas.SetLeft(TextBlock, Location.X + 10); // 10px padding from left of balloon
            Canvas.SetTop(TextBlock, Location.Y + 10); // 10px padding from top of balloon
            Panel.SetZIndex(TextBlock, 1001);
        }
        
        public void UpdateTail(System.Windows.Point balloonConnectionPoint, System.Windows.Point mousePosition)
        {
            // Calculate the tail direction and length from balloon to mouse
            var deltaX = mousePosition.X - balloonConnectionPoint.X;
            var deltaY = mousePosition.Y - balloonConnectionPoint.Y;
            var length = System.Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            
            if (length < 10) return; // Minimum tail length
            
            var tailGeometry = new System.Windows.Media.PathGeometry();
            var tailFigure = new System.Windows.Media.PathFigure();
            
            // Tail connection points on the balloon (using actual dimensions)
            var leftConnect = new System.Windows.Point(BalloonWidth * 0.3, BalloonHeight);
            var rightConnect = new System.Windows.Point(BalloonWidth * 0.4, BalloonHeight);
            
            // The tail tip should be at the mouse position (relative to balloon)
            var tailTip = new System.Windows.Point(
                mousePosition.X - Canvas.GetLeft(BalloonShape),
                mousePosition.Y - Canvas.GetTop(BalloonShape)
            );
            
            // Ensure minimum tail width to keep it visible when stretched
            var baseWidth = BalloonWidth * 0.1; // 10% of balloon width
            var minBaseWidth = 8.0; // Minimum 8 pixels wide
            
            if (baseWidth < minBaseWidth)
            {
                var centerX = (leftConnect.X + rightConnect.X) / 2;
                leftConnect = new System.Windows.Point(centerX - minBaseWidth / 2, BalloonHeight);
                rightConnect = new System.Windows.Point(centerX + minBaseWidth / 2, BalloonHeight);
            }
            
            // Create triangle tail with tip pointing to the mouse position
            tailFigure.StartPoint = leftConnect;
            tailFigure.Segments.Add(new System.Windows.Media.LineSegment(rightConnect, true));
            tailFigure.Segments.Add(new System.Windows.Media.LineSegment(tailTip, true)); // Triangle tip at mouse position
            tailFigure.Segments.Add(new System.Windows.Media.LineSegment(leftConnect, true)); // Close triangle
            tailFigure.IsClosed = true;
            
            tailGeometry.Figures.Add(tailFigure);
            
            // Update the balloon geometry - use rectangle with rounded corners (preserve adaptive shape)
            var balloonBody = new System.Windows.Media.RectangleGeometry(
                new System.Windows.Rect(0, 0, BalloonWidth, BalloonHeight), 10, 10); // 10px corner radius
            
            var combinedGeometry = new System.Windows.Media.GeometryGroup();
            combinedGeometry.Children.Add(balloonBody);
            combinedGeometry.Children.Add(tailGeometry);
            
            BalloonShape.Data = combinedGeometry;
            
            // Ensure the balloon maintains white fill and no stroke even when tail is stretched
            BalloonShape.Fill = System.Windows.Media.Brushes.White;
            BalloonShape.Stroke = null; // No outline
            BalloonShape.StrokeThickness = 0;
            
            // Update tail tip position to follow mouse
            if (TailTip != null)
            {
                Canvas.SetLeft(TailTip, mousePosition.X - 4);
                Canvas.SetTop(TailTip, mousePosition.Y - 4);
            }
        }
    }
}
