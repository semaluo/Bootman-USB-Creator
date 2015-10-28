using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace MrBootman
{
    public class DiskInfoEx
    {
        //private const uint GenericRead = 0x80000000;
        private const int FileShareRead = 1;
        private const int Filesharewrite = 2;
        private const int OpenExisting = 3;
        private const int IoctlVolumeGetVolumeDiskExtents = 0x560000;
        private const int IncorrectFunction = 1;
        private const int ErrorInsufficientBuffer = 122;

        private const int MoreDataIsAvailable = 234;
        private List<string> currentDriveMappings;

        private string errorMessage;
        public enum RESOURCE_SCOPE
        {
            RESOURCE_CONNECTED = 0x1,
            RESOURCE_GLOBALNET = 0x2,
            RESOURCE_REMEMBERED = 0x3,
            RESOURCE_RECENT = 0x4,
            RESOURCE_CONTEXT = 0x5
        }

        public enum RESOURCE_TYPE
        {
            RESOURCETYPE_ANY = 0x0,
            RESOURCETYPE_DISK = 0x1,
            RESOURCETYPE_PRINT = 0x2,
            RESOURCETYPE_RESERVED = 0x8
        }

        public enum RESOURCE_USAGE
        {
            RESOURCEUSAGE_CONNECTABLE = 0x1,
            RESOURCEUSAGE_CONTAINER = 0x2,
            RESOURCEUSAGE_NOLOCALDEVICE = 0x4,
            RESOURCEUSAGE_SIBLING = 0x8,
            RESOURCEUSAGE_ATTACHED = 0x10,
            RESOURCEUSAGE_ALL = (RESOURCEUSAGE_CONNECTABLE | RESOURCEUSAGE_CONTAINER | RESOURCEUSAGE_ATTACHED)
        }

        public enum RESOURCE_DISPLAYTYPE
        {
            RESOURCEDISPLAYTYPE_GENERIC = 0x0,
            RESOURCEDISPLAYTYPE_DOMAIN = 0x1,
            RESOURCEDISPLAYTYPE_SERVER = 0x2,
            RESOURCEDISPLAYTYPE_SHARE = 0x3,
            RESOURCEDISPLAYTYPE_FILE = 0x4,
            RESOURCEDISPLAYTYPE_GROUP = 0x5,
            RESOURCEDISPLAYTYPE_NETWORK = 0x6,
            RESOURCEDISPLAYTYPE_ROOT = 0x7,
            RESOURCEDISPLAYTYPE_SHAREADMIN = 0x8,
            RESOURCEDISPLAYTYPE_DIRECTORY = 0x9,
            RESOURCEDISPLAYTYPE_TREE = 0xa,
            RESOURCEDISPLAYTYPE_NDSCONTAINER = 0xb
        }

        public enum NERR
        {
            NERR_Success = 0,
            ERROR_MORE_DATA = 234,
            ERROR_NO_BROWSER_SERVERS_FOUND = 6118,
            ERROR_INVALID_LEVEL = 124,
            ERROR_ACCESS_DENIED = 5,
            ERROR_INVALID_PARAMETER = 87,
            ERROR_NOT_ENOUGH_MEMORY = 8,
            ERROR_NETWORK_BUSY = 54,
            ERROR_BAD_NETPATH = 53,
            ERROR_NO_NETWORK = 1222,
            ERROR_INVALID_HANDLE_STATE = 1609,
            ERROR_EXTENDED_ERROR = 1208
        }

        public struct NETRESOURCE
        {
            public RESOURCE_SCOPE dwScope;
            public RESOURCE_TYPE dwType;
            public RESOURCE_DISPLAYTYPE dwDisplayType;
            public RESOURCE_USAGE dwUsage;
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
            public string lpLocalName;
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
            public string lpRemoteName;
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
            public string lpComment;
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
            public string lpProvider;
        }

        // Native WINAPI functions to retrieve list of devices
        private const int ERROR_INSUFFICIENT_BUFFER = 0x7A;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint QueryDosDevice(string lpDeviceName, IntPtr lpTargetPath, int ucchMax);

        private class NativeMethods
        {
            [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern SafeFileHandle CreateFile(string fileName
                , int desiredAccess
                , int shareMode
                , IntPtr securityAttributes
                , int creationDisposition
                , int flagsAndAttributes
                , IntPtr hTemplateFile);


            [DllImport("kernel32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DeviceIoControl(SafeFileHandle hVol
                , int controlCode
                , IntPtr inBuffer
                , int inBufferSize
                , ref DiskExtents outBuffer
                , int outBufferSize
                , ref int bytesReturned
                , IntPtr overlapped);

            [DllImport("kernel32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DeviceIoControl(SafeFileHandle hVol
                , int controlCode
                , IntPtr inBuffer
                , int inBufferSize
                , IntPtr outBuffer
                , int outBufferSize
                , ref int bytesReturned
                , IntPtr overlapped);

            [DllImport("mpr.dll", CharSet = CharSet.Auto)]
            public static extern int WNetEnumResource(IntPtr hEnum
                , ref int lpcCount
                , IntPtr lpBuffer
                , ref int lpBufferSize);

            [DllImport("mpr.dll", CharSet = CharSet.Auto)]
            public static extern int WNetOpenEnum(RESOURCE_SCOPE dwScope
                , RESOURCE_TYPE dwType
                , RESOURCE_USAGE dwUsage
                , ref NETRESOURCE lpNetResource
                , ref IntPtr lphEnum);

            [DllImport("mpr.dll", CharSet = CharSet.Auto)]
            public static extern int WNetCloseEnum(IntPtr hEnum);
        }

        // DISK_EXTENT in the msdn.
        [StructLayout(LayoutKind.Sequential)]
        private struct DiskExtent
        {
            public int DiskNumber;
            public long StartingOffset;
            public long ExtentLength;
        }

        // DISK_EXTENTS
        [StructLayout(LayoutKind.Sequential)]
        private struct DiskExtents
        {
            public int numberOfExtents;
            // We can't marhsal an array if we don't know its size.
            public DiskExtent first;
        }

        public  DiskInfoEx()
        {
            Refresh();
        }

        public void Refresh()
        {
            errorMessage = "";
            currentDriveMappings = null;
            currentDriveMappings = new List<string>();
            GetPhysicalDisks(ref currentDriveMappings);
        }

        // A Volume could be on many physical drives.
        // Returns a list of string containing each physical drive the volume uses.
        // For CD Drives with no disc in it will return an empty list.
        private List<string> GetPhysicalDriveStrings(DriveInfo driveInfo)
        {
            SafeFileHandle sfh = null;
            List<string> physicalDrives = new List<string>(1);
            string path = "\\\\.\\" + driveInfo.RootDirectory.ToString().TrimEnd('\\');
            try
            {
                sfh = NativeMethods.CreateFile(path, 0, FileShareRead | Filesharewrite, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
                int bytesReturned = 0;
                DiskExtents de1 = new DiskExtents();
                int numDiskExtents = 0;
                bool result = NativeMethods.DeviceIoControl(sfh
                    , IoctlVolumeGetVolumeDiskExtents
                    , IntPtr.Zero
                    , 0
                    , ref de1
                    , Marshal.SizeOf(de1)
                    , ref bytesReturned, IntPtr.Zero);
                DiskExtents de1Cast = (DiskExtents)de1;
                if (result == true)
                {
                    // there was only one disk extent. So the volume lies on 1 physical drive.
                    physicalDrives.Add("\\\\.\\PhysicalDrive" + de1Cast.first.DiskNumber.ToString());
                    return physicalDrives;
                }
                if (Marshal.GetLastWin32Error() == IncorrectFunction)
                {
                    // The drive is removable and removed, like a CDRom with nothing in it.
                    return physicalDrives;
                }
                if (Marshal.GetLastWin32Error() == MoreDataIsAvailable)
                {
                    // This drive is part of a mirror or volume - handle it below. 
                }
                else if (Marshal.GetLastWin32Error() != ErrorInsufficientBuffer)
                {
                    throw new Win32Exception();
                }
                // Houston, we have a spanner. The volume is on multiple disks.
                // Untested...
                // We need a blob of memory for the DISK_EXTENTS structure, and all the DISK_EXTENTS
                
                int blobSize = Marshal.SizeOf(typeof(DiskExtents)) + (de1Cast.numberOfExtents - 1) * Marshal.SizeOf(typeof(DiskExtent));
                IntPtr pBlob = Marshal.AllocHGlobal(blobSize);
                result = NativeMethods.DeviceIoControl(sfh, IoctlVolumeGetVolumeDiskExtents, IntPtr.Zero, 0, pBlob, blobSize, ref bytesReturned, IntPtr.Zero);
                if (result == false)
                    throw new Win32Exception();
                // Read them out one at a time.
                IntPtr pNext = new IntPtr(pBlob.ToInt64() + 8);
                // is this always ok on 64 bit OSes? ToInt64?
                for (int i = 0; i <= de1Cast.numberOfExtents - 1; i++)
                {
                    DiskExtent diskExtentN = (DiskExtent)Marshal.PtrToStructure(pNext, typeof(DiskExtent));
                    physicalDrives.Add("\\\\.\\PhysicalDrive" + diskExtentN.DiskNumber.ToString());
                    pNext = new IntPtr(pNext.ToInt32() + Marshal.SizeOf(typeof(DiskExtent)));
                }
                return physicalDrives;
            }
            finally
            {
                if (sfh != null)
                {
                    if (sfh.IsInvalid == false)
                    {
                        sfh.Close();
                    }
                    sfh.Dispose();
                }
            }
        }

        public static List<string> QueryDosDevice(string in_sDevice)
        {
          uint returnSize = 0;
          // Arbitrary initial buffer size
          int maxResponseSize = 100;
  
          IntPtr response = IntPtr.Zero;
  
          string allDevices = null;
          string[] devices = null;

          while (returnSize == 0)
          {
              // Allocate response buffer for native call
              response = Marshal.AllocHGlobal(maxResponseSize);
      
              // Check out of memory condition
              if (response != IntPtr.Zero)
              {
                  try
                  {
                      // List DOS devices
                      returnSize = QueryDosDevice(in_sDevice, response, maxResponseSize);
      
                      // List success
                      if (returnSize != 0)
                      {
                          // Result is returned as null-char delimited multistring
                          // Dereference it from ANSI charset
                          allDevices = Marshal.PtrToStringAnsi(response, maxResponseSize);
                      }
                      // The response buffer is too small, reallocate it exponentially and retry
                      else if (Marshal.GetLastWin32Error() == ERROR_INSUFFICIENT_BUFFER)
                      {
                          maxResponseSize = (int)(maxResponseSize * 5);
                      }
                      // Fatal error has occured, throw exception
                      else
                      {
                          Marshal.ThrowExceptionForHR(Marshal.GetLastWin32Error());
                      }
                  }
                  finally
                  {
                      // Always free the allocated response buffer
                      Marshal.FreeHGlobal(response);
                  }
              }
              else
              {
                  throw new OutOfMemoryException("Out of memory when allocating space for QueryDosDevice command!");
              }
          }
  
          // Split zero-character delimited multi-string
          devices = allDevices.Split('\0');

          //var oReturn = devices.Where(device => device.StartsWith("PhysicalDrive")).ToList<string>();

          List<string> results = new List<string>();
          if (devices != null)
          {
              foreach (string result in devices)
              {
                  if (!string.IsNullOrEmpty(result.Trim()))
                      results.Add(result);
              }
          }

          return results;
        }

        public string GetPhysicalDiskParentFor(string logicalDisk)
        {
            string[] parts = null;

            if (logicalDisk.Length > 0)
            {
                foreach (string driveMapping in currentDriveMappings)
                {
                    if (logicalDisk.Substring(0, 2).ToUpper() == driveMapping.Substring(0, 2).ToUpper())
                    {
                        parts = driveMapping.Split('=');
                        string sReturn = parts[parts.Length - 1];
                        return sReturn;
                    }
                }
            }

            return "";
        }

        public bool GetPhysicalDisks(ref List<string> theList)
        {
            List<string> drivesList = null;
            List<string> tmpList = null;
            string[] parts = null;
            StringBuilder drives = new StringBuilder();

            foreach (DriveInfo logicalDrive in DriveInfo.GetDrives())
            {
                try
                {
                    drives.Remove(0, drives.Length);
                    drives.Append(logicalDrive.RootDirectory.ToString());
                    drives.Append("=");

                    if (logicalDrive.DriveType == DriveType.Network)
                    {
                        // Handle network drives here.
                        drives.Append(GetUncPathOfMappedDrive(logicalDrive.RootDirectory.ToString()));
                    }
                    else if (logicalDrive.DriveType == DriveType.CDRom)
                    {
                        // Attempt to get the CDRom's dos name from QueryDosDevice
                        tmpList = QueryDosDevice(logicalDrive.RootDirectory.ToString().Replace("\\", ""));
                        if (tmpList.Count > 0)
                        {
                            parts = tmpList[0].Trim().Split('\\');
                            if (parts[parts.Length - 1].Length > 5)
                            {
                                if (parts[parts.Length - 1].Substring(0, 5) == "CdRom")
                                    parts[parts.Length - 1] = parts[parts.Length - 1].Replace("CdRom", "CD/DVD Rom ");
                            }
                            drives.Append(parts[parts.Length - 1]);
                        }
                        else
                        {
                            drives.Append("n/a");
                        }
                    }
                    else
                    {
                        drivesList = GetPhysicalDriveStrings(logicalDrive);

                        if (drivesList.Count > 0)
                        {
                            foreach (string drive in drivesList)
                            {
                                // get a temp copy of drive for manipulation
                                string driveTemp = drive;

                                // Handle the spanners
                                driveTemp = driveTemp.Replace("\\\\.\\", "");
                                driveTemp = driveTemp.Replace("PhysicalDrive", "Physical Drive ");

                                drives.Append(driveTemp);
                                drives.Append(", ");
                            }
                            drives.Remove(drives.Length - 2, 2);
                        }
                        else
                        {
                            drives.Append("n/a");
                        }
                    }
                    theList.Add(drives.ToString());
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                    //Interaction.MsgBox("" + ex.Message + "\r\n" + "\r\n" + drives.ToString());
                }
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public string GetUncPathOfMappedDrive(string driveLetter)
        {
            string functionReturnValue = null;

            if (driveLetter.Substring(driveLetter.Length - 1, 1) == "\\")
                driveLetter = driveLetter.Replace("\\", "");
            functionReturnValue = "";

            List<string> nwDrives = new List<string>();
            string[] parts = null;

            // Should set this to null
            NETRESOURCE oNeteResource = new NETRESOURCE();

            if (GetNetworkDrives(ref oNeteResource
                , ref nwDrives))
            {
                foreach (string driveMapping in nwDrives)
                {
                    parts = driveMapping.Split('=');
                    if (parts[0].Trim().ToLower() == driveLetter.Trim().ToLower())
                    {
                        return parts[1];
                    }
                }
            }
            return functionReturnValue;
        }

        // Usage:
        //Dim nwDrives As New List(Of String)
        //GetNetworkDrives(Nothing, nwDrives)

        //For Each item As String In nwDrives
        //    'ListBox1.Items.Add(item)
        //Next
        public bool GetNetworkDrives(ref NETRESOURCE o
            , ref List<string> networkDriveCollection)
        {
            bool functionReturnValue = false;

            int iRet = 0;
            IntPtr ptrHandle = new IntPtr();

            try
            {
                iRet = NativeMethods.WNetOpenEnum(RESOURCE_SCOPE.RESOURCE_REMEMBERED
                    , RESOURCE_TYPE.RESOURCETYPE_ANY
                    , RESOURCE_USAGE.RESOURCEUSAGE_ATTACHED
                    , ref o
                    , ref ptrHandle);
                
                if (iRet != 0)
                    return functionReturnValue;

                int entries = 0;
                int buffer = 16384;
                IntPtr ptrBuffer = Marshal.AllocHGlobal(buffer);
                NETRESOURCE nr = default(NETRESOURCE);

                do
                {
                    entries = -1;
                    buffer = 16384;
                    iRet = NativeMethods.WNetEnumResource(ptrHandle
                        , ref entries
                        , ptrBuffer
                        , ref buffer);

                    if (iRet != 0 | entries < 1)
                        break; // TODO: might not be correct. Was : Exit Do

                    Int32 ptr = ptrBuffer.ToInt32();
                    for (int count = 0; count <= entries - 1; count++)
                    {
                        nr = (NETRESOURCE)Marshal.PtrToStructure(new IntPtr(ptr), typeof(NETRESOURCE));
                        if ((RESOURCE_USAGE.RESOURCEUSAGE_CONTAINER == ( nr.dwUsage & RESOURCE_USAGE.RESOURCEUSAGE_CONTAINER)))
                        {
                            if (!GetNetworkDrives(ref nr, ref networkDriveCollection))
                            {
                                throw new Exception("");
                            }
                        }

                        ptr += Marshal.SizeOf(nr);
                        networkDriveCollection.Add(string.Format( nr.lpLocalName + "=" + nr.lpRemoteName));
                    }
                } while (true);
                Marshal.FreeHGlobal(ptrBuffer);
                iRet = NativeMethods.WNetCloseEnum(ptrHandle);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.Message))
                    networkDriveCollection.Add(ex.Message);
                return false;
            }

            return true;
        }

        // Usage:
        //Dim nwComputers As New List(Of String)
        //GetNetworkComputers(Nothing, nwComputers)

        //For Each item As String In nwComputers
        //    ListBox1.Items.Add(item)
        //Next
        public bool GetNetworkComputers(ref NETRESOURCE o
            , ref List<string> networkComputersCollection)
        {
            bool functionReturnValue = false;

            int iRet = 0;
            IntPtr ptrHandle = new IntPtr();

            try
            {
                iRet = NativeMethods.WNetOpenEnum(RESOURCE_SCOPE.RESOURCE_GLOBALNET
                    , RESOURCE_TYPE.RESOURCETYPE_ANY
                    , RESOURCE_USAGE.RESOURCEUSAGE_CONTAINER
                    , ref o
                    , ref ptrHandle);

                if (iRet != 0)
                    return functionReturnValue;

                int entries = 0;
                int buffer = 16384;
                IntPtr ptrBuffer = Marshal.AllocHGlobal(buffer);
                NETRESOURCE nr = default(NETRESOURCE);

                do
                {
                    entries = -1;
                    buffer = 16384;
                    iRet = NativeMethods.WNetEnumResource(ptrHandle
                        , ref entries
                        , ptrBuffer
                        , ref buffer);

                    if (iRet != 0 | entries < 1)
                        break; // TODO: might not be correct. Was : Exit Do

                    Int32 ptr = ptrBuffer.ToInt32();
                    for (int count = 0; count <= entries - 1; count++)
                    {
                        nr = (NETRESOURCE)Marshal.PtrToStructure(new IntPtr(ptr), typeof(NETRESOURCE));
                        if ((RESOURCE_USAGE.RESOURCEUSAGE_CONTAINER == ( nr.dwUsage & RESOURCE_USAGE.RESOURCEUSAGE_CONTAINER)))
                        {
                            if (!GetNetworkComputers(ref nr, ref networkComputersCollection))
                            {
                                throw new Exception("");
                            }
                        }

                        ptr += Marshal.SizeOf(nr);
                        if ( nr.lpRemoteName.Length > 2)
                        {
                            if (nr.lpRemoteName.Substring(0, 2) == "\\\\")
                            {
                                networkComputersCollection.Add(string.Format(nr.lpRemoteName.Remove(0, 2)));
                            }
                        }

                    }
                } while (true);
                Marshal.FreeHGlobal(ptrBuffer);
                iRet = NativeMethods.WNetCloseEnum(ptrHandle);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.Message))
                    networkComputersCollection.Add(ex.Message);
                return false;
            }

            return true;
        }

        private bool WNETOE(ref NETRESOURCE o
            , ref List<string> resourceCollection)
        {
            bool functionReturnValue = false;

            int iRet = 0;
            IntPtr ptrHandle = new IntPtr();

            try
            {
                iRet = NativeMethods.WNetOpenEnum(RESOURCE_SCOPE.RESOURCE_GLOBALNET
                    , RESOURCE_TYPE.RESOURCETYPE_ANY
                    , RESOURCE_USAGE.RESOURCEUSAGE_CONTAINER
                    , ref o
                    , ref ptrHandle);

                if (iRet != 0)
                    return functionReturnValue;

                int entries = 0;
                int buffer = 16384;
                IntPtr ptrBuffer = Marshal.AllocHGlobal(buffer);
                NETRESOURCE nr = default(NETRESOURCE);

                do
                {
                    entries = -1;
                    buffer = 16384;
                    iRet = NativeMethods.WNetEnumResource(ptrHandle
                        , ref entries
                        , ptrBuffer
                        , ref buffer);

                    if (iRet != 0 | entries < 1)
                        break; // TODO: might not be correct. Was : Exit Do

                    Int32 ptr = ptrBuffer.ToInt32();
                    for (int count = 0; count <= entries - 1; count++)
                    {
                        nr = (NETRESOURCE)Marshal.PtrToStructure(new IntPtr(ptr), typeof(NETRESOURCE));
                        if ((RESOURCE_USAGE.RESOURCEUSAGE_CONTAINER == ( nr.dwUsage & RESOURCE_USAGE.RESOURCEUSAGE_CONTAINER)))
                        {
                            if (!WNETOE(ref nr, ref resourceCollection))
                            {
                                throw new Exception("");
                            }
                        }

                        ptr += Marshal.SizeOf(nr);
                        resourceCollection.Add(string.Format(nr.lpLocalName + " = " + nr.lpRemoteName));
                    }
                } while (true);
                Marshal.FreeHGlobal(ptrBuffer);
                iRet = NativeMethods.WNetCloseEnum(ptrHandle);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.Message))
                    resourceCollection.Add(ex.Message);
                return false;
            }

            return true;

        }
    }
}
