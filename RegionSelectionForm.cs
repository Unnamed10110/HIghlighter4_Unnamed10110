using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScrollCapture
{
    public partial class RegionSelectionForm : Form
    {
        public Rectangle SelectedRectangle { get; private set; }
        public IntPtr SelectedWindow { get; private set; }

        private Point startPoint;
        private Point endPoint;
        private bool isSelecting = false;
        private Bitmap backgroundImage;
        private Graphics backgroundGraphics;
        private IntPtr hookID = IntPtr.Zero;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        
        // Variables para la animación del contorno entrecortado
        private Timer animationTimer;
        private float dashOffset = 0f;

        public RegionSelectionForm()
        {
            InitializeForm();
            CaptureBackground();
            InitializeAnimationTimer();
            
            // Instalar hook de teclado global
            InstallKeyboardHook();
            
            // Asegurar que el formulario pueda recibir eventos de teclado
            this.Focus();
            this.BringToFront();
        }

        private void InitializeForm()
        {
            // Configurar formulario para captura de pantalla completa
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            TopMost = true;
            ShowInTaskbar = false;
            Cursor = Cursors.Cross;
            BackColor = Color.Black;
            Opacity = 0.3;
            KeyPreview = true; // Permitir que el formulario reciba eventos de teclado

            // No ocultar el cursor - mantenerlo visible para mejor UX
        }

        private void CaptureBackground()
        {
            // Capturar pantalla completa
            var screenBounds = Screen.PrimaryScreen.Bounds;
            backgroundImage = new Bitmap(screenBounds.Width, screenBounds.Height);
            backgroundGraphics = Graphics.FromImage(backgroundImage);
            backgroundGraphics.CopyFromScreen(screenBounds.X, screenBounds.Y, 0, 0, screenBounds.Size);
        }

        private void InitializeAnimationTimer()
        {
            animationTimer = new Timer();
            animationTimer.Interval = 50; // 20 FPS para animación suave
            animationTimer.Tick += (sender, e) => {
                dashOffset += 2f; // Velocidad de la animación
                if (dashOffset > 20f) dashOffset = 0f; // Reset cuando llega al final del patrón
                if (isSelecting)
                {
                    // Forzar redibujado del formulario
                    this.Invalidate();
                    this.Update();
                }
            };
            animationTimer.Start();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                startPoint = e.Location;
                endPoint = e.Location;
                isSelecting = true;
                
                // Reiniciar la animación
                dashOffset = 0f;
                
                // Asegurar que el timer esté funcionando
                if (!animationTimer.Enabled)
                {
                    animationTimer.Start();
                }
                
                Invalidate();
            }
            else if (e.Button == MouseButtons.Right)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isSelecting)
            {
                endPoint = e.Location;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && isSelecting)
            {
                endPoint = e.Location;
                isSelecting = false;

                // Calcular rectángulo seleccionado
                var rect = new Rectangle(
                    Math.Min(startPoint.X, endPoint.X),
                    Math.Min(startPoint.Y, endPoint.Y),
                    Math.Abs(endPoint.X - startPoint.X),
                    Math.Abs(endPoint.Y - startPoint.Y)
                );

                if (rect.Width > 10 && rect.Height > 10)
                {
                    SelectedRectangle = rect;
                    SelectedWindow = GetWindowAtPoint(rect.Location);
                    DialogResult = DialogResult.OK;
                }
                else
                {
                    DialogResult = DialogResult.Cancel;
                }

                Close();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else if (e.KeyCode == Keys.Enter && isSelecting)
            {
                e.Handled = true;
                // Confirmar selección con Enter
                OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, endPoint.X, endPoint.Y, 0));
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Dibujar fondo
            e.Graphics.DrawImage(backgroundImage, 0, 0);

            if (isSelecting)
            {
                // Dibujar rectángulo de selección
                var rect = new Rectangle(
                    Math.Min(startPoint.X, endPoint.X),
                    Math.Min(startPoint.Y, endPoint.Y),
                    Math.Abs(endPoint.X - startPoint.X),
                    Math.Abs(endPoint.Y - startPoint.Y)
                );

                // Dibujar fondo semi-transparente
                using (var brush = new SolidBrush(Color.FromArgb(30, Color.White)))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }

                // Dibujar contorno blanco entrecortado animado
                using (var pen = new Pen(Color.White, 2)) // Grosor de 2px para mejor visibilidad
                {
                    // Configurar patrón de línea entrecortada
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
                    pen.DashPattern = new float[] { 8f, 4f }; // Líneas de 8px, espacios de 4px
                    pen.DashOffset = dashOffset; // Offset animado
                    
                    // Asegurar que el pen esté configurado correctamente
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    
                    e.Graphics.DrawRectangle(pen, rect);
                }

                // Mostrar dimensiones
                string sizeText = $"{rect.Width} x {rect.Height}";
                using (var font = new Font("Arial", 12, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                using (var outlineBrush = new SolidBrush(Color.Black))
                {
                    // Dibujar contorno negro para el texto
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            if (i != 0 || j != 0)
                            {
                                e.Graphics.DrawString(sizeText, font, outlineBrush, rect.X + i, rect.Y - 25 + j);
                            }
                        }
                    }
                    
                    // Dibujar texto blanco
                    e.Graphics.DrawString(sizeText, font, brush, rect.X, rect.Y - 25);
                }
            }
            else
            {
                // Mostrar instrucciones cuando no se está seleccionando
                string instructions = "Arrastra para seleccionar región | ESC para cancelar | Enter para confirmar";
                using (var font = new Font("Arial", 14, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.Yellow))
                using (var outlineBrush = new SolidBrush(Color.Black))
                {
                    var textSize = e.Graphics.MeasureString(instructions, font);
                    var x = (Width - textSize.Width) / 2;
                    var y = Height / 2 - 50;
                    
                    // Dibujar contorno negro
                    for (int i = -2; i <= 2; i++)
                    {
                        for (int j = -2; j <= 2; j++)
                        {
                            if (i != 0 || j != 0)
                            {
                                e.Graphics.DrawString(instructions, font, outlineBrush, x + i, y + j);
                            }
                        }
                    }
                    
                    // Dibujar texto amarillo
                    e.Graphics.DrawString(instructions, font, brush, x, y);
                }
            }
        }

        private IntPtr GetWindowAtPoint(Point point)
        {
            return NativeMethods.WindowFromPoint(point);
        }

        // Declaraciones de Windows API para el hook de teclado
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private void InstallKeyboardHook()
        {
            if (hookID == IntPtr.Zero)
            {
                hookID = SetWindowsHookEx(WH_KEYBOARD_LL, KeyboardHookProc, GetModuleHandle(null), 0);
            }
        }

        private void UninstallKeyboardHook()
        {
            if (hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(hookID);
                hookID = IntPtr.Zero;
            }
        }

        private IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (vkCode == (int)Keys.Escape)
                {
                    // Cerrar el formulario desde el hook
                    this.Invoke(new Action(() => {
                        DialogResult = DialogResult.Cancel;
                        Close();
                    }));
                    return (IntPtr)1; // Consumir el evento
                }
            }
            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Detener y limpiar timer de animación
            animationTimer?.Stop();
            animationTimer?.Dispose();
            
            // Desinstalar hook de teclado
            UninstallKeyboardHook();
            
            // Limpiar recursos
            backgroundGraphics?.Dispose();
            backgroundImage?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
