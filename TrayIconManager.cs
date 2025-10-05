using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScrollCapture
{
    public class TrayIconManager : IDisposable
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip contextMenu;
        private ScrollCaptureManager captureManager;

        public TrayIconManager(ScrollCaptureManager manager)
        {
            captureManager = manager;
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            // Crear menú contextual
            contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Iniciar Captura", null, OnStartCapture);
            contextMenu.Items.Add("Detener Captura", null, OnStopCapture);
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Salir", null, OnExit);

            // Crear icono de bandeja
            trayIcon = new NotifyIcon()
            {
                Icon = CreateTrayIcon(),
                ContextMenuStrip = contextMenu,
                Visible = true,
                Text = "Scroll Capture - Presiona Ctrl+NumPad0"
            };

            // Eventos
            trayIcon.DoubleClick += OnTrayIconDoubleClick;
        }

        private Icon CreateTrayIcon()
        {
            try
            {
                // Use system warning icon (yellow exclamation mark)
                return SystemIcons.Warning;
            }
            catch (Exception)
            {
                // Fallback: use application icon
                return SystemIcons.Application;
            }
        }

        public void ShowNotification(string title, string text)
        {
            trayIcon.ShowBalloonTip(3000, title, text, ToolTipIcon.Info);
        }

        private void OnTrayIconDoubleClick(object sender, EventArgs e)
        {
            if (captureManager.IsCapturing)
            {
                captureManager.StopCapture();
            }
            else
            {
                captureManager.StartCapture();
            }
        }

        private void OnStartCapture(object sender, EventArgs e)
        {
            captureManager.StartCapture();
        }

        private void OnStopCapture(object sender, EventArgs e)
        {
            captureManager.StopCapture();
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        public void Dispose()
        {
            trayIcon?.Dispose();
            contextMenu?.Dispose();
        }
    }
}
