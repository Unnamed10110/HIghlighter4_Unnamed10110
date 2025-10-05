using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

// Aliases para resolver conflictos de nombres
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using Path = System.Windows.Shapes.Path;
using DrawingColor = System.Drawing.Color;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingPath = System.IO.Path;
using WpfImage = System.Windows.Controls.Image;

namespace Highlighter4
{
    public partial class ImageEditor : Window
    {
        #region Enums y Clases de Soporte
        
        public enum DrawingTool
        {
            None,
            Arrow,      // F1
            Line,       // F2
            Rectangle,  // F3
            Highlighter, // F4
            Blur,       // F5
            Speech,     // F6
            Crop        // F7
        }
        
        public class DrawingAction
        {
            public DrawingTool Tool { get; set; }
            public List<UIElement> Elements { get; set; } = new List<UIElement>();
            public string Description { get; set; }
        }
        
        #endregion
        
        #region Variables de Estado
        
        private DrawingTool currentTool = DrawingTool.None;
        private bool isDrawing = false;
        private Point startPoint;
        private Point endPoint;
        
        // Variables para diferentes herramientas
        private Path previewArrow;
        private Line previewLine;
        private Rectangle previewRectangle;
        private Rectangle previewHighlighter;
        private Rectangle previewBlur;
        private Path previewSpeech;
        
        // Historial para undo/redo
        private List<DrawingAction> undoHistory = new List<DrawingAction>();
        private List<DrawingAction> redoHistory = new List<DrawingAction>();
        private DrawingAction currentAction;
        
        // Imagen de fondo
        private BitmapSource backgroundImage;
        private string originalImagePath;
        
        // Variables para crop
        private Rectangle? previewCrop;
        private bool isCropDrawing = false;
        
        // Variables para speech balloon
        private bool isSpeechBalloonMode = false;
        private List<SpeechBalloon> speechBalloons = new List<SpeechBalloon>();
        private bool isStretchingTail = false;
        private SpeechBalloon? currentStretchingBalloon;
        private Point tailStartPoint;
        
        #endregion
        
        #region Constructor y Eventos de Ventana
        
        public ImageEditor()
        {
            InitializeComponent();
            InitializeEditor();
        }
        
        public ImageEditor(BitmapSource image) : this()
        {
            LoadImage(image);
        }
        
        public ImageEditor(string imagePath) : this()
        {
            LoadImageFromFile(imagePath);
        }
        
        private void InitializeEditor()
        {
            // Configurar eventos de teclado
            this.KeyDown += ImageEditor_KeyDown;
            this.Focusable = true;
            this.Focus();
            
            // Configurar canvas
            MainCanvas.Background = new SolidColorBrush(Colors.Transparent);
            
            // Update status
            UpdateStatus("Editor ready - Select a tool");
            UpdateToolButtons();
            
            // Enfocar la ventana cuando se carga
            this.Loaded += (s, e) => {
                this.Activate();
                this.Topmost = true;
                this.Topmost = false;
                this.Focus();
                
                // Enfocar el botón Save después de un pequeño delay
                Dispatcher.BeginInvoke(new Action(() => {
                    SaveButton?.Focus();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            };
        }
        
        private void ImageEditor_KeyDown(object sender, KeyEventArgs e)
        {
            HandleKeyboardShortcuts(e);
        }
        
        #endregion
        
        #region Carga de Imágenes
        
        public void LoadImage(BitmapSource image)
        {
            backgroundImage = image;
            
            // Limpiar canvas
            MainCanvas.Children.Clear();
            
            // Agregar imagen de fondo
            var imageControl = new System.Windows.Controls.Image
            {
                Source = image,
                Stretch = Stretch.None
            };
            
            Canvas.SetLeft(imageControl, 0);
            Canvas.SetTop(imageControl, 0);
            MainCanvas.Children.Add(imageControl);
            
            // Ajustar tamaño del canvas
            MainCanvas.Width = image.PixelWidth;
            MainCanvas.Height = image.PixelHeight;
        }
        
        public void LoadImageFromFile(string imagePath)
        {
            try
            {
                originalImagePath = imagePath;
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                
                LoadImage(bitmap);
                UpdateStatus($"Image loaded: {DrawingPath.GetFileName(imagePath)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        #endregion
        
        #region Manejo de Eventos del Canvas
        
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Manejar modo speech balloon
            if (isSpeechBalloonMode)
            {
                var clickPoint = e.GetPosition(MainCanvas);
                CreateSpeechBalloon(clickPoint);
                return;
            }
            
            if (currentTool == DrawingTool.None) return;
            
            startPoint = e.GetPosition(MainCanvas);
            isDrawing = true;
            
            // Iniciar nueva acción para undo
            StartNewAction();
            
            switch (currentTool)
            {
                case DrawingTool.Arrow:
                    StartArrowDrawing();
                    break;
                case DrawingTool.Line:
                    StartLineDrawing();
                    break;
                case DrawingTool.Rectangle:
                    StartRectangleDrawing();
                    break;
                case DrawingTool.Highlighter:
                    StartHighlighterDrawing();
                    break;
                case DrawingTool.Blur:
                    StartBlurDrawing();
                    break;
                case DrawingTool.Speech:
                    StartSpeechDrawing();
                    break;
                case DrawingTool.Crop:
                    // Si ya hay un crop overlay activo, no iniciar otro
                    if (cropOverlay == null)
                    {
                        StartCropDrawing();
                    }
                    break;
            }
            
            UpdatePositionText(startPoint);
        }
        
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDrawing) return;
            
            endPoint = e.GetPosition(MainCanvas);
            UpdatePositionText(endPoint);
            
            switch (currentTool)
            {
                case DrawingTool.Arrow:
                    UpdateArrowDrawing();
                    break;
                case DrawingTool.Line:
                    UpdateLineDrawing();
                    break;
                case DrawingTool.Rectangle:
                    UpdateRectangleDrawing();
                    break;
                case DrawingTool.Highlighter:
                    UpdateHighlighterDrawing();
                    break;
                case DrawingTool.Blur:
                    UpdateBlurDrawing();
                    break;
                case DrawingTool.Speech:
                    UpdateSpeechDrawing();
                    break;
                case DrawingTool.Crop:
                    UpdateCropDrawing();
                    break;
            }
        }
        
        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!isDrawing) return;
            
            isDrawing = false;
            
            switch (currentTool)
            {
                case DrawingTool.Arrow:
                    FinishArrowDrawing();
                    break;
                case DrawingTool.Line:
                    FinishLineDrawing();
                    break;
                case DrawingTool.Rectangle:
                    FinishRectangleDrawing();
                    break;
                case DrawingTool.Highlighter:
                    FinishHighlighterDrawing();
                    break;
                case DrawingTool.Blur:
                    FinishBlurDrawing();
                    break;
                case DrawingTool.Speech:
                    FinishSpeechDrawing();
                    break;
                case DrawingTool.Crop:
                    FinishCropDrawing();
                    break;
            }
            
            // Finalizar acción para undo
            FinishCurrentAction();
        }
        
        #endregion
        
        #region Herramientas de Dibujo
        
        #region F1 - Flechas
        private void StartArrowDrawing()
        {
            previewArrow = new Path
            {
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 3,
                Fill = new SolidColorBrush(Colors.Red)
            };
            MainCanvas.Children.Add(previewArrow);
        }
        
        private void UpdateArrowDrawing()
        {
            if (previewArrow == null) return;
            
            var arrowGeometry = CreateArrowGeometry(startPoint, endPoint);
            previewArrow.Data = arrowGeometry;
        }
        
        private void FinishArrowDrawing()
        {
            if (previewArrow == null) return;
            
            // Convertir preview a elemento final
            var finalArrow = new Path
            {
                Data = previewArrow.Data,
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 3,
                Fill = new SolidColorBrush(Colors.Red)
            };
            
            MainCanvas.Children.Remove(previewArrow);
            MainCanvas.Children.Add(finalArrow);
            currentAction.Elements.Add(finalArrow);
            
            previewArrow = null;
        }
        
        private Geometry CreateArrowGeometry(Point start, Point end)
        {
            var group = new GeometryGroup();
            
            // Línea principal
            var line = new LineGeometry(start, end);
            group.Children.Add(line);
            
            // Calcular punta de flecha
            var direction = new Vector(end.X - start.X, end.Y - start.Y);
            direction.Normalize();
            
            var arrowLength = 15;
            var arrowAngle = Math.PI / 6; // 30 grados
            
            var arrow1 = new Vector(
                direction.X * Math.Cos(arrowAngle) + direction.Y * Math.Sin(arrowAngle),
                -direction.X * Math.Sin(arrowAngle) + direction.Y * Math.Cos(arrowAngle)
            ) * arrowLength;
            
            var arrow2 = new Vector(
                direction.X * Math.Cos(-arrowAngle) + direction.Y * Math.Sin(-arrowAngle),
                -direction.X * Math.Sin(-arrowAngle) + direction.Y * Math.Cos(-arrowAngle)
            ) * arrowLength;
            
            var arrow1End = new Point(end.X - arrow1.X, end.Y - arrow1.Y);
            var arrow2End = new Point(end.X - arrow2.X, end.Y - arrow2.Y);
            
            group.Children.Add(new LineGeometry(end, arrow1End));
            group.Children.Add(new LineGeometry(end, arrow2End));
            
            return group;
        }
        #endregion
        
        #region F2 - Líneas
        private void StartLineDrawing()
        {
            previewLine = new Line
            {
                X1 = startPoint.X,
                Y1 = startPoint.Y,
                X2 = endPoint.X,
                Y2 = endPoint.Y,
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 2
            };
            MainCanvas.Children.Add(previewLine);
        }
        
        private void UpdateLineDrawing()
        {
            if (previewLine == null) return;
            previewLine.X2 = endPoint.X;
            previewLine.Y2 = endPoint.Y;
        }
        
        private void FinishLineDrawing()
        {
            if (previewLine == null) return;
            
            var finalLine = new Line
            {
                X1 = previewLine.X1,
                Y1 = previewLine.Y1,
                X2 = previewLine.X2,
                Y2 = previewLine.Y2,
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 2
            };
            
            MainCanvas.Children.Remove(previewLine);
            MainCanvas.Children.Add(finalLine);
            currentAction.Elements.Add(finalLine);
            
            previewLine = null;
        }
        #endregion
        
        #region F3 - Rectángulos
        private void StartRectangleDrawing()
        {
            previewRectangle = new Rectangle
            {
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 2,
                Fill = null
            };
            
            Canvas.SetLeft(previewRectangle, startPoint.X);
            Canvas.SetTop(previewRectangle, startPoint.Y);
            MainCanvas.Children.Add(previewRectangle);
        }
        
        private void UpdateRectangleDrawing()
        {
            if (previewRectangle == null) return;
            
            var left = Math.Min(startPoint.X, endPoint.X);
            var top = Math.Min(startPoint.Y, endPoint.Y);
            var width = Math.Abs(endPoint.X - startPoint.X);
            var height = Math.Abs(endPoint.Y - startPoint.Y);
            
            Canvas.SetLeft(previewRectangle, left);
            Canvas.SetTop(previewRectangle, top);
            previewRectangle.Width = width;
            previewRectangle.Height = height;
        }
        
        private void FinishRectangleDrawing()
        {
            if (previewRectangle == null) return;
            
            var finalRectangle = new Rectangle
            {
                Width = previewRectangle.Width,
                Height = previewRectangle.Height,
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 2,
                Fill = null
            };
            
            Canvas.SetLeft(finalRectangle, Canvas.GetLeft(previewRectangle));
            Canvas.SetTop(finalRectangle, Canvas.GetTop(previewRectangle));
            
            MainCanvas.Children.Remove(previewRectangle);
            MainCanvas.Children.Add(finalRectangle);
            currentAction.Elements.Add(finalRectangle);
            
            previewRectangle = null;
        }
        #endregion
        
        #region F4 - Resaltador
        private void StartHighlighterDrawing()
        {
            previewHighlighter = new Rectangle
            {
                Stroke = new SolidColorBrush(Colors.Transparent),
                Fill = new SolidColorBrush(Color.FromArgb(100, 255, 255, 0))
            };
            
            Canvas.SetLeft(previewHighlighter, startPoint.X);
            Canvas.SetTop(previewHighlighter, startPoint.Y);
            MainCanvas.Children.Add(previewHighlighter);
        }
        
        private void UpdateHighlighterDrawing()
        {
            if (previewHighlighter == null) return;
            
            var left = Math.Min(startPoint.X, endPoint.X);
            var top = Math.Min(startPoint.Y, endPoint.Y);
            var width = Math.Abs(endPoint.X - startPoint.X);
            var height = Math.Abs(endPoint.Y - startPoint.Y);
            
            Canvas.SetLeft(previewHighlighter, left);
            Canvas.SetTop(previewHighlighter, top);
            previewHighlighter.Width = width;
            previewHighlighter.Height = height;
        }
        
        private void FinishHighlighterDrawing()
        {
            if (previewHighlighter == null) return;
            
            var finalHighlighter = new Rectangle
            {
                Width = previewHighlighter.Width,
                Height = previewHighlighter.Height,
                Stroke = new SolidColorBrush(Colors.Transparent),
                Fill = new SolidColorBrush(Color.FromArgb(100, 255, 255, 0))
            };
            
            Canvas.SetLeft(finalHighlighter, Canvas.GetLeft(previewHighlighter));
            Canvas.SetTop(finalHighlighter, Canvas.GetTop(previewHighlighter));
            
            MainCanvas.Children.Remove(previewHighlighter);
            MainCanvas.Children.Add(finalHighlighter);
            currentAction.Elements.Add(finalHighlighter);
            
            previewHighlighter = null;
        }
        #endregion
        
        #region F5 - Blur
        private void StartBlurDrawing()
        {
            previewBlur = new Rectangle
            {
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 5 },
                Fill = new SolidColorBrush(Colors.Transparent)
            };
            
            Canvas.SetLeft(previewBlur, startPoint.X);
            Canvas.SetTop(previewBlur, startPoint.Y);
            MainCanvas.Children.Add(previewBlur);
        }
        
        private void UpdateBlurDrawing()
        {
            if (previewBlur == null) return;
            
            var left = Math.Min(startPoint.X, endPoint.X);
            var top = Math.Min(startPoint.Y, endPoint.Y);
            var width = Math.Abs(endPoint.X - startPoint.X);
            var height = Math.Abs(endPoint.Y - startPoint.Y);
            
            Canvas.SetLeft(previewBlur, left);
            Canvas.SetTop(previewBlur, top);
            previewBlur.Width = width;
            previewBlur.Height = height;
        }
        
        private void FinishBlurDrawing()
        {
            if (previewBlur == null) return;
            
            var left = Canvas.GetLeft(previewBlur);
            var top = Canvas.GetTop(previewBlur);
            var width = previewBlur.Width;
            var height = previewBlur.Height;
            
            // Crear una imagen con el contenido capturado
            var capturedImage = CaptureCanvasRegion(left, top, width, height);
            if (capturedImage != null)
            {
                // Aplicar blur a la imagen capturada
                var blurredImage = new WpfImage
                {
                    Source = capturedImage,
                    Effect = new BlurEffect { Radius = 15 },
                    Width = width,
                    Height = height
                };
                
                Canvas.SetLeft(blurredImage, left);
                Canvas.SetTop(blurredImage, top);
                
                MainCanvas.Children.Remove(previewBlur);
                MainCanvas.Children.Add(blurredImage);
                currentAction.Elements.Add(blurredImage);
            }
            
            previewBlur = null;
        }
        
        private BitmapSource CaptureCanvasRegion(double x, double y, double width, double height)
        {
            try
            {
                // Capturar todo el canvas primero
                var canvasBounds = VisualTreeHelper.GetDescendantBounds(MainCanvas);
                var fullCanvasBitmap = new RenderTargetBitmap(
                    (int)canvasBounds.Width, (int)canvasBounds.Height, 96, 96, PixelFormats.Pbgra32);
                
                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    var visualBrush = new VisualBrush(MainCanvas);
                    drawingContext.DrawRectangle(visualBrush, null, canvasBounds);
                }
                
                fullCanvasBitmap.Render(drawingVisual);
                fullCanvasBitmap.Freeze();
                
                // Recortar solo la región deseada
                var cropRect = new Int32Rect((int)x, (int)y, (int)width, (int)height);
                var croppedBitmap = new CroppedBitmap(fullCanvasBitmap, cropRect);
                croppedBitmap.Freeze();
                
                return croppedBitmap;
            }
            catch
            {
                return null;
            }
        }
        #endregion
        
        #region F6 - Globos de Texto
        private void StartSpeechDrawing()
        {
            previewSpeech = new Path
            {
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255))
            };
            MainCanvas.Children.Add(previewSpeech);
        }
        
        private void UpdateSpeechDrawing()
        {
            if (previewSpeech == null) return;
            
            var speechGeometry = CreateSpeechBubbleGeometry(startPoint, endPoint);
            previewSpeech.Data = speechGeometry;
        }
        
        private void FinishSpeechDrawing()
        {
            if (previewSpeech == null) return;
            
            var finalSpeech = new Path
            {
                Data = previewSpeech.Data,
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255))
            };
            
            MainCanvas.Children.Remove(previewSpeech);
            MainCanvas.Children.Add(finalSpeech);
            currentAction.Elements.Add(finalSpeech);
            
            previewSpeech = null;
        }
        
        private Geometry CreateSpeechBubbleGeometry(Point start, Point end)
        {
            var group = new GeometryGroup();
            
            // Calcular dimensiones del globo
            var width = Math.Abs(end.X - start.X);
            var height = Math.Abs(end.Y - start.Y);
            var left = Math.Min(start.X, end.X);
            var top = Math.Min(start.Y, end.Y);
            
            if (width < 50) width = 50;
            if (height < 30) height = 30;
            
            // Cuerpo principal del globo (rectángulo redondeado)
            var rect = new System.Windows.Media.RectangleGeometry(new System.Windows.Rect(left, top, width, height), 10, 10);
            group.Children.Add(rect);
            
            // Cola del globo (triángulo)
            var tailWidth = 15;
            var tailHeight = 20;
            var tailX = left + width / 2 - tailWidth / 2;
            var tailY = top + height;
            
            var tailGeometry = new System.Windows.Media.PathGeometry();
            var tailFigure = new System.Windows.Media.PathFigure();
            tailFigure.StartPoint = new Point(tailX, tailY);
            tailFigure.Segments.Add(new System.Windows.Media.LineSegment(new Point(tailX + tailWidth / 2, tailY + tailHeight), true));
            tailFigure.Segments.Add(new System.Windows.Media.LineSegment(new Point(tailX + tailWidth, tailY), true));
            tailFigure.Segments.Add(new System.Windows.Media.LineSegment(new Point(tailX, tailY), true));
            tailFigure.IsClosed = true;
            tailGeometry.Figures.Add(tailFigure);
            
            group.Children.Add(tailGeometry);
            
            return group;
        }
        #endregion
        
        #region F7 - Crop Mejorado
        private CropOverlay? cropOverlay;
        private bool isCropMode = false;
        
        private void StartCropDrawing()
        {
            // Si ya hay un crop overlay activo, no crear otro
            if (cropOverlay != null)
                return;
                
            // Entrar en modo crop
            isCropMode = true;
            isCropDrawing = true;
            
            // Crear overlay de crop interactivo
            cropOverlay = new CropOverlay(MainCanvas, backgroundImage);
            cropOverlay.CropCompleted += OnCropCompleted;
            cropOverlay.CropCancelled += OnCropCancelled;
            
            // Show instructions
            UpdateStatus("Crop mode activated - Drag to select area, use handles to adjust");
        }
        
        private void UpdateCropDrawing()
        {
            // El crop overlay maneja su propia actualización
            if (cropOverlay != null)
            {
                cropOverlay.UpdateCropArea(startPoint, endPoint);
            }
        }
        
        private void FinishCropDrawing()
        {
            if (cropOverlay != null && isCropDrawing)
            {
                cropOverlay.FinalizeCropArea();
                isCropDrawing = false;
            }
        }
        
        private void OnCropCompleted(double left, double top, double width, double height)
        {
            CropImage(left, top, width, height);
            ExitCropMode();
        }
        
        private void OnCropCancelled()
        {
            ExitCropMode();
        }
        
        private void ExitCropMode()
        {
            isCropMode = false;
            isCropDrawing = false;
            cropOverlay = null;
            UpdateStatus("Crop mode deactivated");
        }
        
        private void CropImage(double left, double top, double width, double height)
        {
            try
            {
                // Crear un nuevo bitmap recortado
                var croppedBitmap = new CroppedBitmap(backgroundImage, new Int32Rect(
                    (int)left, (int)top, (int)width, (int)height));
                
                // Limpiar el canvas
                MainCanvas.Children.Clear();
                
                // Agregar la imagen recortada
                var imageControl = new System.Windows.Controls.Image
                {
                    Source = croppedBitmap,
                    Stretch = Stretch.None
                };
                MainCanvas.Children.Add(imageControl);
                
                // Actualizar la imagen de fondo
                backgroundImage = croppedBitmap;
                
                UpdateStatus($"Image cropped: {width:F0} x {height:F0} pixels");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cropping image: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion
        
        #region Clase CropOverlay
        public class CropOverlay
        {
            private Canvas parentCanvas;
            private BitmapSource originalImage;
            private Rectangle cropArea;
            private Rectangle darkOverlay;
            private List<Ellipse> handles;
            private List<Rectangle> handleAreas;
            private List<Rectangle> draggableBorders;
            private bool isDragging = false;
            private bool isResizing = false;
            private bool isBorderDragging = false;
            private int activeHandle = -1;
            private int activeBorder = -1;
            private Point lastMousePosition;
            private double cropLeft, cropTop, cropWidth, cropHeight;
            
            public event Action<double, double, double, double>? CropCompleted;
            public event Action? CropCancelled;
            
            public CropOverlay(Canvas canvas, BitmapSource image)
            {
                parentCanvas = canvas;
                originalImage = image;
                
                // Agregar eventos globales de mouse al canvas
                parentCanvas.MouseMove += OnCanvasMouseMove;
                parentCanvas.MouseLeftButtonUp += OnCanvasMouseUp;
                
                // Crear overlay oscuro
                darkOverlay = new Rectangle
                {
                    Width = canvas.Width,
                    Height = canvas.Height,
                    Fill = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(darkOverlay, 0);
                Canvas.SetTop(darkOverlay, 0);
                canvas.Children.Add(darkOverlay);
                
                // Crear área de crop inicial (toda la imagen)
                cropLeft = 0;
                cropTop = 0;
                cropWidth = canvas.Width;
                cropHeight = canvas.Height;
                
                CreateCropArea();
                CreateHandles();
                ShowCropButtons();
            }
            
            private void OnCanvasMouseMove(object sender, MouseEventArgs e)
            {
                if (isResizing && activeHandle >= 0)
                {
                    HandleMouseMove(activeHandle, e.GetPosition(parentCanvas));
                }
                else if (isBorderDragging && activeBorder >= 0)
                {
                    HandleBorderMouseMove(activeBorder, e.GetPosition(parentCanvas));
                }
            }
            
            private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
            {
                if (isResizing)
                {
                    EndHandleDrag();
                }
                else if (isBorderDragging)
                {
                    EndBorderDrag();
                }
            }
            
            private void CreateCropArea()
            {
                // Área de crop (transparente)
                cropArea = new Rectangle
                {
                    Width = cropWidth,
                    Height = cropHeight,
                    Fill = System.Windows.Media.Brushes.Transparent,
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 8, 4 }, // Líneas entrecortadas
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(cropArea, cropLeft);
                Canvas.SetTop(cropArea, cropTop);
                parentCanvas.Children.Add(cropArea);
                
                // Crear bordes arrastrables para redimensionar
                CreateDraggableBorders();
                
                // Agregar animación a las líneas entrecortadas
                StartDashAnimation();
                
                // Crear "ventana" en el overlay oscuro
                UpdateDarkOverlay();
            }
            
            private void StartDashAnimation()
            {
                // Crear animación para las líneas entrecortadas
                var dashAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 12, // Longitud del dash (8) + gap (4)
                    Duration = new Duration(TimeSpan.FromSeconds(1)),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                
                // Aplicar la animación al StrokeDashOffset
                cropArea.BeginAnimation(Rectangle.StrokeDashOffsetProperty, dashAnimation);
            }
            
            private void CreateHandles()
            {
                handles = new List<Ellipse>();
                handleAreas = new List<Rectangle>();
                
                // Handles en las esquinas y centros de los bordes
                var handlePositions = new[]
                {
                    new { X = 0.0, Y = 0.0, Type = 0 }, // Esquina superior izquierda
                    new { X = 0.5, Y = 0.0, Type = 1 }, // Centro superior
                    new { X = 1.0, Y = 0.0, Type = 2 }, // Esquina superior derecha
                    new { X = 1.0, Y = 0.5, Type = 3 }, // Centro derecho
                    new { X = 1.0, Y = 1.0, Type = 4 }, // Esquina inferior derecha
                    new { X = 0.5, Y = 1.0, Type = 5 }, // Centro inferior
                    new { X = 0.0, Y = 1.0, Type = 6 }, // Esquina inferior izquierda
                    new { X = 0.0, Y = 0.5, Type = 7 }  // Centro izquierdo
                };
                
                foreach (var pos in handlePositions)
                {
                    var handle = new Ellipse
                    {
                        Width = 8,
                        Height = 8,
                        Fill = new SolidColorBrush(Colors.Yellow),
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 1
                    };
                    
                    var handleArea = new Rectangle
                    {
                        Width = 16,
                        Height = 16,
                        Fill = System.Windows.Media.Brushes.Transparent,
                        Cursor = GetCursorForHandle(pos.Type)
                    };
                    
                    handles.Add(handle);
                    handleAreas.Add(handleArea);
                    
                    var handleX = cropLeft + (cropWidth * pos.X) - 4;
                    var handleY = cropTop + (cropHeight * pos.Y) - 4;
                    var areaX = cropLeft + (cropWidth * pos.X) - 8;
                    var areaY = cropTop + (cropHeight * pos.Y) - 8;
                    
                    Canvas.SetLeft(handle, handleX);
                    Canvas.SetTop(handle, handleY);
                    Canvas.SetLeft(handleArea, areaX);
                    Canvas.SetTop(handleArea, areaY);
                    
                    parentCanvas.Children.Add(handle);
                    parentCanvas.Children.Add(handleArea);
                    
                    // Eventos de mouse para el handle
                    var index = handles.Count - 1;
                    handleArea.MouseLeftButtonDown += (s, e) => {
                        e.Handled = true;
                        StartHandleDrag(index, e.GetPosition(parentCanvas));
                    };
                    handleArea.MouseMove += (s, e) => {
                        if (isResizing && activeHandle == index)
                        {
                            e.Handled = true;
                            HandleMouseMove(index, e.GetPosition(parentCanvas));
                        }
                    };
                    handleArea.MouseLeftButtonUp += (s, e) => {
                        if (isResizing && activeHandle == index)
                        {
                            e.Handled = true;
                            EndHandleDrag();
                        }
                    };
                }
            }
            
            private void CreateDraggableBorders()
            {
                draggableBorders = new List<Rectangle>();
                
                // Crear 4 bordes arrastrables: superior, inferior, izquierdo, derecho
                var borderThickness = 8; // Grosor del área de arrastre
                
                // Borde superior
                var topBorder = new Rectangle
                {
                    Width = cropWidth,
                    Height = borderThickness,
                    Fill = System.Windows.Media.Brushes.Transparent,
                    Cursor = Cursors.SizeNS,
                    Tag = "TopBorder"
                };
                Canvas.SetLeft(topBorder, cropLeft);
                Canvas.SetTop(topBorder, cropTop - borderThickness / 2);
                
                // Borde inferior
                var bottomBorder = new Rectangle
                {
                    Width = cropWidth,
                    Height = borderThickness,
                    Fill = System.Windows.Media.Brushes.Transparent,
                    Cursor = Cursors.SizeNS,
                    Tag = "BottomBorder"
                };
                Canvas.SetLeft(bottomBorder, cropLeft);
                Canvas.SetTop(bottomBorder, cropTop + cropHeight - borderThickness / 2);
                
                // Borde izquierdo
                var leftBorder = new Rectangle
                {
                    Width = borderThickness,
                    Height = cropHeight,
                    Fill = System.Windows.Media.Brushes.Transparent,
                    Cursor = Cursors.SizeWE,
                    Tag = "LeftBorder"
                };
                Canvas.SetLeft(leftBorder, cropLeft - borderThickness / 2);
                Canvas.SetTop(leftBorder, cropTop);
                
                // Borde derecho
                var rightBorder = new Rectangle
                {
                    Width = borderThickness,
                    Height = cropHeight,
                    Fill = System.Windows.Media.Brushes.Transparent,
                    Cursor = Cursors.SizeWE,
                    Tag = "RightBorder"
                };
                Canvas.SetLeft(rightBorder, cropLeft + cropWidth - borderThickness / 2);
                Canvas.SetTop(rightBorder, cropTop);
                
                // Agregar todos los bordes
                var borders = new[] { topBorder, bottomBorder, leftBorder, rightBorder };
                foreach (var border in borders)
                {
                    draggableBorders.Add(border);
                    parentCanvas.Children.Add(border);
                    
                    // Eventos de mouse para arrastrar el borde
                    var index = draggableBorders.Count - 1;
                    border.MouseLeftButtonDown += (s, e) => {
                        e.Handled = true;
                        StartBorderDrag(index, e.GetPosition(parentCanvas));
                    };
                    border.MouseMove += (s, e) => {
                        if (isBorderDragging && activeBorder == index)
                        {
                            e.Handled = true;
                            HandleBorderMouseMove(index, e.GetPosition(parentCanvas));
                        }
                    };
                    border.MouseLeftButtonUp += (s, e) => {
                        if (isBorderDragging && activeBorder == index)
                        {
                            e.Handled = true;
                            EndBorderDrag();
                        }
                    };
                }
            }
            
            private void UpdateDarkOverlay()
            {
                // Crear máscara con agujero para el área de crop
                var geometryGroup = new GeometryGroup();
                
                // Rectángulo completo
                geometryGroup.Children.Add(new RectangleGeometry(new Rect(0, 0, parentCanvas.Width, parentCanvas.Height)));
                
                // Restar el área de crop
                geometryGroup.Children.Add(new RectangleGeometry(new Rect(cropLeft, cropTop, cropWidth, cropHeight)));
                geometryGroup.FillRule = FillRule.EvenOdd;
                
                darkOverlay.Clip = geometryGroup;
            }
            
            
            private Cursor GetCursorForHandle(int handleType)
            {
                return handleType switch
                {
                    0 or 4 => Cursors.SizeNWSE, // Esquinas diagonales
                    2 or 6 => Cursors.SizeNESW,
                    1 or 5 => Cursors.SizeNS,    // Bordes verticales
                    3 or 7 => Cursors.SizeWE,    // Bordes horizontales
                    _ => Cursors.Arrow
                };
            }
            
            private void StartHandleDrag(int handleIndex, Point position)
            {
                isResizing = true;
                activeHandle = handleIndex;
                lastMousePosition = position;
                
                // Capturar el mouse en el canvas padre
                parentCanvas.CaptureMouse();
            }
            
            private void HandleMouseMove(int handleIndex, Point position)
            {
                if (!isResizing || activeHandle != handleIndex) return;
                
                var deltaX = position.X - lastMousePosition.X;
                var deltaY = position.Y - lastMousePosition.Y;
                
                // Ajustar según el handle activo
                switch (activeHandle)
                {
                    case 0: // Esquina superior izquierda
                        cropLeft += deltaX;
                        cropTop += deltaY;
                        cropWidth -= deltaX;
                        cropHeight -= deltaY;
                        break;
                    case 1: // Centro superior
                        cropTop += deltaY;
                        cropHeight -= deltaY;
                        break;
                    case 2: // Esquina superior derecha
                        cropTop += deltaY;
                        cropWidth += deltaX;
                        cropHeight -= deltaY;
                        break;
                    case 3: // Centro derecho
                        cropWidth += deltaX;
                        break;
                    case 4: // Esquina inferior derecha
                        cropWidth += deltaX;
                        cropHeight += deltaY;
                        break;
                    case 5: // Centro inferior
                        cropHeight += deltaY;
                        break;
                    case 6: // Esquina inferior izquierda
                        cropLeft += deltaX;
                        cropWidth -= deltaX;
                        cropHeight += deltaY;
                        break;
                    case 7: // Centro izquierdo
                        cropLeft += deltaX;
                        cropWidth -= deltaX;
                        break;
                }
                
                // Aplicar límites mínimos
                if (cropWidth < 20) cropWidth = 20;
                if (cropHeight < 20) cropHeight = 20;
                
                // Aplicar límites del canvas
                if (cropLeft < 0) { cropWidth += cropLeft; cropLeft = 0; }
                if (cropTop < 0) { cropHeight += cropTop; cropTop = 0; }
                if (cropLeft + cropWidth > parentCanvas.Width) cropWidth = parentCanvas.Width - cropLeft;
                if (cropTop + cropHeight > parentCanvas.Height) cropHeight = parentCanvas.Height - cropTop;
                
                UpdateCropDisplay();
                lastMousePosition = position;
            }
            
            private void EndHandleDrag()
            {
                isResizing = false;
                activeHandle = -1;
                
                // Liberar la captura del mouse
                parentCanvas.ReleaseMouseCapture();
            }
            
            private void StartBorderDrag(int borderIndex, Point position)
            {
                isBorderDragging = true;
                activeBorder = borderIndex;
                lastMousePosition = position;
                
                // Capturar el mouse en el canvas padre
                parentCanvas.CaptureMouse();
            }
            
            private void HandleBorderMouseMove(int borderIndex, Point position)
            {
                if (!isBorderDragging || activeBorder != borderIndex) return;
                
                var deltaX = position.X - lastMousePosition.X;
                var deltaY = position.Y - lastMousePosition.Y;
                
                // Ajustar según el borde activo
                switch (activeBorder)
                {
                    case 0: // Borde superior
                        cropTop += deltaY;
                        cropHeight -= deltaY;
                        break;
                    case 1: // Borde inferior
                        cropHeight += deltaY;
                        break;
                    case 2: // Borde izquierdo
                        cropLeft += deltaX;
                        cropWidth -= deltaX;
                        break;
                    case 3: // Borde derecho
                        cropWidth += deltaX;
                        break;
                }
                
                // Aplicar límites mínimos
                if (cropWidth < 20) cropWidth = 20;
                if (cropHeight < 20) cropHeight = 20;
                
                // Aplicar límites del canvas
                if (cropLeft < 0) { cropWidth += cropLeft; cropLeft = 0; }
                if (cropTop < 0) { cropHeight += cropTop; cropTop = 0; }
                if (cropLeft + cropWidth > parentCanvas.Width) cropWidth = parentCanvas.Width - cropLeft;
                if (cropTop + cropHeight > parentCanvas.Height) cropHeight = parentCanvas.Height - cropTop;
                
                UpdateCropDisplay();
                lastMousePosition = position;
            }
            
            private void EndBorderDrag()
            {
                isBorderDragging = false;
                activeBorder = -1;
                
                // Liberar la captura del mouse
                parentCanvas.ReleaseMouseCapture();
            }
            
            public void UpdateCropArea(Point start, Point end)
            {
                if (isDragging)
                {
                    var deltaX = end.X - start.X;
                    var deltaY = end.Y - start.Y;
                    
                    cropLeft += deltaX;
                    cropTop += deltaY;
                    
                    // Aplicar límites
                    if (cropLeft < 0) cropLeft = 0;
                    if (cropTop < 0) cropTop = 0;
                    if (cropLeft + cropWidth > parentCanvas.Width) cropLeft = parentCanvas.Width - cropWidth;
                    if (cropTop + cropHeight > parentCanvas.Height) cropTop = parentCanvas.Height - cropHeight;
                    
                    UpdateCropDisplay();
                }
            }
            
            public void FinalizeCropArea()
            {
                // Mostrar botones de confirmación
                ShowCropButtons();
            }
            
            private void ShowCropButtons()
            {
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 20),
                    Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0))
                };
                
                var cropButton = new Button
                {
                    Content = "✓ Recortar",
                    Margin = new Thickness(5),
                    Padding = new Thickness(10, 5, 10, 5),
                    Background = new SolidColorBrush(Colors.Green),
                    Foreground = new SolidColorBrush(Colors.White)
                };
                cropButton.Click += (s, e) => CropCompleted?.Invoke(cropLeft, cropTop, cropWidth, cropHeight);
                
                var cancelButton = new Button
                {
                    Content = "✗ Cancelar",
                    Margin = new Thickness(5),
                    Padding = new Thickness(10, 5, 10, 5),
                    Background = new SolidColorBrush(Colors.Red),
                    Foreground = new SolidColorBrush(Colors.White)
                };
                cancelButton.Click += (s, e) => CropCancelled?.Invoke();
                
                buttonPanel.Children.Add(cropButton);
                buttonPanel.Children.Add(cancelButton);
                
                Canvas.SetLeft(buttonPanel, cropLeft + (cropWidth / 2) - 75);
                Canvas.SetTop(buttonPanel, cropTop + cropHeight + 10);
                parentCanvas.Children.Add(buttonPanel);
            }
            
            private void UpdateCropDisplay()
            {
                // Actualizar posición del área de crop
                Canvas.SetLeft(cropArea, cropLeft);
                Canvas.SetTop(cropArea, cropTop);
                cropArea.Width = cropWidth;
                cropArea.Height = cropHeight;
                
                // Actualizar handles
                UpdateHandles();
                
                // Actualizar bordes arrastrables
                UpdateDraggableBorders();
                
                // Actualizar overlay oscuro
                UpdateDarkOverlay();
            }
            
            private void UpdateHandles()
            {
                var handlePositions = new[]
                {
                    new { X = 0.0, Y = 0.0 }, new { X = 0.5, Y = 0.0 }, new { X = 1.0, Y = 0.0 },
                    new { X = 1.0, Y = 0.5 }, new { X = 1.0, Y = 1.0 }, new { X = 0.5, Y = 1.0 },
                    new { X = 0.0, Y = 1.0 }, new { X = 0.0, Y = 0.5 }
                };
                
                for (int i = 0; i < handles.Count; i++)
                {
                    var pos = handlePositions[i];
                    var handleX = cropLeft + (cropWidth * pos.X) - 4;
                    var handleY = cropTop + (cropHeight * pos.Y) - 4;
                    var areaX = cropLeft + (cropWidth * pos.X) - 8;
                    var areaY = cropTop + (cropHeight * pos.Y) - 8;
                    
                    Canvas.SetLeft(handles[i], handleX);
                    Canvas.SetTop(handles[i], handleY);
                    Canvas.SetLeft(handleAreas[i], areaX);
                    Canvas.SetTop(handleAreas[i], areaY);
                }
            }
            
            private void UpdateDraggableBorders()
            {
                if (draggableBorders == null || draggableBorders.Count < 4) return;
                
                var borderThickness = 8; // Debe coincidir con el grosor definido en CreateDraggableBorders
                
                // Actualizar borde superior
                var topBorder = draggableBorders[0];
                topBorder.Width = cropWidth;
                Canvas.SetLeft(topBorder, cropLeft);
                Canvas.SetTop(topBorder, cropTop - borderThickness / 2);
                
                // Actualizar borde inferior
                var bottomBorder = draggableBorders[1];
                bottomBorder.Width = cropWidth;
                Canvas.SetLeft(bottomBorder, cropLeft);
                Canvas.SetTop(bottomBorder, cropTop + cropHeight - borderThickness / 2);
                
                // Actualizar borde izquierdo
                var leftBorder = draggableBorders[2];
                leftBorder.Height = cropHeight;
                Canvas.SetLeft(leftBorder, cropLeft - borderThickness / 2);
                Canvas.SetTop(leftBorder, cropTop);
                
                // Actualizar borde derecho
                var rightBorder = draggableBorders[3];
                rightBorder.Height = cropHeight;
                Canvas.SetLeft(rightBorder, cropLeft + cropWidth - borderThickness / 2);
                Canvas.SetTop(rightBorder, cropTop);
            }
            
            public void Dispose()
            {
                // Remover eventos de mouse
                parentCanvas.MouseMove -= OnCanvasMouseMove;
                parentCanvas.MouseLeftButtonUp -= OnCanvasMouseUp;
                
                // Liberar captura de mouse si está activa
                if (isResizing || isBorderDragging)
                {
                    parentCanvas.ReleaseMouseCapture();
                }
                
                // Remover todos los elementos del canvas
                parentCanvas.Children.Remove(darkOverlay);
                parentCanvas.Children.Remove(cropArea);
                
                foreach (var handle in handles)
                    parentCanvas.Children.Remove(handle);
                foreach (var area in handleAreas)
                    parentCanvas.Children.Remove(area);
                foreach (var border in draggableBorders)
                    parentCanvas.Children.Remove(border);
            }
        }
        #endregion
        
        #endregion
        
        #region Historial (Undo/Redo)
        
        private void StartNewAction()
        {
            currentAction = new DrawingAction
            {
                Tool = currentTool,
                Description = $"Dibujar {currentTool}"
            };
        }
        
        private void FinishCurrentAction()
        {
            if (currentAction != null && currentAction.Elements.Count > 0)
            {
                undoHistory.Add(currentAction);
                redoHistory.Clear(); // Limpiar redo al hacer nueva acción
                currentAction = null;
                
                UpdateUndoRedoButtons();
            }
        }
        
        private void Undo()
        {
            if (undoHistory.Count == 0) return;
            
            var action = undoHistory.Last();
            undoHistory.RemoveAt(undoHistory.Count - 1);
            
            // Remover elementos del canvas
            foreach (var element in action.Elements)
            {
                MainCanvas.Children.Remove(element);
            }
            
            redoHistory.Add(action);
            UpdateUndoRedoButtons();
            UpdateStatus($"Undone: {action.Description}");
        }
        
        private void Redo()
        {
            if (redoHistory.Count == 0) return;
            
            var action = redoHistory.Last();
            redoHistory.RemoveAt(redoHistory.Count - 1);
            
            // Restaurar elementos al canvas
            foreach (var element in action.Elements)
            {
                MainCanvas.Children.Add(element);
            }
            
            undoHistory.Add(action);
            UpdateUndoRedoButtons();
            UpdateStatus($"Redone: {action.Description}");
        }
        
        private void UpdateUndoRedoButtons()
        {
            UndoButton.IsEnabled = undoHistory.Count > 0;
            RedoButton.IsEnabled = redoHistory.Count > 0;
        }
        
        #endregion
        
        #region Funcionalidades de Archivo
        
        private void SaveImage()
        {
            try
            {
                // Generar ruta automática: Downloads/Highlighter4/dd-MM-yyyy/dd-MM-yyyy_HH-mm-ss.png
                var downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                downloadsPath = DrawingPath.Combine(downloadsPath, "Downloads");
                
                var now = DateTime.Now;
                var dateFolder = now.ToString("dd-MM-yyyy");
                var fileName = now.ToString("dd-MM-yyyy_HH-mm-ss") + ".png";
                
                var basePath = DrawingPath.Combine(downloadsPath, "Highlighter4", dateFolder);
                
                // Crear directorio si no existe
                Directory.CreateDirectory(basePath);
                
                var fullPath = DrawingPath.Combine(basePath, fileName);
                
                // Renderizar y guardar imagen
                var bitmap = RenderCanvasToBitmap();
                SaveBitmapToFile(bitmap, fullPath);
                
                // Mostrar notificación con miniatura
                var systemBitmap = ConvertBitmapSourceToBitmap(bitmap);
                Program.NotifyImageSaved("", "", systemBitmap, fullPath);
                
                // Copiar también al clipboard
                try
                {
                    Clipboard.SetImage(bitmap);
                    UpdateStatus($"Image auto-saved: {fileName} and copied to clipboard");
                }
                catch (Exception clipEx)
                {
                    UpdateStatus($"Image auto-saved: {fileName} (error copying to clipboard: {clipEx.Message})");
                }
                
                // Close editor after saving
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void CopyToClipboard()
        {
            try
            {
                var bitmap = RenderCanvasToBitmap();
                Clipboard.SetImage(bitmap);
                
                // También guardar automáticamente en Downloads/Highlighter4/dd-MM-yyyy/dd-MM-yyyy.png
                var downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                downloadsPath = DrawingPath.Combine(downloadsPath, "Downloads");
                
                var now = DateTime.Now;
                var dateFolder = now.ToString("dd-MM-yyyy");
                var fileName = now.ToString("dd-MM-yyyy") + ".png";
                
                var basePath = DrawingPath.Combine(downloadsPath, "Highlighter4", dateFolder);
                
                // Crear directorio si no existe
                Directory.CreateDirectory(basePath);
                
                var fullPath = DrawingPath.Combine(basePath, fileName);
                
                // Guardar la imagen
                SaveBitmapToFile(bitmap, fullPath);
                
                // Mostrar notificación con miniatura
                var systemBitmap = ConvertBitmapSourceToBitmap(bitmap);
                Program.NotifyImageSaved("", "", systemBitmap, fullPath);
                
                UpdateStatus($"Image copied to clipboard and saved: {fileName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private BitmapSource RenderCanvasToBitmap()
        {
            var bounds = VisualTreeHelper.GetDescendantBounds(MainCanvas);
            var renderTarget = new RenderTargetBitmap(
                (int)bounds.Width, (int)bounds.Height, 96, 96, PixelFormats.Pbgra32);
            
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                var visualBrush = new VisualBrush(MainCanvas);
                drawingContext.DrawRectangle(visualBrush, null, bounds);
            }
            
            renderTarget.Render(drawingVisual);
            return renderTarget;
        }
        
        private void SaveBitmapToFile(BitmapSource bitmap, string filePath)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fileStream);
            }
        }
        
        private System.Drawing.Bitmap ConvertBitmapSourceToBitmap(BitmapSource bitmapSource)
        {
            using (var stream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(stream);
                stream.Position = 0;
                return new System.Drawing.Bitmap(stream);
            }
        }
        
        #endregion
        
        #region Eventos de Botones
        
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveImage();
        }
        
        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            CopyToClipboard();
        }
        
        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            Undo();
        }
        
        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            Redo();
        }
        
        #endregion
        
        #region Eventos de Herramientas
        
        private void ArrowToolButton_Click(object sender, RoutedEventArgs e)
        {
            SetCurrentTool(DrawingTool.Arrow);
        }
        
        private void LineToolButton_Click(object sender, RoutedEventArgs e)
        {
            SetCurrentTool(DrawingTool.Line);
        }
        
        private void RectangleToolButton_Click(object sender, RoutedEventArgs e)
        {
            SetCurrentTool(DrawingTool.Rectangle);
        }
        
        private void HighlighterToolButton_Click(object sender, RoutedEventArgs e)
        {
            SetCurrentTool(DrawingTool.Highlighter);
        }
        
        private void BlurToolButton_Click(object sender, RoutedEventArgs e)
        {
            SetCurrentTool(DrawingTool.Blur);
        }
        
        private void SpeechToolButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleSpeechBalloonMode();
        }
        
        private void CropToolButton_Click(object sender, RoutedEventArgs e)
        {
            SetCurrentTool(DrawingTool.Crop);
        }
        
        #endregion
        
        #region Utilidades
        
        private void SetCurrentTool(DrawingTool tool)
        {
            currentTool = tool;
            UpdateToolButtons();
            UpdateStatus($"Tool selected: {tool}");
            
            // Si se selecciona crop, iniciarlo automáticamente
            if (tool == DrawingTool.Crop)
            {
                StartCropDrawing();
            }
        }
        
        private void ToggleSpeechBalloonMode()
        {
            if (!isSpeechBalloonMode)
            {
                // Desactivar otras herramientas
                currentTool = DrawingTool.None;
                
                // Activar modo speech balloon
                isSpeechBalloonMode = true;
                this.Cursor = Cursors.Hand;
                UpdateToolButtons();
                UpdateStatus("Speech balloon mode activated - Click to create a balloon");
            }
            else
            {
                // Deactivate speech balloon mode
                isSpeechBalloonMode = false;
                this.Cursor = Cursors.Arrow;
                UpdateToolButtons();
                UpdateStatus("Speech balloon mode deactivated");
            }
        }
        
        private void CreateSpeechBalloon(Point position)
        {
            // Crear un diálogo de entrada de texto
            var inputDialog = new TextInputDialog("Ingresa el texto para el globo:");
            inputDialog.Owner = this;
            
            if (inputDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(inputDialog.Text))
            {
                // Iniciar nueva acción para undo
                StartNewAction();
                
                // Crear speech balloon con el texto ingresado
                var speechBalloon = new SpeechBalloon(inputDialog.Text, position);
                speechBalloons.Add(speechBalloon);
                
                // Agregar al canvas
                MainCanvas.Children.Add(speechBalloon.BalloonShape);
                MainCanvas.Children.Add(speechBalloon.TextBlock);
                
                // Hacer arrastrable y estirable
                MakeDraggable(speechBalloon);
                MakeTailStretchable(speechBalloon);
                
                // Agregar elementos a la acción actual para undo
                currentAction.Elements.Add(speechBalloon.BalloonShape);
                currentAction.Elements.Add(speechBalloon.TextBlock);
                if (speechBalloon.DragArea != null)
                    currentAction.Elements.Add(speechBalloon.DragArea);
                if (speechBalloon.TailTip != null)
                    currentAction.Elements.Add(speechBalloon.TailTip);
                currentAction.Tool = DrawingTool.Speech;
                currentAction.Description = $"Globo de texto: {inputDialog.Text}";
                
                // Finalizar acción para undo
                FinishCurrentAction();
                
                UpdateStatus($"Speech balloon created: '{inputDialog.Text}'");
                
                // Auto-deactivate speech balloon mode after creation
                isSpeechBalloonMode = false;
                this.Cursor = Cursors.Arrow;
                UpdateToolButtons();
                UpdateStatus("Speech balloon mode automatically deactivated");
            }
        }
        
        private void MakeDraggable(SpeechBalloon speechBalloon)
        {
            // Crear un rectángulo transparente para captura de mouse confiable
            var dragArea = new Rectangle
            {
                Width = speechBalloon.BalloonWidth,
                Height = speechBalloon.BalloonHeight,
                Fill = System.Windows.Media.Brushes.Transparent,
                Stroke = System.Windows.Media.Brushes.Transparent,
                Tag = "SpeechBalloonDragArea",
                IsHitTestVisible = true,
                Cursor = Cursors.Hand
            };

            // Posicionar el área de arrastre sobre el globo
            Canvas.SetLeft(dragArea, Canvas.GetLeft(speechBalloon.BalloonShape));
            Canvas.SetTop(dragArea, Canvas.GetTop(speechBalloon.BalloonShape));
            Panel.SetZIndex(dragArea, 1002); // Encima del globo pero debajo del texto

            MainCanvas.Children.Add(dragArea);

            // Almacenar referencia para limpieza
            speechBalloon.DragArea = dragArea;

            // Crear variables de estado de arrastre compartidas
            bool isDragging = false;
            Point lastPosition = new Point();

            // Mouse down en área de arrastre
            dragArea.MouseLeftButtonDown += (s, e) =>
            {
                // Verificar si alguna herramienta de dibujo está activa
                if (currentTool != DrawingTool.None)
                {
                    return; // Dejar que el evento suba al manejador de ventana
                }
                
                isDragging = true;
                lastPosition = e.GetPosition(MainCanvas);
                dragArea.CaptureMouse();
                e.Handled = true;
            };

            // Mouse move - manejar arrastre
            dragArea.MouseMove += (s, e) =>
            {
                if (isDragging)
                {
                    var currentPosition = e.GetPosition(MainCanvas);
                    var deltaX = currentPosition.X - lastPosition.X;
                    var deltaY = currentPosition.Y - lastPosition.Y;

                    // Actualizar posición del globo
                    Canvas.SetLeft(speechBalloon.BalloonShape, Canvas.GetLeft(speechBalloon.BalloonShape) + deltaX);
                    Canvas.SetTop(speechBalloon.BalloonShape, Canvas.GetTop(speechBalloon.BalloonShape) + deltaY);

                    // Actualizar posición del texto
                    Canvas.SetLeft(speechBalloon.TextBlock, Canvas.GetLeft(speechBalloon.TextBlock) + deltaX);
                    Canvas.SetTop(speechBalloon.TextBlock, Canvas.GetTop(speechBalloon.TextBlock) + deltaY);

                    // Actualizar posición del área de arrastre
                    Canvas.SetLeft(dragArea, Canvas.GetLeft(dragArea) + deltaX);
                    Canvas.SetTop(dragArea, Canvas.GetTop(dragArea) + deltaY);

                    // Actualizar posición del punto de cola
                    if (speechBalloon.TailTip != null)
                    {
                        Canvas.SetLeft(speechBalloon.TailTip, Canvas.GetLeft(speechBalloon.TailTip) + deltaX);
                        Canvas.SetTop(speechBalloon.TailTip, Canvas.GetTop(speechBalloon.TailTip) + deltaY);
                    }

                    lastPosition = currentPosition;
                    e.Handled = true;
                }
            };

            // Mouse up - detener arrastre
            dragArea.MouseLeftButtonUp += (s, e) =>
            {
                // Verificar si alguna herramienta de dibujo está activa
                if (currentTool != DrawingTool.None)
                {
                    return;
                }
                
                if (isDragging)
                {
                    isDragging = false;
                    dragArea.ReleaseMouseCapture();
                    e.Handled = true;
                }
            };
        }
        
        private void MakeTailStretchable(SpeechBalloon speechBalloon)
        {
            // Agregar un punto rojo visible en la punta de la cola para estirar
            var tailTip = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = System.Windows.Media.Brushes.Red,
                Stroke = System.Windows.Media.Brushes.DarkRed,
                StrokeThickness = 1,
                Tag = "TailTip"
            };

            // Posicionar en la punta de la cola triangular
            Canvas.SetLeft(tailTip, Canvas.GetLeft(speechBalloon.BalloonShape) + speechBalloon.BalloonWidth * 0.35 - 4);
            Canvas.SetTop(tailTip, Canvas.GetTop(speechBalloon.BalloonShape) + speechBalloon.BalloonHeight + 20 - 4);
            Panel.SetZIndex(tailTip, 1003);

            MainCanvas.Children.Add(tailTip);

            bool isStretching = false;
            Point stretchStartPoint;

            tailTip.MouseLeftButtonDown += (s, e) =>
            {
                // Verificar si alguna herramienta de dibujo está activa
                if (currentTool != DrawingTool.None)
                {
                    return;
                }
                
                isStretching = true;
                stretchStartPoint = e.GetPosition(MainCanvas);
                tailTip.CaptureMouse();
                isStretchingTail = true;
                currentStretchingBalloon = speechBalloon;
                // Establecer tailStartPoint en el punto de conexión de la cola del globo
                tailStartPoint = new Point(
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
                // Verificar si alguna herramienta de dibujo está activa
                if (currentTool != DrawingTool.None)
                {
                    return;
                }
                
                if (isStretching)
                {
                    isStretching = false;
                    tailTip.ReleaseMouseCapture();
                    isStretchingTail = false;
                    currentStretchingBalloon = null;
                    this.Cursor = Cursors.Arrow;
                    e.Handled = true;
                }
            };

            // Almacenar referencia
            speechBalloon.TailTip = tailTip;
        }
        
        private void UpdateToolButtons()
        {
            // Reset all buttons
            ArrowToolButton.Background = new SolidColorBrush(Color.FromArgb(255, 33, 38, 45));
            LineToolButton.Background = new SolidColorBrush(Color.FromArgb(255, 33, 38, 45));
            RectangleToolButton.Background = new SolidColorBrush(Color.FromArgb(255, 33, 38, 45));
            HighlighterToolButton.Background = new SolidColorBrush(Color.FromArgb(255, 33, 38, 45));
            BlurToolButton.Background = new SolidColorBrush(Color.FromArgb(255, 33, 38, 45));
            SpeechToolButton.Background = new SolidColorBrush(Color.FromArgb(255, 33, 38, 45));
            CropToolButton.Background = new SolidColorBrush(Color.FromArgb(255, 33, 38, 45));
            
            // Highlight current tool
            var activeColor = new SolidColorBrush(Color.FromArgb(255, 31, 111, 235));
            
            if (isSpeechBalloonMode)
            {
                SpeechToolButton.Background = activeColor;
            }
            else
            {
                switch (currentTool)
                {
                    case DrawingTool.Arrow:
                        ArrowToolButton.Background = activeColor;
                        break;
                    case DrawingTool.Line:
                        LineToolButton.Background = activeColor;
                        break;
                    case DrawingTool.Rectangle:
                        RectangleToolButton.Background = activeColor;
                        break;
                    case DrawingTool.Highlighter:
                        HighlighterToolButton.Background = activeColor;
                        break;
                    case DrawingTool.Blur:
                        BlurToolButton.Background = activeColor;
                        break;
                    case DrawingTool.Speech:
                        SpeechToolButton.Background = activeColor;
                        break;
                    case DrawingTool.Crop:
                        CropToolButton.Background = activeColor;
                        break;
                }
            }
        }
        
        private void HandleKeyboardShortcuts(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (!HandleEscapeHierarchy())
                {
                    // Solo cerrar el editor si no hay herramientas activas
                    this.Close();
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                // Enter para guardar la imagen (excepto cuando hay popups activos)
                if (!IsAnyPopupOpen())
                {
                    SaveImage();
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SaveImage();
                e.Handled = true;
            }
            else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                CopyToClipboard();
                e.Handled = true;
            }
            else if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Undo();
                e.Handled = true;
            }
            else if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Redo();
                e.Handled = true;
            }
            else if (e.Key == Key.F1)
            {
                SetCurrentTool(DrawingTool.Arrow);
                e.Handled = true;
            }
            else if (e.Key == Key.F2)
            {
                SetCurrentTool(DrawingTool.Line);
                e.Handled = true;
            }
            else if (e.Key == Key.F3)
            {
                SetCurrentTool(DrawingTool.Rectangle);
                e.Handled = true;
            }
            else if (e.Key == Key.F4)
            {
                SetCurrentTool(DrawingTool.Highlighter);
                e.Handled = true;
            }
            else if (e.Key == Key.F5)
            {
                SetCurrentTool(DrawingTool.Blur);
                e.Handled = true;
            }
            else if (e.Key == Key.F6)
            {
                ToggleSpeechBalloonMode();
                e.Handled = true;
            }
            else if (e.Key == Key.X && Keyboard.Modifiers == (ModifierKeys.Alt | ModifierKeys.Shift))
            {
                ToggleSpeechBalloonMode();
                e.Handled = true;
            }
            else if (e.Key == Key.F7)
            {
                SetCurrentTool(DrawingTool.Crop);
                e.Handled = true;
            }
        }
        
        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }
        
        private bool IsAnyPopupOpen()
        {
            // Verificar si hay algún popup o ventana modal abierta
            return System.Windows.Application.Current.Windows
                .OfType<Window>()
                .Any(w => w != this && w.IsVisible && w.Owner == this);
        }
        
        private bool HandleEscapeHierarchy()
        {
            // 1. Si hay un crop overlay activo, cancelarlo
            if (cropOverlay != null)
            {
                CancelCrop();
                return true; // Indica que se manejó el Escape
            }
            
            // 2. Si está en modo speech balloon, salir del modo
            if (isSpeechBalloonMode)
            {
                ExitSpeechBalloonMode();
                return true; // Indica que se manejó el Escape
            }
            
            // 3. Si no hay herramientas activas, no manejar el Escape aquí
            // para que se cierre el editor
            return false;
        }
        
        private void UpdatePositionText(Point position)
        {
            PositionText.Text = $"X: {(int)position.X}, Y: {(int)position.Y}";
        }
        
        private void CancelCrop()
        {
            if (cropOverlay != null)
            {
                cropOverlay.Dispose();
                cropOverlay = null;
                SetCurrentTool(DrawingTool.None);
                UpdateStatus("Crop cancelado");
            }
        }
        
        private void ExitSpeechBalloonMode()
        {
            isSpeechBalloonMode = false;
            SetCurrentTool(DrawingTool.None);
            UpdateStatus("Modo speech balloon desactivado");
        }
        
        #endregion
    }
    
    #region Dialogo de Entrada de Texto
    
    public class TextInputDialog : Window
    {
        private TextBox textBox;
        
        public string Text => textBox?.Text ?? "";
        
        public TextInputDialog(string prompt)
        {
            Title = "Texto del Globo";
            Width = 450;
            Height = 250;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            var label = new Label
            {
                Content = prompt + "\n(Presiona Enter para nueva línea, Ctrl+Enter para crear)",
                Margin = new Thickness(10),
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 12
            };
            Grid.SetRow(label, 0);
            grid.Children.Add(label);
            
            textBox = new TextBox
            {
                Margin = new Thickness(10),
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromArgb(255, 33, 38, 45)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 48, 54, 61)),
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MinHeight = 100
            };
            Grid.SetRow(textBox, 1);
            grid.Children.Add(textBox);
            
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };
            
            var okButton = new Button
            {
                Content = "Crear (Ctrl+Enter)",
                Width = 120,
                Margin = new Thickness(5, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromArgb(255, 31, 111, 235)),
                Foreground = new SolidColorBrush(Colors.White)
            };
            okButton.Click += (s, e) => { DialogResult = true; Close(); };
            
            var cancelButton = new Button
            {
                Content = "Cancelar (Escape)",
                Width = 120,
                Margin = new Thickness(5, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromArgb(255, 33, 38, 45)),
                Foreground = new SolidColorBrush(Colors.White)
            };
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };
            
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);
            
            Content = grid;
            Background = new SolidColorBrush(Color.FromArgb(255, 13, 17, 23));
            
            textBox.Focus();
            textBox.SelectAll();
            
            // Ctrl+Enter para confirmar, Escape para cancelar
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    DialogResult = true;
                    Close();
                }
                else if (e.Key == Key.Escape)
                {
                    DialogResult = false;
                    Close();
                }
            };
        }
    }
    
    #endregion
}
