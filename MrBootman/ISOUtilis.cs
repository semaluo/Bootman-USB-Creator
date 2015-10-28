using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using DiscUtils;
using DiscUtils.Iso9660;

using DiscUtils.Udf;

namespace MrBootman
{
    class ISOUtils
    {
        public System.Windows.Controls.TextBox LogTextBox { get; set; }

        // Constructor
        public ISOUtils(System.Windows.Controls.TextBox inout_oLogTextBox)
        {
            LogTextBox = inout_oLogTextBox;
        }

        /**************************************************************************************************************
         * ISO Extract
         *************************************************************************************************************/
        public bool ExtractISO(string in_sIsoPathAndFileName
            , string in_sIsoExtractionRootDir
            , bool in_bCreateSubdirectory)
        {
            try
            {
                using (FileStream ISOStream = File.Open(in_sIsoPathAndFileName, FileMode.Open))
                {
                    DiscUtils.FileSystemManager oMgr = new FileSystemManager();
                    DiscUtils.FileSystemInfo[] oFileInfoItems = oMgr.DetectFileSystems(ISOStream);

                    bool bIsUDF = false;
                    foreach (DiscUtils.FileSystemInfo oInfo in oFileInfoItems)
                    {
                        string sDesc = oInfo.Description;

                        if (sDesc == "OSTA Universal Disk Format (UDF)")
                        {
                            bIsUDF = true;
                        }
                    }

                    string sIsoExtractionPath = "";

                    if (in_bCreateSubdirectory)
                    {
                        // Extract to subdirectory on drive
                        sIsoExtractionPath
                            = Path.Combine(in_sIsoExtractionRootDir, Path.GetFileNameWithoutExtension(in_sIsoPathAndFileName));
                    }
                    else
                    {
                        // Extract to ISO Extraction Root directory
                        sIsoExtractionPath = in_sIsoExtractionRootDir;
                    }

                    if (bIsUDF)
                    {
                        BootmanLog.LogError(LogTextBox, "Processing ISO Extract as ISO-UDF");

                        UdfReader oUDFReader = new UdfReader(ISOStream);
                        ExtractDirectoryISOUDF(oUDFReader.Root
                            , sIsoExtractionPath
                            , "");
                        oUDFReader.Dispose();
                        return true;
                    }
                    else
                    {
                        BootmanLog.LogError(LogTextBox, "Processing ISO Extract as ISO-9660");

                        CDReader oISO9660Reader = new CDReader(ISOStream, true, true);
                        ExtractDirectoryISO9660(oISO9660Reader.Root
                            , sIsoExtractionPath
                            , "");
                        oISO9660Reader.Dispose();
                        return true;
                    }
                }
            }
            catch(Exception oEx)
            {
                BootmanLog.LogError(LogTextBox, "Error Extracting ISO: " + oEx.Message);
                return false;
            }
        }

        /**************************************************************************************************************
          * Extract ISO 9660
          *************************************************************************************************************/
        private void ExtractDirectoryISO9660(DiscDirectoryInfo Dinfo, string RootPath, string PathinISO)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(PathinISO))
                {
                    PathinISO += "\\" + Dinfo.Name;
                }
                RootPath += "\\" + Dinfo.Name;
                AppendDirectoryISO9660(RootPath);
                foreach (DiscDirectoryInfo dinfo in Dinfo.GetDirectories())
                {
                    ExtractDirectoryISO9660(dinfo, RootPath, PathinISO);
                }
                foreach (DiscFileInfo finfo in Dinfo.GetFiles())
                {
                    using (Stream FileStr = finfo.OpenRead())
                    {
                        using (FileStream Fs = File.Create(RootPath + "\\" + finfo.Name)) // Here you can Set the BufferSize Also e.g. File.Create(RootPath + "\\" + finfo.Name, 4 * 1024)
                        {
                            FileStr.CopyTo(Fs, 4 * 1024); // Buffer Size is 4 * 1024 but you can modify it in your code as per your need
                        }
                    }
                }
            }
            catch(Exception oEx)
            {
                BootmanLog.LogError(LogTextBox, "Error Extracting ISO: " + oEx.Message);
            }
        }

        private void AppendDirectoryISO9660(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (DirectoryNotFoundException Ex)
            {
                AppendDirectoryISO9660(Path.GetDirectoryName(path));
            }
            catch (PathTooLongException Exx)
            {
                AppendDirectoryISO9660(Path.GetDirectoryName(path));
            }
        }

        /**************************************************************************************************************
         * Extract ISO 9660
         *************************************************************************************************************/
        public bool CreateISO9660(string in_sISOPathAndFilename
            , string in_sISORootSourceRootDir)
        {
            try
            {
                CDBuilder oISOBuilder = new CDBuilder();
                oISOBuilder.UseJoliet = true;
                oISOBuilder.VolumeIdentifier = "A_SAMPLE_DISK";
                string sISOPathAndFilename = in_sISOPathAndFilename;

                // Add Files to ISO
                bool bResultAddFiles = AddFilesToISO9660(in_sISORootSourceRootDir
                , ""
                , oISOBuilder
                , false);

                if (bResultAddFiles)
                {
                    // Build the ISO
                    oISOBuilder.Build(sISOPathAndFilename);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception oEx)
            {
                BootmanLog.LogError(LogTextBox, "Error Extracting ISO: " + oEx.Message);
                return false;
            }
        }

        private bool AddFilesToISO9660(string in_sDirectoryRead
            , string in_sDirectoryISO
            , CDBuilder in_oISOBuilder
            , bool in_bCreateISOSubDirectory)
        {
            try
            {
                string sDirectoryISO = in_sDirectoryISO.Trim();

                if (System.IO.Directory.Exists(in_sDirectoryRead))
                {
                    // Create new ISO Subdirectory
                    if (in_bCreateISOSubDirectory)
                    {
                        in_oISOBuilder.AddDirectory(sDirectoryISO);
                    }

                    System.IO.DirectoryInfo oDirInfo = new DirectoryInfo(in_sDirectoryRead);

                    // Copy the files and overwrite destination files if they already exist. 
                    foreach (FileInfo oFileInfo in oDirInfo.GetFiles())
                    {
                        // Add File to ISO
                        try
                        {
                            if (Properties.Settings.Default.DebugOn)
                            {
                                BootmanLog.LogError(LogTextBox
                                    , "ISO Create Add: >> " + sDirectoryISO + "\\" + oFileInfo.Name);
                            }

                            in_oISOBuilder.AddFile(sDirectoryISO + "\\" + oFileInfo.Name
                                , oFileInfo.FullName);
                        }
                        catch(Exception oEx)
                        {
                            BootmanLog.LogError(LogTextBox
                                , "Error: " + oEx.Message);

                            throw new Exception("Rethrow - " + oEx.Message);
                        }
                    }

                    foreach (DirectoryInfo oSubDirInfo in oDirInfo.GetDirectories())
                    {
                       string sNewDirRead = oSubDirInfo.FullName;

                       string sNewDirISO = "";
                       if (sDirectoryISO.Length > 0)
                       {
                           sNewDirISO = sDirectoryISO + "\\" + oSubDirInfo.Name;
                       }
                       else
                       {
                           sNewDirISO = oSubDirInfo.Name;
                       }
                      
                        // Recursive call
                       bool bRecusiveCallResult = AddFilesToISO9660(sNewDirRead
                            , sNewDirISO
                            , in_oISOBuilder
                            , true);

                        if (!bRecusiveCallResult)
                        {
                            return false;
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
                BootmanLog.LogError(LogTextBox
                    , "Error: " + oEx.Message);
                return false;
            }
        }

        /**************************************************************************************************************
         * Extract ISO 13346 - UDF
         *************************************************************************************************************/
        private void ExtractDirectoryISOUDF(DiscDirectoryInfo Dinfo, string RootPath, string PathinISO)
        {
            try
            {

                if (!string.IsNullOrWhiteSpace(PathinISO))
                {
                    PathinISO += "\\" + Dinfo.Name;
                }
                RootPath += "\\" + Dinfo.Name;
                AppendDirectoryISOUDF(RootPath);
                foreach (DiscDirectoryInfo dinfo in Dinfo.GetDirectories())
                {
                    ExtractDirectoryISOUDF(dinfo, RootPath, PathinISO);
                }
                foreach (DiscFileInfo finfo in Dinfo.GetFiles())
                {
                    using (Stream FileStr = finfo.OpenRead())
                    {
                        using (FileStream Fs = File.Create(RootPath + "\\" + finfo.Name)) // Here you can Set the BufferSize Also e.g. File.Create(RootPath + "\\" + finfo.Name, 4 * 1024)
                        {
                            FileStr.CopyTo(Fs, 4 * 1024); // Buffer Size is 4 * 1024 but you can modify it in your code as per your need
                        }
                    }
                }
            }
            catch (Exception oEx)
            {
                BootmanLog.LogError(LogTextBox, "Error Extracting ISO: " + oEx.Message);
            }
        }
        private void AppendDirectoryISOUDF(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (DirectoryNotFoundException Ex)
            {
                AppendDirectoryISOUDF(Path.GetDirectoryName(path));
            }
            catch (PathTooLongException Exx)
            {
                AppendDirectoryISOUDF(Path.GetDirectoryName(path));
            }
        }
    }
}
