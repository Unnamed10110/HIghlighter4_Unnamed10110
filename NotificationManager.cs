using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Highlighter4
{
    public class NotificationManager : IDisposable
    {
        private bool isDisposed = false;

        public NotificationManager()
        {
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            // Tray icon disabled - no icon will be shown
            // The NotificationManager will only show popup notifications
            System.Diagnostics.Debug.WriteLine("NotificationManager: Tray icon disabled");
        }

        private Icon CreateTrayIcon()
        {
            // Crear icono con forma de "H" rojo y blanco de 16x16
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                // Fondo transparente
                g.Clear(Color.Transparent);
                
                // Crear la letra "H" en rojo
                using (var brush = new SolidBrush(Color.Red))
                {
                    // Líneas verticales de la H
                    g.FillRectangle(brush, 2, 2, 2, 12);  // Línea izquierda
                    g.FillRectangle(brush, 12, 2, 2, 12); // Línea derecha
                    g.FillRectangle(brush, 4, 6, 8, 2);   // Línea horizontal
                }
            }

            return Icon.FromHandle(bitmap.GetHicon());
        }

        public void ShowImageNotification(string title, string message, Bitmap image, int durationSeconds = 6)
        {
            try
            {
                // Usar siempre la ventana solo imagen (más grande)
                var notificationWindow = new ImageOnlyNotificationWindow(image, durationSeconds, null);
                notificationWindow.Show();
            }
            catch (Exception ex)
            {
                // Si falla la notificación con miniatura, solo log el error
                System.Diagnostics.Debug.WriteLine($"Error showing image notification: {ex.Message}");
            }
        }

        public void ShowImageNotification(string title, string message, Bitmap image, string imagePath, int durationSeconds = 6)
        {
            try
            {
                // Usar siempre la ventana solo imagen (más grande) con ruta para abrir
                var notificationWindow = new ImageOnlyNotificationWindow(image, durationSeconds, imagePath);
                notificationWindow.Show();
            }
            catch (Exception ex)
            {
                // Si falla la notificación con miniatura, solo log el error
                System.Diagnostics.Debug.WriteLine($"Error showing image notification: {ex.Message}");
            }
        }

        public void ShowSimpleNotification(string title, string message, int durationSeconds = 3)
        {
            // Tray icon disabled - simple notifications won't be shown
            System.Diagnostics.Debug.WriteLine($"Simple notification (disabled): {title} - {message}");
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                // Nothing to dispose - tray icon is disabled
                isDisposed = true;
            }
        }
    }

    // Ventana de notificación personalizada que muestra la miniatura de la imagen
    public class ImageNotificationWindow : Form
    {
        private Timer autoCloseTimer;
        private bool isClosing = false;

        public ImageNotificationWindow(string title, string message, Bitmap image, int durationSeconds)
        {
            InitializeWindow(title, message, image, durationSeconds);
        }

        private void InitializeWindow(string title, string message, Bitmap image, int durationSeconds)
        {
            // Configuración básica de la ventana
            this.Text = title;
            this.Size = new Size(280, 140);
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            
            // Posicionar en la esquina inferior derecha de la pantalla
            var screen = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(screen.Right - this.Width - 10, screen.Bottom - this.Height - 10);
            
            // Configurar apariencia
            this.BackColor = Color.FromArgb(45, 45, 48); // Fondo oscuro
            this.Padding = new Padding(10);

            // Crear miniatura de la imagen más grande
            var thumbnail = CreateThumbnail(image, 120, 120);

            // Panel principal
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            // Panel de la imagen
            var imagePanel = new Panel
            {
                Size = new Size(120, 120),
                Location = new Point(10, 10),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // PictureBox para mostrar la miniatura
            var pictureBox = new PictureBox
            {
                Image = thumbnail,
                Size = new Size(118, 118),
                Location = new Point(1, 1),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            imagePanel.Controls.Add(pictureBox);

            // Label para el título
            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(140, 10),
                Size = new Size(130, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Label para el mensaje
            var messageLabel = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.LightGray,
                Location = new Point(140, 40),
                Size = new Size(130, 50),
                TextAlign = ContentAlignment.TopLeft
            };

            // Botón de cerrar
            var closeButton = new Button
            {
                Text = "×",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(25, 25),
                Location = new Point(this.Width - 35, 5),
                Cursor = Cursors.Hand
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => CloseWindow();

            // Agregar controles al panel principal
            mainPanel.Controls.Add(imagePanel);
            mainPanel.Controls.Add(titleLabel);
            mainPanel.Controls.Add(messageLabel);
            mainPanel.Controls.Add(closeButton);

            this.Controls.Add(mainPanel);

            // Configurar cierre automático
            autoCloseTimer = new Timer
            {
                Interval = durationSeconds * 1000
            };
            autoCloseTimer.Tick += (s, e) => CloseWindow();
            autoCloseTimer.Start();

            // Hacer la ventana clickeable para cerrar
            this.Click += (s, e) => CloseWindow();
            foreach (Control control in mainPanel.Controls)
            {
                control.Click += (s, e) => CloseWindow();
            }
        }

        private Bitmap CreateThumbnail(Bitmap originalImage, int maxWidth, int maxHeight)
        {
            // Calcular dimensiones manteniendo proporción
            double ratio = Math.Min((double)maxWidth / originalImage.Width, (double)maxHeight / originalImage.Height);
            int newWidth = (int)(originalImage.Width * ratio);
            int newHeight = (int)(originalImage.Height * ratio);

            // Crear miniatura
            var thumbnail = new Bitmap(newWidth, newHeight);
            using (var g = Graphics.FromImage(thumbnail))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                
                g.DrawImage(originalImage, 0, 0, newWidth, newHeight);
            }

            return thumbnail;
        }

        private void CloseWindow()
        {
            if (isClosing) return;
            isClosing = true;

            autoCloseTimer?.Stop();
            autoCloseTimer?.Dispose();

            // Animación de desvanecimiento
            var fadeTimer = new Timer { Interval = 50 };
            fadeTimer.Tick += (s, e) =>
            {
                if (this.Opacity > 0.1)
                {
                    this.Opacity -= 0.1;
                }
                else
                {
                    fadeTimer.Stop();
                    fadeTimer.Dispose();
                    this.Close();
                }
            };
            fadeTimer.Start();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.Opacity = 0;
            
            // Animación de aparición
            var fadeInTimer = new Timer { Interval = 50 };
            fadeInTimer.Tick += (s, ev) =>
            {
                if (this.Opacity < 1.0)
                {
                    this.Opacity += 0.1;
                }
                else
                {
                    fadeInTimer.Stop();
                    fadeInTimer.Dispose();
                }
            };
            fadeInTimer.Start();
        }
    }

    // Ventana de notificación que muestra solo la imagen grande sin texto
    public class ImageOnlyNotificationWindow : Form
    {
        private Timer autoCloseTimer;
        private bool isClosing = false;
        private string? imagePath;

        public ImageOnlyNotificationWindow(Bitmap image, int durationSeconds, string? imagePath = null)
        {
            this.imagePath = imagePath;
            InitializeWindow(image, durationSeconds);
        }

        private void InitializeWindow(Bitmap image, int durationSeconds)
        {
            // Configuración básica de la ventana
            this.Text = "";
            this.Size = new Size(300, 300);
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            
            // Posicionar en la esquina inferior derecha de la pantalla
            var screen = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(screen.Right - this.Width - 10, screen.Bottom - this.Height - 10);
            
            // Configurar apariencia
            this.BackColor = Color.Black; // Fondo negro
            this.Padding = new Padding(5);

            // Crear miniatura de la imagen que ocupe casi toda la ventana
            var thumbnail = CreateThumbnail(image, 290, 290);

            // PictureBox para mostrar la miniatura
            var pictureBox = new PictureBox
            {
                Image = thumbnail,
                Size = new Size(290, 290),
                Location = new Point(5, 5),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };

            // Agregar directamente el PictureBox a la ventana
            this.Controls.Add(pictureBox);

            // Configurar cierre automático
            autoCloseTimer = new Timer
            {
                Interval = durationSeconds * 1000
            };
            autoCloseTimer.Tick += (s, e) => CloseWindow();
            autoCloseTimer.Start();

            // Hacer la ventana clickeable para abrir imagen o cerrar
            this.Click += OnNotificationClick;
            pictureBox.Click += OnNotificationClick;
        }

        private Bitmap CreateThumbnail(Bitmap originalImage, int maxWidth, int maxHeight)
        {
            // Calcular dimensiones manteniendo proporción
            double ratio = Math.Min((double)maxWidth / originalImage.Width, (double)maxHeight / originalImage.Height);
            int newWidth = (int)(originalImage.Width * ratio);
            int newHeight = (int)(originalImage.Height * ratio);

            // Crear miniatura
            var thumbnail = new Bitmap(newWidth, newHeight);
            using (var g = Graphics.FromImage(thumbnail))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                
                g.DrawImage(originalImage, 0, 0, newWidth, newHeight);
            }

            return thumbnail;
        }

        private void CloseWindow()
        {
            if (isClosing) return;
            isClosing = true;

            autoCloseTimer?.Stop();
            autoCloseTimer?.Dispose();

            // Animación de desvanecimiento
            var fadeTimer = new Timer { Interval = 50 };
            fadeTimer.Tick += (s, e) =>
            {
                if (this.Opacity > 0.1)
                {
                    this.Opacity -= 0.1;
                }
                else
                {
                    fadeTimer.Stop();
                    fadeTimer.Dispose();
                    this.Close();
                }
            };
            fadeTimer.Start();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.Opacity = 0;
            
            // Animación de aparición
            var fadeInTimer = new Timer { Interval = 50 };
            fadeInTimer.Tick += (s, ev) =>
            {
                if (this.Opacity < 1.0)
                {
                    this.Opacity += 0.1;
                }
                else
                {
                    fadeInTimer.Stop();
                    fadeInTimer.Dispose();
                }
            };
            fadeInTimer.Start();
        }

        private void OnNotificationClick(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                try
                {
                    // Abrir la imagen en el visor predeterminado de Windows
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = imagePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error opening image: {ex.Message}");
                }
            }
            
            // Cerrar la notificación después de hacer clic
            CloseWindow();
        }
    }
}
