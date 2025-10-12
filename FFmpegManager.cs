using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Highlighter4
{
    /// <summary>
    /// Manages FFmpeg installation and verification
    /// </summary>
    public class FFmpegManager
    {
        private static bool _isCheckingFFmpeg = false;
        private static bool _ffmpegCheckCompleted = false;
        
        /// <summary>
        /// Checks if FFmpeg is installed and offers to install it if not found
        /// </summary>
        public static async Task CheckAndInstallFFmpegAsync()
        {
            if (_ffmpegCheckCompleted || _isCheckingFFmpeg)
                return;
                
            _isCheckingFFmpeg = true;
            
            try
            {
                await Task.Run(async () =>
                {
                    System.Diagnostics.Debug.WriteLine("Checking FFmpeg installation...");
                    
                    if (IsFFmpegInstalled())
                    {
                        System.Diagnostics.Debug.WriteLine("FFmpeg is already installed");
                        _ffmpegCheckCompleted = true;
                        return;
                    }
                    
                    System.Diagnostics.Debug.WriteLine("FFmpeg not found, prompting user for installation");
                    
                    // Ask user if they want to install FFmpeg
                    bool shouldInstall = false;
                    
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var result = MessageBox.Show(
                            "FFmpeg is not installed on your system.\n\n" +
                            "FFmpeg is required for advanced screen recording and video capture features.\n\n" +
                            "Would you like to install it automatically?\n\n" +
                            "This process will attempt to install FFmpeg using available package managers " +
                            "(winget, chocolatey, or scoop).",
                            "FFmpeg Installation Required",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                            
                        shouldInstall = (result == MessageBoxResult.Yes);
                    });
                    
                    if (shouldInstall)
                    {
                        await InstallFFmpegAsync();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("User declined FFmpeg installation");
                        
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            MessageBox.Show(
                                "FFmpeg installation was declined.\n\n" +
                                "Some advanced features may not be available.\n\n" +
                                "You can install FFmpeg manually from: https://ffmpeg.org/download.html",
                                "FFmpeg Not Installed",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        });
                    }
                    
                    _ffmpegCheckCompleted = true;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking FFmpeg: {ex.Message}");
            }
            finally
            {
                _isCheckingFFmpeg = false;
            }
        }
        
        /// <summary>
        /// Checks if FFmpeg is installed and accessible via PATH
        /// </summary>
        public static bool IsFFmpegInstalled()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(startInfo))
                {
                    process?.WaitForExit(3000); // Wait max 3 seconds
                    return process?.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Attempts to install FFmpeg using available package managers
        /// </summary>
        private static async Task InstallFFmpegAsync()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show(
                    "Attempting to install FFmpeg...\n\n" +
                    "This may take a few minutes. Please wait.",
                    "Installing FFmpeg",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });
            
            bool installed = false;
            
            // Try winget first
            System.Diagnostics.Debug.WriteLine("Attempting FFmpeg installation with winget...");
            installed = await TryInstallWithWingetAsync();
            
            if (!installed)
            {
                System.Diagnostics.Debug.WriteLine("Winget failed, trying chocolatey...");
                installed = await TryInstallWithChocoAsync();
            }
            
            if (!installed)
            {
                System.Diagnostics.Debug.WriteLine("Chocolatey failed, trying scoop...");
                installed = await TryInstallWithScoopAsync();
            }
            
            // Show result to user
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (installed)
                {
                    MessageBox.Show(
                        "FFmpeg has been installed successfully!\n\n" +
                        "You may need to restart the application for changes to take effect.",
                        "Installation Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    var result = MessageBox.Show(
                        "Automatic installation failed.\n\n" +
                        "Would you like to open the FFmpeg download page for manual installation?",
                        "Manual Installation Required",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                        
                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "https://ffmpeg.org/download.html",
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error opening browser: {ex.Message}");
                        }
                    }
                }
            });
        }
        
        /// <summary>
        /// Attempts to install FFmpeg using winget
        /// </summary>
        public static async Task<bool> TryInstallWithWingetAsync()
        {
            try
            {
                // Check if winget is available
                if (!await IsCommandAvailableAsync("winget"))
                {
                    System.Diagnostics.Debug.WriteLine("Winget is not available");
                    return false;
                }
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "install --id=Gyan.FFmpeg --exact --silent --accept-source-agreements --accept-package-agreements",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                        return false;
                        
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    
                    await Task.Run(() => process.WaitForExit(300000)); // Wait max 5 minutes
                    
                    System.Diagnostics.Debug.WriteLine($"Winget output: {output}");
                    System.Diagnostics.Debug.WriteLine($"Winget error: {error}");
                    
                    if (process.ExitCode == 0)
                    {
                        // Verify installation
                        await Task.Delay(2000); // Wait for PATH refresh
                        return IsFFmpegInstalled();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Winget installation error: {ex.Message}");
            }
            
            return false;
        }
        
        /// <summary>
        /// Attempts to install FFmpeg using Chocolatey
        /// </summary>
        public static async Task<bool> TryInstallWithChocoAsync()
        {
            try
            {
                // Check if choco is available
                if (!await IsCommandAvailableAsync("choco"))
                {
                    System.Diagnostics.Debug.WriteLine("Chocolatey is not available");
                    return false;
                }
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "choco",
                    Arguments = "install ffmpeg -y",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas" // Run as administrator
                };
                
                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                        return false;
                        
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    
                    await Task.Run(() => process.WaitForExit(300000)); // Wait max 5 minutes
                    
                    System.Diagnostics.Debug.WriteLine($"Choco output: {output}");
                    System.Diagnostics.Debug.WriteLine($"Choco error: {error}");
                    
                    if (process.ExitCode == 0)
                    {
                        // Verify installation
                        await Task.Delay(2000); // Wait for PATH refresh
                        return IsFFmpegInstalled();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Chocolatey installation error: {ex.Message}");
            }
            
            return false;
        }
        
        /// <summary>
        /// Attempts to install FFmpeg using Scoop
        /// </summary>
        public static async Task<bool> TryInstallWithScoopAsync()
        {
            try
            {
                // Check if scoop is available
                if (!await IsCommandAvailableAsync("scoop"))
                {
                    System.Diagnostics.Debug.WriteLine("Scoop is not available");
                    return false;
                }
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "scoop",
                    Arguments = "install ffmpeg",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                        return false;
                        
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    
                    await Task.Run(() => process.WaitForExit(300000)); // Wait max 5 minutes
                    
                    System.Diagnostics.Debug.WriteLine($"Scoop output: {output}");
                    System.Diagnostics.Debug.WriteLine($"Scoop error: {error}");
                    
                    if (process.ExitCode == 0)
                    {
                        // Verify installation
                        await Task.Delay(2000); // Wait for PATH refresh
                        return IsFFmpegInstalled();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Scoop installation error: {ex.Message}");
            }
            
            return false;
        }
        
        /// <summary>
        /// Checks if a command is available in PATH
        /// </summary>
        private static async Task<bool> IsCommandAvailableAsync(string command)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "where",
                    Arguments = command,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                        return false;
                        
                    await Task.Run(() => process.WaitForExit(3000));
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}

