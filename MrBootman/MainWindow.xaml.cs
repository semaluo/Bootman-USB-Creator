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
using log4net;
using log4net.Config;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.IO;
using DiscUtils.Iso9660;
using System.Windows.Interop;

namespace MrBootman
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Class properties and variables
        public Dictionary<string, DiskPartDriveMap> DriveMap { get; set; }

        private BackgroundWorker BGWorker;

        private const int MI_DEFAULT_HEIGHT_MAIN_FORM = 505;
        private const int MI_DEFAULT_HEIGHT_OUTPUT_LOG = 265;
        private const string MS_TEMP_WORK_AREA = "TempWorkArea";
        private const string MS_SYSTEM_VOLUME_INFORMATION = "System Volume Information";
        private const string MS_CREATE = "CREATE";
        private const string MS_CANCEL = "CANCEL";

        private DateTime dtRemoveUsbDriveTStamp = new DateTime(1900, 1, 1, 0, 0, 0, 0);
        private DateTime dtAddUsbDriveTStamp = new DateTime(1900, 1, 1, 0, 0, 0, 0);

        private System.Threading.Thread WorkerThread { get; set; }

        private bool SkipRefreshAllDriveComboBoxes { get; set; }

        #endregion Class properties and variables

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();

            WorkerThread = null;

            BGWorker = new System.ComponentModel.BackgroundWorker();
            this.BGWorker.DoWork
                += new System.ComponentModel.DoWorkEventHandler(this.BGWorker_DoWork);

            BGWorker.RunWorkerCompleted +=
                new RunWorkerCompletedEventHandler(BGWorker_RunWorkerCompleted);

            try
            {
                // Configure Logging
                XmlConfigurator.Configure();

                // Set Application Assembly version in log4net for logging purposes
                var oVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                log4net.ThreadContext.Properties["AppAsmVer"]
                    = oVersion.ToString();

                // Show logging
                if (Properties.Settings.Default.ShowLog)
                {
                    BootmanLog.LogToDisplay = true;
                    chkBtnLog.IsChecked = true;
                }
                else
                {
                    BootmanLog.LogToDisplay = false;
                    chkBtnLog.IsChecked = false;
                }

                // Set destination Drive Letters on all the combo boxes
                RefreshAllDriveComboBoxes();
            }
            catch (Exception oEX)
            {
                BootmanLog.LogError(txtLogOutput, "Error: " + oEX.Message);
            } 
        }
        #endregion Constructor

        #region Background Worker Methods
        /**************************************************************************************************************
         * Background Worker Methods
         *************************************************************************************************************/
        private void RunBGWorker(Object inout_oDataRec)
        {
            try
            {
                // Start USBBootDrivBGWorker
                if (BGWorker.IsBusy)
                {
                    this.BGWorker.DoWork
                        -= new System.ComponentModel.DoWorkEventHandler(this.BGWorker_DoWork);
                    BGWorker.Dispose();

                    BGWorker = new System.ComponentModel.BackgroundWorker();
                    this.BGWorker.DoWork
                        += new System.ComponentModel.DoWorkEventHandler(this.BGWorker_DoWork);

                    BGWorker.RunWorkerAsync(inout_oDataRec);
                }
                else
                {
                    BGWorker.RunWorkerAsync(inout_oDataRec);
                }

                // Show progress bar
                lblStatusDisplay.Content = "Starting";
                progressBarBootman.IsIndeterminate = true;
            }
            catch (Exception oEx)
            {
                BootmanLog.LogError(txtLogOutput, "Error: " + oEx.Message);
            }
        }

        private void BGWorker_DoWork(object sender
            , DoWorkEventArgs e)
        {
            try
            {
                // Get the current 
                WorkerThread = System.Threading.Thread.CurrentThread;

                // Update the create button 
                Application.Current.Dispatcher.BeginInvoke(
                     System.Windows.Threading.DispatcherPriority.Background,
                     new Action(() => this.btnCreate.Content = MS_CANCEL));

                BackgroundWorker oHelperBW = sender as BackgroundWorker;

                string sTypeName = e.Argument.GetType().Name;
                switch (sTypeName)
	            {
                    case "ImageToDriveDataRec":
                        ImageToDriveDataRec oImageToDriveDataRec = (ImageToDriveDataRec)e.Argument;

                        e.Result = BackgroundProcessImageToDrive(oHelperBW
                            , oImageToDriveDataRec);

                        break;

                    case "DriveToDriveDataRec":
                        DriveToDriveDataRec oDriveToDriveDataRec = (DriveToDriveDataRec)e.Argument;

                        e.Result = BackgroundProcessDriveToDrive(oHelperBW
                            , oDriveToDriveDataRec);

                        break;

                    case "CreateImageDataRec":
                        CreateImageDataRec oCreateImageDataRec = (CreateImageDataRec)e.Argument;

                        e.Result = BackgroundProcessCreateImage(oHelperBW
                            , oCreateImageDataRec);

                        break;

		            default:
                        break;
	            }

                if (oHelperBW.CancellationPending)
                {
                    e.Cancel = true;
                }
            }
            catch (System.Threading.ThreadAbortException oExAbort)
            {
                e.Cancel = true; //We must set Cancel property to true!
                System.Threading.Thread.ResetAbort(); //Prevents ThreadAbortException propagation
                BootmanLog.LogInfo(txtLogOutput
                    , "Background Worker - Abort: " + oExAbort.Message);
            }

            catch (Exception oEx)
            {
                e.Cancel = true; //We must set Cancel property to true!
                BootmanLog.LogError(txtLogOutput
                    , "Background Worker Exception: " + oEx.Message);
            }
        }

        private void BGWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((e.Cancelled == true))
            {
                //this.tbProgress.Text = "Canceled!";
                BootmanLog.LogInfo(txtLogOutput
                    , "Background Work Events were canceled.");
            }
            else if (!(e.Error == null))
            {
                BootmanLog.LogError(txtLogOutput, e.Error.Message);
            }
            else
            {
               BootmanLog.LogInfo(txtLogOutput
                   , "Background Worker Events were completed.");
            }

            // Cleanup worker thread and show button create button as create
            WorkerThread = null;
            btnCreate.IsEnabled = true;
            btnCreate.Content = MS_CREATE;

            // Refesh Drive information
            RefreshAllDriveComboBoxes();

            // Hide progress bar
            progressBarBootman.IsIndeterminate = false;
            lblStatusDisplay.Content = "Completed";
        }

        // Background worker logic
        private bool BackgroundProcessImageToDrive(BackgroundWorker oBW
            , ImageToDriveDataRec in_oDriveWorkDataRec)
        {
            // Set Application Assembly version in log4net for logging purposes
            // This is by thread context so need to be set for each thread
            var oVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            log4net.ThreadContext.Properties["AppAsmVer"]
                = oVersion.ToString();

            bool bResultFormatBootReady = false;
            if (in_oDriveWorkDataRec.IsFormatBootReady)
            {
                // Update Status
                Application.Current.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() => this.lblStatusDisplay.Content = "Formatting Drive"));

                String sDiskPartWorkPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory
                    , MS_TEMP_WORK_AREA);

                // Clean up existing files in temp work area
                CleanupTempWorkArea(sDiskPartWorkPath);

                // Lookup drive number
                int iDriveNumber = DriveMap[in_oDriveWorkDataRec.DriveLetterDestination].ParentDriveNumber;

                // Create DiskPart Work file
                DiskpartUtils oDiskpartUtils = new DiskpartUtils(txtLogOutput);
                bool bResultDiskPartWorkFile = oDiskpartUtils.CreateDiskPartWorkFile(in_oDriveWorkDataRec.DriveLetterDestination
                    , iDriveNumber
                    , sDiskPartWorkPath);

                if (bResultDiskPartWorkFile)
                {
                    BootmanLog.LogInfo(txtLogOutput, "Created Diskpart workfile.");

                    // execute diskpart
                    string sApplicationTargetDir = sDiskPartWorkPath;
                    string sApplicationName = "diskpart.exe";
                    string sArguments = " /s " + "DiskPartWorkFile.txt";

                    bResultFormatBootReady =  RunAppProcess(sApplicationTargetDir
                        , sApplicationName
                        , sArguments
                        , false
                        , false);

                    if(bResultFormatBootReady)
                    {
                        // Put in wait  before moving on to next 
                        System.Threading.Thread.Sleep(1000);
                        BootmanLog.LogInfo(txtLogOutput
                            , "Reloading Drive map.");

                        // LoadDrives
                        LoadDrives(true);
                    }
                }
                else
                {
                    BootmanLog.LogInfo(txtLogOutput
                        , "WARNING: Diskpart actions were not completed.");
                }
            }

            // Check to make sure format completed if required
            if
            (
                (in_oDriveWorkDataRec.IsFormatBootReady && bResultFormatBootReady)
                || (!in_oDriveWorkDataRec.IsFormatBootReady)
            )
            {
                // Update Status
                Application.Current.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() => this.lblStatusDisplay.Content = "Extracting ISO"));

                // Extract ISO image data
                if (in_oDriveWorkDataRec.IsbExtractImageDirectToDrive)
                {
                    // Lookup drive number
                    string sDrivePath = DriveMap[in_oDriveWorkDataRec.DriveLetterDestination].Path;

                    BootmanLog.LogInfo(txtLogOutput
                        , "Begin ISO Extract to Removable Drive.");
                    bool bResultExtractISO = false;
                    if (in_oDriveWorkDataRec.IsFormatBootReady)
                    {
                        // Extract files to Drive root for Format/Boot ready 
                        ISOUtils oISOUtils = new ISOUtils(txtLogOutput);
                        bResultExtractISO = oISOUtils.ExtractISO(in_oDriveWorkDataRec.ImagePathAndName
                            , sDrivePath
                            , false);
                    }
                    else
                    {
                        // Extract files to subfolder in root of drive when no format occurs
                        // Subfolder will be name the same as iso file
                        ISOUtils oISOUtils = new ISOUtils(txtLogOutput);
                        oISOUtils.ExtractISO(in_oDriveWorkDataRec.ImagePathAndName
                            , sDrivePath
                            , true);
                    }

                    if (bResultExtractISO)
                    {
                        BootmanLog.LogInfo(txtLogOutput
                            , "Completed ISO Extract to Removable Drive.");
                        return true;
                    }
                    else
                    {
                        BootmanLog.LogInfo(txtLogOutput
                            , "Failed to Complete ISO Extract to Removable Drive.");
                        return false;
                    }
                }
                else
                {
                    String sDiskPartWorkPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory
                        , MS_TEMP_WORK_AREA);

                    ISOUtils oISOUtils = new ISOUtils(txtLogOutput);
                    bool bResultExtractISO = oISOUtils.ExtractISO(in_oDriveWorkDataRec.ImagePathAndName
                        , sDiskPartWorkPath
                        , true);


                    if (!bResultExtractISO)
                    {
                        BootmanLog.LogInfo(txtLogOutput
                            , "Failed to Complete ISO Extract to Local Drive Temp Work Area.");
                        return false;
                    }
                    else
                    {
                        BootmanLog.LogInfo(txtLogOutput
                            , "Completed ISO Extract to Local Drive Temp Work Area.");

                        BootmanLog.LogInfo(txtLogOutput
                            , "Begin copy to Removable Drive.");

                        // To copy a file to another location
                        string sDrivePath = DriveMap[in_oDriveWorkDataRec.DriveLetterDestination].Path;
                        string sRootFileSourceDirectory = System.IO.Path.Combine(sDiskPartWorkPath
                            , System.IO.Path.GetFileNameWithoutExtension(in_oDriveWorkDataRec.ImagePathAndName));

                        bool bResultCopy = CopyFilesToDestinationDrive(sRootFileSourceDirectory
                            , ""
                            , sDrivePath);

                        if (bResultCopy)
                        {
                            BootmanLog.LogInfo(txtLogOutput
                            , "Completed copy to Removable Drive.");
                        }

                        return true;
                    }
                }
            }
            else
            {
                BootmanLog.LogInfo(txtLogOutput
                    , "Format/Make Boot Ready action did not complete.");

                return false;
            }
        }

        private bool BackgroundProcessDriveToDrive(BackgroundWorker oBW
            , DriveToDriveDataRec in_oDriveWorkDataRec)
        {
            // Set Application Assembly version in log4net for logging purposes
            // This is by thread context so need to be set for each thread
            var oVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            log4net.ThreadContext.Properties["AppAsmVer"]
                = oVersion.ToString();

            bool bResultFormatBootReady = false;
            if (in_oDriveWorkDataRec.IsFormatBootReady)
            {

                // Update Status
                Application.Current.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() => this.lblStatusDisplay.Content = "Formatting Drive"));

                String sDiskPartWorkPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory
                    , MS_TEMP_WORK_AREA);

                // Clean up existing files in temp work area
                CleanupTempWorkArea(sDiskPartWorkPath);

                // Lookup drive number
                int iDriveNumber = DriveMap[in_oDriveWorkDataRec.DestinationDriveLetter].ParentDriveNumber;

                // Create DiskPart Work file
                DiskpartUtils oDiskpartUtils = new DiskpartUtils(txtLogOutput);
                bool bResultDiskPartWorkFile = oDiskpartUtils.CreateDiskPartWorkFile(in_oDriveWorkDataRec.DestinationDriveLetter
                    , iDriveNumber
                    , sDiskPartWorkPath);

                if (bResultDiskPartWorkFile)
                {
                    BootmanLog.LogInfo(txtLogOutput, "Created Diskpart workfile.");

                    // execute diskpart
                    string sApplicationTargetDir = sDiskPartWorkPath;
                    string sApplicationName = "diskpart.exe";
                    string sArguments = " /s " + "DiskPartWorkFile.txt";

                    bResultFormatBootReady = RunAppProcess(sApplicationTargetDir
                        , sApplicationName
                        , sArguments
                        , false
                        , false);

                    if (bResultFormatBootReady)
                    {
                        // Put in wait  before moving on to next 
                        System.Threading.Thread.Sleep(1000);
                        BootmanLog.LogInfo(txtLogOutput, "Reloading Drive map.");

                        // LoadDrives
                        LoadDrives(true);
                    }
                }
                else
                {
                    BootmanLog.LogInfo(txtLogOutput
                        , "WARNING: Diskpart actions were not completed.");
                }
            }

            // Check to make sure format completed if required
            if
            (
                (in_oDriveWorkDataRec.IsFormatBootReady && bResultFormatBootReady)
                || (!in_oDriveWorkDataRec.IsFormatBootReady)
            )
            {
                // Update Status
                Application.Current.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() => this.lblStatusDisplay.Content = "Copying Files"));

                BootmanLog.LogInfo(txtLogOutput
                    , "Begin copy to Removable Drive.");

                bool bResultXCopy = false;

                // To copy a file to another location
                string sSourceDrivePath = DriveMap[in_oDriveWorkDataRec.SourceDriveLetter].Path;
                string sDestinationDrivePath = DriveMap[in_oDriveWorkDataRec.DestinationDriveLetter].Path;

                if (in_oDriveWorkDataRec.UseXCopyForFileCopy)
                {
                    // Execute XCopy
                    string sAppTargetDirXCopy = "";
                    string sAppNameXCopy = "XCopy.exe";
                    string sArgumentsXCopy = sSourceDrivePath + " " + sDestinationDrivePath + " /e /y /I";

                    bResultXCopy = RunAppProcess(sAppTargetDirXCopy
                        , sAppNameXCopy
                        , sArgumentsXCopy
                        , false
                        , false);

                    if (bResultXCopy)
                    {
                        BootmanLog.LogInfo(txtLogOutput
                            , "Completed XCopy to Removable Drive.");

                        return true;
                    }
                    else
                    {
                        BootmanLog.LogInfo(txtLogOutput
                        , "Failed to Complete XCopy to Removable Drive.");

                        return false;
                    }
                }
                else
                {
                    // Standard reccursive copy routine
                    bool bResultCopy = CopyFilesToDestinationDrive(sSourceDrivePath
                        , ""
                        , sDestinationDrivePath);

                    if (bResultCopy)
                    {
                        BootmanLog.LogInfo(txtLogOutput
                            , "Completed copy to Removable Drive.");
                        return true;
                    }
                    else
                    {
                        BootmanLog.LogInfo(txtLogOutput
                            , "Failed to Complete copy to Removable Drive.");
                        return false;
                    }
                }
            }
            else
            {
                BootmanLog.LogInfo(txtLogOutput
                    , "Format/Make Boot Ready action did not complete.");

                return false;
            }
        }

        private bool BackgroundProcessCreateImage(BackgroundWorker oBW
            , CreateImageDataRec in_oDriveWorkDataRec)
        {
            // Set Application Assembly version in log4net for logging purposes
            // This is by thread context so need to be set for each thread
            var oVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            log4net.ThreadContext.Properties["AppAsmVer"]
                = oVersion.ToString();

            if (in_oDriveWorkDataRec.IsImageBootReady)
            {
                // DO FUTURE WORK HERE
            }

            BootmanLog.LogInfo(txtLogOutput
                , "Starting Create Image.");

            ISOUtils oISOUtils = new ISOUtils(txtLogOutput);
            bool bCreateImageResult = oISOUtils.CreateISO9660(in_oDriveWorkDataRec.DestinationFile
                , in_oDriveWorkDataRec.SourceDirectory);
            if (bCreateImageResult)
            {
                BootmanLog.LogInfo(txtLogOutput
                , "Completed Create Image.");
                return true;
            }
            else
            {
                BootmanLog.LogInfo(txtLogOutput
                , "Failed to Create Image.");
                return false;
            }
        }

        public bool RunAppProcess(string in_sApplicationTargetDir
            , string in_sApplicationName
            , string in_sArguments
            , bool in_bShowBatchConsole
            , bool in_bWireupOutputAndErrorEvents)
        {
            Process oProc = null;
            int iExitCode = 0;
           
            try
            {
                // Build new process to execute
                ProcessStartInfo oProcStartInfo = new ProcessStartInfo();

                oProcStartInfo.Verb = "runas";

                oProcStartInfo.WorkingDirectory = in_sApplicationTargetDir;
                oProcStartInfo.FileName = in_sApplicationName;
                oProcStartInfo.Arguments = in_sArguments;

                BootmanLog.LogInfo(txtLogOutput
                    , "CMD: " + in_sApplicationTargetDir + "\\" + in_sApplicationName + " " + in_sArguments);

                if (in_bShowBatchConsole)
                {
                    oProcStartInfo.UseShellExecute = false;

                    // Start Execution of process
                    using (oProc = Process.Start(oProcStartInfo))
                    {
                        try
                        {
                            oProc.WaitForExit();

                            BootmanLog.LogInfo(txtLogOutput
                                , "CMD Execution completed");

                            return true;
                        }
                        catch (System.Threading.ThreadAbortException oExAbort)
                        {
                            // Stop the process and clean everything up;
                            if (oProc != null)
                            {
                                oProc.Kill();
                                oProc.Close();
                                oProc.Dispose();
                            }
                            System.Threading.Thread.ResetAbort(); //Prevents ThreadAbortException propagation
                            BootmanLog.LogInfo(txtLogOutput, "Process Aborted for Application : " + in_sApplicationName
                                + "\r\n" + ">> Error Message: " + oExAbort.Message);
                            return false;
                        }

                        catch (Exception oEx)
                        {
                            // Stop the process and clean everything up;
                            if (oProc != null)
                            {
                                oProc.Kill();
                                oProc.Close();
                                oProc.Dispose();
                            }
                            BootmanLog.LogInfo(txtLogOutput
                                , "Process Exception for Application : " + in_sApplicationName
                                + "\r\n" + ">> Error Message: " + oEx.Message);

                            return false;
                        }
                    }
                }
                else
                {
                    oProcStartInfo.CreateNoWindow = true;
                    oProcStartInfo.UseShellExecute = false;
                    oProcStartInfo.RedirectStandardOutput = true;
                    oProcStartInfo.RedirectStandardError = true;  
                    oProcStartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                    // Start Execution of process
                    using (oProc = Process.Start(oProcStartInfo))
                    {
                        try
                        {
                            if (in_bWireupOutputAndErrorEvents)
                            {
                                oProc.ErrorDataReceived += this.ProcErrorDataHandler;
                                oProc.OutputDataReceived += this.ProcOutputDataHandler;
                            }
                            using (StreamReader reader = oProc.StandardOutput)
                            {
                                string sResult = reader.ReadToEnd();

                                sResult = sResult.Replace("\n\n", "\r\n");
                                sResult = sResult.Replace("\r\r", "\r\n");
                                sResult = sResult.Replace("    0 percent completed\r", "");
                                sResult = sResult.Replace("\r\n\r\n", "\r\n");
                                sResult = sResult.Replace("  100 percent completed", "Format Drive - 100 percent completed");
                                BootmanLog.LogInfo(txtLogOutput
                                    , "\r\n***** Command Process Standard Output Begin *****");
                                BootmanLog.LogInfo(txtLogOutput
                                    , sResult);
                                BootmanLog.LogInfo(txtLogOutput
                                    , "***** Command Process Standard Output End *****\r\n");
                            }

                            string sResultError = "";
                            using (StreamReader oReaderStandardError = oProc.StandardError)
                            {
                                sResultError = oReaderStandardError.ReadToEnd();

                                if (sResultError.Length > 0)
                                {
                                    BootmanLog.LogInfo(txtLogOutput
                                        , "\r\n***** Command Process Error Begin *****");
                                    BootmanLog.LogInfo(txtLogOutput
                                        , sResultError);
                                    BootmanLog.LogInfo(txtLogOutput
                                        , "***** Command Process Error End *****\r\n");
                                }
                            }

                            oProc.WaitForExit();
                            iExitCode = oProc.ExitCode;

                            if (sResultError.Length > 0 || iExitCode != 0)
                            {
                                BootmanLog.LogInfo(txtLogOutput
                                    , "CMD Execution completed with with errors.");
                                return false;
                            }
                            else
                            {
                                BootmanLog.LogInfo(txtLogOutput
                                    , "CMD Execution completed without any errors.");

                                return true;
                            }
                        }
                        catch (System.Threading.ThreadAbortException oExAbort)
                        {
                            // Stop the process and clean everything up;
                            if (oProc != null)
                            {
                                oProc.Kill();
                                oProc.Close();
                                oProc.Dispose();
                            }
                            System.Threading.Thread.ResetAbort(); //Prevents ThreadAbortException propagation
                            BootmanLog.LogInfo(txtLogOutput, "Process Aborted for Application : " + in_sApplicationName
                                + "\r\n" + ">> Error Message: " + oExAbort.Message);
                            return false;
                        }
                        catch (Exception oEx)
                        {
                            // Stop the process and clean everything up;
                            if (oProc != null)
                            {
                                oProc.Kill();
                                oProc.Close();
                                oProc.Dispose();
                            }
                            BootmanLog.LogInfo(txtLogOutput, "Process Exception for Application : " + in_sApplicationName
                                + "\r\n" + ">> Error Message: " + oEx.Message);

                            return false;
                        }
                        finally
                        {
                            if (in_bWireupOutputAndErrorEvents)
                            {
                                oProc.ErrorDataReceived -= this.ProcErrorDataHandler;
                                oProc.OutputDataReceived -= this.ProcOutputDataHandler;
                            }
                        }
                    } // using for process
                }
            }

            catch (Exception oEx)
            {
                BootmanLog.LogInfo(txtLogOutput, "Process Exception for Application : " + in_sApplicationName
                    + "\r\n" + ">> Error Message: " + oEx.Message);

                return false;
            }
        }

        private void ProcErrorDataHandler(object sender, DataReceivedEventArgs args)
        {
            string sMessage = args.Data;
            BootmanLog.LogInfo(txtLogOutput
                , sMessage);
        }

        private void ProcOutputDataHandler(object sender, DataReceivedEventArgs args)
        {
            string sMessage = args.Data;
            BootmanLog.LogInfo(txtLogOutput
            , sMessage);
        }

        #endregion Background Worker Methods

        #region Tab Control - Tab 1 Methods
        /**************************************************************************************************************
         * Tab Control - Tab 1 Methods
         *************************************************************************************************************/
        private void btnTab1Browse_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlgOpenFile = new Microsoft.Win32.OpenFileDialog();

            dlgOpenFile.InitialDirectory = "c:\\";
            dlgOpenFile.Filter = "iso files (*.iso)|*.iso";
            dlgOpenFile.FilterIndex = 2;
            dlgOpenFile.RestoreDirectory = true;

            Nullable<bool> bResult = dlgOpenFile.ShowDialog();

            if (bResult == true)
            {
                try
                {
                    txtTab1ImageFile.Text = dlgOpenFile.FileName;

                }
                catch (Exception oEx)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + oEx.Message);
                }
            }
        }

        private void ImageToDriveCreate()
        {
            string sDriveLetterDestination = cboTab1DestinationDriveLetter.SelectedItem.ToString();
            string sImagePathAndName = txtTab1ImageFile.Text;
            bool bExtractImageDirectToDrive = (chkTab1ExtractImageDirectToDrive.IsChecked == true);
            bool bFormatBootReady = (chkTab1FormatBootReady.IsChecked == true);

            if (sDriveLetterDestination.Trim().Length > 0
                && sImagePathAndName.Trim().Length > 0)
            {
                ImageToDriveDataRec oDriveWorkDataRec
                    = new ImageToDriveDataRec();

                oDriveWorkDataRec.ImagePathAndName = sImagePathAndName;
                oDriveWorkDataRec.DriveLetterDestination = sDriveLetterDestination;
                oDriveWorkDataRec.IsbExtractImageDirectToDrive = bExtractImageDirectToDrive;
                oDriveWorkDataRec.IsFormatBootReady = bFormatBootReady;

                RunBGWorker(oDriveWorkDataRec);

                BootmanLog.LogInfo(txtLogOutput
                    , "Starting Drive Creation.");
            }
            else
            {
                CustomMessageBox frmCustomMessageBox = new CustomMessageBox();
                frmCustomMessageBox.Owner = this;
                frmCustomMessageBox.lblMessage.Text = "Please Select Image and Drive.";
                frmCustomMessageBox.Show();
            }
        }

        private void chkTab1FormatBootReady_Unchecked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                System.Threading.Thread.Sleep(1000);
                RefreshAllDriveComboBoxes();
            }
        }

        private void chkTab1FormatBootReady_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                System.Threading.Thread.Sleep(1000);
                RefreshAllDriveComboBoxes();
            }
        }

        private void chkTab1ExtractImageDirectToDrive_Loaded(object sender, RoutedEventArgs e)
        {
            CheckBox oCheckBox = sender as CheckBox;
            oCheckBox.IsChecked = true;
        }
        #endregion Tab Control - Tab 1 Methods

        #region Tab Control - Tab 2 Methods
        /**************************************************************************************************************
         * Tab Control - Tab 2 Methods
         *************************************************************************************************************/
        private void DriveToDriveCreate()
        {
            string sDestinationDriveLetter = "";
            if (cboTab2DestinationDriveLetter.SelectedItem != null)
            {
                sDestinationDriveLetter = cboTab2DestinationDriveLetter.SelectedItem.ToString().Trim();
            }
            string sSourceDriveLetter = "";
            if (cboTab2SourceDriveLetter.SelectedItem != null)
            {
                sSourceDriveLetter = cboTab2SourceDriveLetter.SelectedItem.ToString().Trim();
            }
            bool bFormatBootReady = (chkTab2FormatBootReady.IsChecked == true);
            bool bUseXCopy = (chkTab2UseXCopy.IsChecked == true);

            if (sDestinationDriveLetter.Length > 0
                && sSourceDriveLetter.Length > 0)
            {
                if (sSourceDriveLetter != sDestinationDriveLetter)
                {
                    // Check Size
                    long lSizeSourceUsed = DriveMap[sSourceDriveLetter].Info.TotalSize
                        - DriveMap[sSourceDriveLetter].Info.TotalFreeSpace;
                    long lSizeDestFree = -999;
                    long lSizeDestTotal = -999;
                    try
                    { 
                        lSizeDestFree = DriveMap[sDestinationDriveLetter].Info.TotalFreeSpace;
                        lSizeDestTotal = DriveMap[sDestinationDriveLetter].Info.TotalSize;
                    }

                    catch(System.UnauthorizedAccessException oExAuth)
                    {
                        BootmanLog.LogError(txtLogOutput, "Error Accessing Drive Info: " + sDestinationDriveLetter
                            + "\r\n" + oExAuth.Message);
                    }
                    catch(Exception oEx)
                    {
                        // Log error here
                        BootmanLog.LogError(txtLogOutput, "Error Accessing Drive Info: " + sDestinationDriveLetter
                            +"\r\n" + oEx.Message);
                    }

                    // Determine if there is enough free space
                    bool bIsEnoughSpace = false;
                    if (bFormatBootReady)
                    {
                        if (lSizeDestFree > 0)
                        {
                            if (lSizeDestTotal > lSizeSourceUsed)
                            {
                                bIsEnoughSpace = true;
                            }
                        }
                        else
                        {
                            // We don't know the size
                            // Lets try to format and copy
                            bIsEnoughSpace = true;
                        }
                    }
                    else
                    {
                        if (lSizeDestFree > 0)
                        {
                            if (lSizeDestFree > lSizeSourceUsed)
                            {
                                bIsEnoughSpace = true;
                            }
                        }
                    }

                    if (bIsEnoughSpace)
                    {
                        DriveToDriveDataRec oDriveWorkDataRec
                            = new DriveToDriveDataRec();

                        oDriveWorkDataRec.SourceDriveLetter = sSourceDriveLetter;
                        oDriveWorkDataRec.DestinationDriveLetter = sDestinationDriveLetter;
                        oDriveWorkDataRec.IsFormatBootReady = bFormatBootReady;
                        oDriveWorkDataRec.UseXCopyForFileCopy = bUseXCopy;

                        RunBGWorker(oDriveWorkDataRec);

                        BootmanLog.LogInfo(txtLogOutput
                            , "Starting Drive Creation.");
                    }
                    else
                    {
                        if (lSizeDestFree < 0)
                        {
                            CustomMessageBox frmCustomMessageBox = new CustomMessageBox();
                            frmCustomMessageBox.Owner = this;
                            frmCustomMessageBox.lblMessage.Text =  "Destination Drive does not appear to be Formated."
                                + " " + "Please Select \"Format/Make Boot Ready\" Option.";
                            frmCustomMessageBox.Show();
                        }
                        else
                        {
                            CustomMessageBox frmCustomMessageBox = new CustomMessageBox();
                            frmCustomMessageBox.Owner = this;
                            frmCustomMessageBox.lblMessage.Text = ""
                                + "\r\n" + "Source Used Size: " + lSizeSourceUsed
                                + "\r\n" + "Destinatin Free Space: " + lSizeDestFree
                                + "\r\n" + "Destination Drive does not have enough free space.";
                            frmCustomMessageBox.Show();
                        }
                    }
                }
                else
                {
                    CustomMessageBox frmCustomMessageBox = new CustomMessageBox();
                    frmCustomMessageBox.Owner = this;
                    frmCustomMessageBox.lblMessage.Text = "Source and Destination Drive cannot be the same.";
                    frmCustomMessageBox.Show();
                }
            }
            else
            {
                CustomMessageBox frmCustomMessageBox = new CustomMessageBox();
                frmCustomMessageBox.Owner = this;
                frmCustomMessageBox.lblMessage.Text = "Please Select Source and Destination Drive.";
                frmCustomMessageBox.Show();
            }
        }

        private void chkTab2FormatBootReady_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                RefreshAllDriveComboBoxes();
            }
        }

        private void chkTab2FormatBootReady_Unchecked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                RefreshAllDriveComboBoxes();
            }
        }

        private void chkTab2UseXCopy_Loaded(object sender, RoutedEventArgs e)
        {
            CheckBox oCheckBox = sender as CheckBox;
            oCheckBox.IsChecked = true;
        }
        #endregion Tab Control - Tab 2 Methods

        #region Tab Control - Tab 3 Methods
        /**************************************************************************************************************
         * Tab Control - Tab 3 Methods
         *************************************************************************************************************/
        private void btnTab3BrowseSource_Click(object sender, RoutedEventArgs e)
        {
            // Show the FolderBrowserDialog.
            System.Windows.Forms.FolderBrowserDialog oFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult oDialogResult = oFolderBrowserDialog.ShowDialog();

            if (oDialogResult == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    txtTab3SourceDirectory.Text = oFolderBrowserDialog.SelectedPath;
                }
                catch (Exception oEx)
                {
                    MessageBox.Show("Error: Could not read file Directory. Original Error: " + oEx.Message);
                }
            }
        }

        private void btnTab3BrowseDestination_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog oSaveFileDialog = new System.Windows.Forms.SaveFileDialog();
            oSaveFileDialog.Filter = "ISO File (*.iso)|*.iso";
            oSaveFileDialog.FileName = "Untitled";
            oSaveFileDialog.Title = "Save As";
            if (oSaveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtTab3DestinationFile.Text = oSaveFileDialog.FileName;
            }
        }

        private void CreateImageCreate()
        {
            string sSourceDirectory = txtTab3SourceDirectory.Text;
            string sDestinationFile = txtTab3DestinationFile.Text;

            bool bImageBootReady = (chkTab1FormatBootReady.IsChecked == true);

            if (sSourceDirectory.Trim().Length > 0
                && sDestinationFile.Trim().Length > 0)
            {
                CreateImageDataRec oDriveWorkDataRec
                        = new CreateImageDataRec();

                oDriveWorkDataRec.SourceDirectory = sSourceDirectory;
                oDriveWorkDataRec.DestinationFile = sDestinationFile;
                oDriveWorkDataRec.IsImageBootReady = bImageBootReady;

                RunBGWorker(oDriveWorkDataRec);

                BootmanLog.LogInfo(txtLogOutput
                    , "Starting Image Creation.");
            }
            else
            {
                CustomMessageBox frmCustomMessageBox = new CustomMessageBox();
                frmCustomMessageBox.Owner = this;
                frmCustomMessageBox.lblMessage.Text = "Please Select Source and Destination Drive.";
                frmCustomMessageBox.Show();
            }
        }
        #endregion Tab Control - Tab 3 Methods

        #region Tab Control - Tab 4 Methods
        /**************************************************************************************************************
         * Tab Control - Tab 4 Methods
         *************************************************************************************************************/
        private void btnTab4SysInfo_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("msinfo32.exe");
        }
        #endregion Tab Control - Tab 4 Methods

        #region Log Methods
        /**************************************************************************************************************
         * Log Methods
         *************************************************************************************************************/
        private void chkBtnLog_Checked(object sender, RoutedEventArgs e)
        {
            frmMainWindow.Height = MI_DEFAULT_HEIGHT_MAIN_FORM;

            txtLogOutput.Clear();
            txtLogOutput.Visibility = Visibility.Visible;

            if(this.IsLoaded)
            {
                Properties.Settings.Default.ShowLog = true;
                Properties.Settings.Default.Save();
            }
        }

        private void chkBtnLog_Unchecked(object sender, RoutedEventArgs e)
        {
            frmMainWindow.Height = MI_DEFAULT_HEIGHT_MAIN_FORM - MI_DEFAULT_HEIGHT_OUTPUT_LOG;

            txtLogOutput.Clear();
            txtLogOutput.Visibility = Visibility.Collapsed;

            if (this.IsLoaded)
            {
                Properties.Settings.Default.ShowLog = false;
                Properties.Settings.Default.Save();
            }
        }

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            txtLogOutput.Clear();
        }
        #endregion Log Methods

        #region Application Work Methods
        /**************************************************************************************************************
         * Application Work Methods
         *************************************************************************************************************/

        private void RefreshAllDriveComboBoxes()
        {
            SkipRefreshAllDriveComboBoxes = true;

            List<DriveType> oRemovableDriveType =
                new List<DriveType>() { DriveType.Removable };

            List<DriveType> oAllStandardDriveTypes =
                new List<DriveType>() { DriveType.Removable, DriveType.CDRom, DriveType.Fixed, DriveType.Network };

            // Load Drives
            DriveMap = LoadDrives(true);

            if (chkTab1FormatBootReady.IsChecked == true)
            {
                // Show formated and unformated drives
                RefreshComboBoxDriveLetters(cboTab1DestinationDriveLetter, oRemovableDriveType, true);
            }
            else
            {
                // Show only formated drives
                RefreshComboBoxDriveLetters(cboTab1DestinationDriveLetter, oRemovableDriveType, false);
            }

            RefreshComboBoxDriveLetters(cboTab2SourceDriveLetter, oAllStandardDriveTypes, false);
            if (chkTab2FormatBootReady.IsChecked == true)
            {
                RefreshComboBoxDriveLetters(cboTab2DestinationDriveLetter, oRemovableDriveType, true);
            }
            else
            {
                RefreshComboBoxDriveLetters(cboTab2DestinationDriveLetter, oRemovableDriveType, false);
            }

            SkipRefreshAllDriveComboBoxes = false;
        }

        private void RefreshComboBoxDriveLetters(ComboBox in_cboDestinationDriveLetters
            , IEnumerable<DriveType> in_oValidDriveTypes
            , bool in_bAllowNotReadyDrives)
        {
            // Clear all Items from combobox
            in_cboDestinationDriveLetters.Items.Clear();

            // Populate drive combobox
            foreach (string sDriveLetter in DriveMap.Keys)
            {
                DriveType eDriveType = DriveMap[sDriveLetter].Info.DriveType;
                if (in_oValidDriveTypes.Contains(eDriveType))
                {
                    if (DriveMap[sDriveLetter].Info.IsReady
                        || in_bAllowNotReadyDrives)
                    {
                        in_cboDestinationDriveLetters.Items.Add(sDriveLetter);
                    }
                }
            }

            // Set default drive
            if (in_cboDestinationDriveLetters.Items.Count > 0)
            {
                in_cboDestinationDriveLetters.SelectedIndex = 0;
            }
        }

        private static void CleanupTempWorkArea(String sDiskPartWorkPath)
        {
            System.IO.DirectoryInfo downloadedMessageInfo = new DirectoryInfo(sDiskPartWorkPath);

            foreach (FileInfo file in downloadedMessageInfo.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in downloadedMessageInfo.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        private bool CopyFilesToDestinationDrive(String in_sRootFileSourceDirectory
            , string in_sFileSourceSubDirectory
            , string in_sTargetPath)
        {
            try
            {
                string sSourcePath = "";
                string sFileTargetPath = "";
                if (in_sFileSourceSubDirectory.Trim().Length > 0)
                {
                    sSourcePath = System.IO.Path.Combine(in_sRootFileSourceDirectory
                        , in_sFileSourceSubDirectory);

                    sFileTargetPath = System.IO.Path.Combine(in_sTargetPath
                        , in_sFileSourceSubDirectory);
                }
                else
                {
                    sSourcePath = in_sRootFileSourceDirectory;
                    sFileTargetPath = in_sTargetPath;
                }

                if (System.IO.Directory.Exists(sSourcePath))
                {
                    System.IO.DirectoryInfo oDirInfo = new DirectoryInfo(sSourcePath);

                    // Create directory if it does not exist
                    if (!System.IO.Directory.Exists(sFileTargetPath))
                    {
                        Directory.CreateDirectory(sFileTargetPath);
                    }

                    // Copy the files and overwrite destination files if they already exist. 
                    foreach (FileInfo oFileInfo in oDirInfo.GetFiles())
                    {
                        // Use static Path methods to extract only the file name from the path.
                        string sFileName = System.IO.Path.GetFileName(oFileInfo.Name);
                        string sDestFile = System.IO.Path.Combine(sFileTargetPath
                        , sFileName);
                        oFileInfo.CopyTo(sDestFile
                            , true);
                    }

                    foreach (DirectoryInfo oSubDirInfo in oDirInfo.GetDirectories())
                    {
                        if (oSubDirInfo.Name != MS_SYSTEM_VOLUME_INFORMATION)
                        {
                            string sNewSubDir = "";
                            if (in_sFileSourceSubDirectory.Trim().Length > 0)
                            {
                                sNewSubDir = System.IO.Path.Combine(in_sFileSourceSubDirectory, oSubDirInfo.Name);
                            }
                            else
                            {
                                sNewSubDir = oSubDirInfo.Name;
                            }

                            // Recursive call
                            bool bRecursiveCallResult = CopyFilesToDestinationDrive(in_sRootFileSourceDirectory
                                , sNewSubDir
                                , in_sTargetPath);

                            if (!bRecursiveCallResult)
                            {
                                // return false if a recursive call fails
                                return false;
                            }
                        }
                    }
                    return true;
                }
                else
                {
                    Console.WriteLine("Source path does not exist!");
                    return false;
                }
            }
            catch (Exception oEx)
            {
                BootmanLog.LogError(txtLogOutput
                    , "Error: " + oEx.Message);
                return false;
            }
        }

        private Dictionary<string, DiskPartDriveMap> LoadDrives(bool bAllowNotReadyDrives)
        {
            Dictionary<string, DiskPartDriveMap> oDriveDict = new Dictionary<string, DiskPartDriveMap>();

            //int iDriveNumber = -1;

            List<DriveType> oValidDiskPartDriveTypes =
                new List<DriveType>() { DriveType.Removable, DriveType.Fixed};

            DiskInfoEx oDiskInfoEx  = new DiskInfoEx();
            foreach (DriveInfo oDriveInfo in DriveInfo.GetDrives())
            {
                string sParentDrives = oDiskInfoEx.GetPhysicalDiskParentFor(oDriveInfo.RootDirectory.ToString());
                string[] oParentDriveList = sParentDrives.Split(',');

                foreach (string sParentDrive in oParentDriveList)
                {
                    if (
                        (oDriveInfo.IsReady)
                        || (!oDriveInfo.IsReady
                            && (oDriveInfo.DriveType == DriveType.Removable
                                || oDriveInfo.DriveType == DriveType.Fixed)
                            && bAllowNotReadyDrives)
                        )
                    {
                        string sDrivePath = oDriveInfo.Name;
                        DiskPartDriveMap oDriveMap = new DiskPartDriveMap();
                        string sDriveLetter = sDrivePath.Replace(@":\", "");
                        oDriveMap.Letter = sDriveLetter;
                        oDriveMap.Path = sDrivePath;

                        if (sParentDrive.Contains("Physical Drive"))
                        {
                            // Physical Drive
                            int iStart = sParentDrive.IndexOf("Physical Drive");
                            try
                            {
                                int iDriveNumber = Convert.ToInt32(sParentDrive.Substring(iStart + 14));
                                oDriveMap.ParentDriveNumber = iDriveNumber;
                            }
                            catch
                            {
                                oDriveMap.ParentDriveNumber = -999;
                            }
                            oDriveMap.ParentDriveType = "Physical Drive";
                        }
                        else
                        {
                            // CDRom
                            if (sParentDrive.Contains("CD/DVD Rom"))
                            {
                                // Physical Drive
                                int iStart = sParentDrive.IndexOf("CD/DVD Rom");
                                try
                                {

                                    int iDriveNumber = Convert.ToInt32(sParentDrive.Substring(iStart + 10));
                                    oDriveMap.ParentDriveNumber = iDriveNumber;
                                }
                                catch
                                {
                                    oDriveMap.ParentDriveNumber = -999;
                                }
                                oDriveMap.ParentDriveType = "CD/DVD Rom";
                            }
                            else
                            {
                                oDriveMap.ParentDriveType = sParentDrive;
                                oDriveMap.ParentDriveNumber = -999;
                            }
                        }
                        oDriveMap.Info = oDriveInfo;
                        oDriveDict.Add(sDriveLetter, oDriveMap);
                    }

                } //foreach
                
            } // foreach

            // Spit out Drive table info
            BootmanLog.LogInfo(txtLogOutput, "**** Drive Map ****");
            foreach (DiskPartDriveMap item in oDriveDict.Values)
            {
                BootmanLog.LogInfo(txtLogOutput
                    , item.ParentDriveNumber
                    + "  " + item.Info 
                    + "  " + item.Letter
                    + "  " + item.ParentDriveType);
            }

            return oDriveDict;
        }
        #endregion Application Work Methods

        #region USB Drive Notification
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Adds the windows message processing hook and registers USB device add/removal notification.
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            if (source != null)
            {
                var windowHandle = source.Handle;
                source.AddHook(HwndHandler);
                UsbNotification.RegisterUsbDeviceNotification(windowHandle);
            }
        }

         //<summary>
         //Method that receives window messages.
         //</summary>
        private IntPtr HwndHandler(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            if (msg == UsbNotification.WmDevicechange)
            {
                switch ((int)wparam)
                {
                    case UsbNotification.DbtDeviceremovecomplete:
                        
                        if (dtRemoveUsbDriveTStamp < DateTime.Now.AddMilliseconds(-500))
                        {
                            dtRemoveUsbDriveTStamp = DateTime.Now;
                             
                        }
                        else
                        {
                            Usb_DeviceRemoved();
                            // Reset time
                            dtAddUsbDriveTStamp = new DateTime(1900, 1, 1, 0, 0, 0, 0);
                        }
                        
                        break;
                    case UsbNotification.DbtDevicearrival:
                        if (dtAddUsbDriveTStamp < DateTime.Now.AddMilliseconds(-500))
                        {
                            dtAddUsbDriveTStamp = DateTime.Now;
                            
                        }
                        else
                        {
                            Usb_DeviceAdded(); 
                            // Reset time
                            dtAddUsbDriveTStamp = new DateTime(1900, 1, 1, 0, 0, 0, 0);
                        }
                        break;
                }
            }

            handled = false;
            return IntPtr.Zero;
        }

        public void Usb_DeviceRemoved()
        { 
            RefreshAllDriveComboBoxes();
        }

        public void Usb_DeviceAdded()
        {
            RefreshAllDriveComboBoxes();
        }
        #endregion USB Drive Notification

        #region App Action buttons

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {

            if (WorkerThread != null)
            {
                // Cancel Actions

                WorkerThread.Abort();
                WorkerThread = null;

                btnCreate.Content = "CANCELING";
                btnCreate.IsEnabled = false;
            }
            else
            {
                // Create Actions
                TabItem oTabItem = tabctrlBootman.SelectedItem as TabItem;

                switch (oTabItem.Name)
                {
                    case "tabitemImageToDrive":
                        ImageToDriveCreate();
                        break;

                    case "tabitemDriveToDrive":
                        DriveToDriveCreate();
                        break;

                    case "tabitemCreateImage":
                        CreateImageCreate();
                        break;

                    default:
                        break;
                }
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtTab1ImageFile.Clear();
            chkTab1ExtractImageDirectToDrive.IsChecked = true;
            chkTab1FormatBootReady.IsChecked = true;

            chkTab2FormatBootReady.IsChecked = true;
            chkTab2UseXCopy.IsChecked = true;

            txtTab3DestinationFile.Clear();
            txtTab3SourceDirectory.Clear();

            RefreshAllDriveComboBoxes();
        }
        #endregion App Action buttons
        //hide buttons that are not needed on info screen
        private void TabItemAbout_GotFocus(object sender, RoutedEventArgs e)
        {
            btnRefresh.Visibility = System.Windows.Visibility.Collapsed;
            btnCreate.Visibility = System.Windows.Visibility.Collapsed;

            lblStatusLabel.Visibility = System.Windows.Visibility.Collapsed;
            lblStatusDisplay.Visibility = System.Windows.Visibility.Collapsed;
            progressBarBootman.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void tabitemCreateImage_GotFocus(object sender, RoutedEventArgs e)
        {
            btnRefresh.Visibility = System.Windows.Visibility.Visible;
            btnCreate.Visibility = System.Windows.Visibility.Visible;

            lblStatusLabel.Visibility = System.Windows.Visibility.Visible;
            lblStatusDisplay.Visibility = System.Windows.Visibility.Visible;
            progressBarBootman.Visibility = System.Windows.Visibility.Visible;
        }

        private void tabitemDriveToDrive_GotFocus(object sender, RoutedEventArgs e)
        {
            btnRefresh.Visibility = System.Windows.Visibility.Visible;
            btnCreate.Visibility = System.Windows.Visibility.Visible;

            lblStatusLabel.Visibility = System.Windows.Visibility.Visible;
            lblStatusDisplay.Visibility = System.Windows.Visibility.Visible;
            progressBarBootman.Visibility = System.Windows.Visibility.Visible;
        }

        private void tabitemImageToDrive_GotFocus(object sender, RoutedEventArgs e)
        {
            btnRefresh.Visibility = System.Windows.Visibility.Visible;
            btnCreate.Visibility = System.Windows.Visibility.Visible;

            lblStatusLabel.Visibility = System.Windows.Visibility.Visible;
            lblStatusDisplay.Visibility = System.Windows.Visibility.Visible;
            progressBarBootman.Visibility = System.Windows.Visibility.Visible;
        }

        private void frmMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Properties.Settings.Default.UACOverrideAccepted)
            {
                // Show Accept UAC override
                AcceptUACOverride frmAcceptUACOverride = new AcceptUACOverride();
                frmAcceptUACOverride.Owner = this;
                frmAcceptUACOverride.Show();
            }
        }
    }
    
    #region Application Data Classes

    /**************************************************************************************************************
     * Application Data Classes
     *************************************************************************************************************/
    public class DiskPartDriveMap
    {
        public string Letter { get; set; }
        public string Path { get; set; }
        public string ParentDriveType { get; set; }
        public int ParentDriveNumber { get; set; }

        public DriveInfo Info { get; set; }
    }

    public class ImageToDriveDataRec
    {
        public string ImagePathAndName { get; set; }
        public string DriveLetterDestination { get; set; }
        public bool IsbExtractImageDirectToDrive { get; set; }
        public bool IsFormatBootReady { get; set; }
    }

    public class DriveToDriveDataRec
    {
        public string SourceDriveLetter { get; set; }
        public string DestinationDriveLetter { get; set; }
        public bool IsFormatBootReady { get; set; }
        public bool UseXCopyForFileCopy { get; set; }
    }

    public class CreateImageDataRec
    {
        public string SourceDirectory { get; set; }
        public string DestinationFile { get; set; }
        public bool IsImageBootReady { get; set; }
    }
    #endregion Application Data Classes
}
