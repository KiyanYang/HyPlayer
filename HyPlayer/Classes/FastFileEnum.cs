// Refers to: https://blog.richasy.cn/uwp-file-io/
// Author: Richasy (github @Richasy)

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FileAttributes = System.IO.FileAttributes;

namespace HyPlayer.Classes
{
    class FastFileEnum
    {
        public const int FIND_FIRST_EX_LARGE_FETCH = 2;
        public enum FINDEX_INFO_LEVELS
        {
            FindExInfoStandard = 0,
            FindExInfoBasic = 1
        }

        public enum FINDEX_SEARCH_OPS
        {
            FindExSearchNameMatch = 0,
            FindExSearchLimitToDirectories = 1,
            FindExSearchLimitToDevices = 2
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindFirstFileExFromApp(
            string lpFileName,
            FINDEX_INFO_LEVELS fInfoLevelId,
            out WIN32_FIND_DATA lpFindFileData,
            FINDEX_SEARCH_OPS fSearchOp,
            IntPtr lpSearchFilter,
            int dwAdditionalFlags);

        [DllImport("api-ms-win-core-file-l1-1-0.dll", CharSet = CharSet.Unicode)]
        static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("api-ms-win-core-file-l1-1-0.dll")]
        static extern bool FindClose(IntPtr hFindFile);


        public static async Task<int> FindFilesWithWin32(string folderPath, HashSet<string> fileList)
        {
            if (folderPath.Contains("iTunes") || folderPath.Contains("Cache")) return 0; // Itunes 的空文件夹, 扫描时间指数级增长
            WIN32_FIND_DATA findData;
            FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
            int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;
            IntPtr hFile = FindFirstFileExFromApp(folderPath + "\\\\*.*",
                findInfoLevel,
                out findData,
                FINDEX_SEARCH_OPS.FindExSearchNameMatch,
                IntPtr.Zero,
                additionalFlags);
            if (hFile != IntPtr.Zero)
            {
                do
                {
                    if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
                    {
                        fileList.Add(folderPath + "\\" + findData.cFileName);
                    }
                    else
                    {
                        if (findData.cFileName != "." && findData.cFileName != "..")
                            await FindFilesWithWin32(folderPath + "\\\\" + findData.cFileName, fileList);
                    }
                } while (FindNextFile(hFile, out findData));
                FindClose(hFile);
            }
            return 0;
        }
    }
}