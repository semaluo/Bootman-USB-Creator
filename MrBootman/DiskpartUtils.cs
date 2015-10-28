using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MrBootman
{
    public class DiskpartUtils
    {
        public System.Windows.Controls.TextBox LogTextBox { get; set; }

        // Constructor
        public DiskpartUtils(System.Windows.Controls.TextBox inout_oLogTextBox)
        {
            LogTextBox = inout_oLogTextBox;
        }

        public bool CreateDiskPartWorkFile(string in_sUsbDriveLetter
            , int in_iDriveNumber
            , string in_sWorkAreaPath
       )
        {
            try
            {
                // Create directory
                string sDiskPartWorkFile = Path.Combine(in_sWorkAreaPath, "DiskPartWorkFile.txt");

                using (System.IO.FileStream oFileStream = System.IO.File.Create(sDiskPartWorkFile))
                { 
                    StreamWriter oStreamWriter = new StreamWriter(oFileStream);

                    //select disk
                    oStreamWriter.WriteLine("select disk " + in_iDriveNumber);
                    oStreamWriter.WriteLine("");

                    //clean
                    oStreamWriter.WriteLine("clean");
                    oStreamWriter.WriteLine("");

                    //create partition primary
                    oStreamWriter.WriteLine("create part pri");
                    oStreamWriter.WriteLine("");

                    //select partition
                    oStreamWriter.WriteLine("select part 1");
                    oStreamWriter.WriteLine("");

                    //format
                    oStreamWriter.WriteLine("format fs=ntfs quick");
                    oStreamWriter.WriteLine("");

                    //make active
                    oStreamWriter.WriteLine("active");
                    oStreamWriter.WriteLine("");

                    //assign
                    oStreamWriter.WriteLine("assign letter=" + in_sUsbDriveLetter);
                    oStreamWriter.WriteLine("");

                    oStreamWriter.Dispose();
                }
                return true;
            }
            catch (Exception oEx)
            {
                BootmanLog.LogError(LogTextBox,"Error: " + oEx.Message);

                return false;
            }
        }
    }
}
