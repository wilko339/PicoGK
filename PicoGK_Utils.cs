//
// SPDX-License-Identifier: Apache-2.0
//
// PicoGK ("peacock") is a compact software kernel for computational geometry,
// specifically for use in Computational Engineering Models (CEM).
//
// For more information, please visit https://picogk.org
// 
// PicoGK is developed and maintained by LEAP 71 - © 2023-2024 by LEAP 71
// https://leap71.com
//
// Computational Engineering will profoundly change our physical world in the
// years ahead. Thank you for being part of the journey.
//
// We have developed this library to be used widely, for both commercial and
// non-commercial projects alike. Therefore, we have released it under a 
// permissive open-source license.
//
// The foundation of PicoGK is a thin layer on top of the powerful open-source
// OpenVDB project, which in turn uses many other Free and Open Source Software
// libraries. We are grateful to be able to stand on the shoulders of giants.
//
// LEAP 71 licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with the
// License. You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, THE SOFTWARE IS
// PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED.
//
// See the License for the specific language governing permissions and
// limitations under the License.   
//

using System;
using System.IO;
using System.Numerics;
using System.Diagnostics;

namespace PicoGK
{
    public class Utils
    {
        /// <summary>
        /// Strip quotes of a quoted path like "/usr/lib/" -> /usr/lib/
        /// </summary>
        /// <param name="strPath"></param>Path that is potentially quoted
        /// <returns></returns>Unquoted path
        static public string strStripQuotesFromPath(string strPath)
        {
            if (strPath.StartsWith("\"") && strPath.EndsWith("\""))
            {
                return strPath.Substring(1, strPath.Length - 2);
            }

            return strPath;
        }



        /// <summary>
        /// Returns the path to the source folder of your project, under the
        /// assumption that the executable .NET DLL is in its usual place.
        /// 
        /// This function is can be used to load files in a subdirectory
        /// of your source code, such as the viewer environment or fonts.
        /// </summary>
        /// <returns>The assumed path to the source code root</returns>
        static public string strProjectRootFolder()
        {
            string strPath = strStripQuotesFromPath(Environment.CommandLine);

            for (int n = 0; n < 4; n++)
            {
                strPath = Path.GetDirectoryName(strPath) ?? "";
            }

            return strPath;
        }

        /// <summary>
        /// Returns a file name in the form 20230930_134500 to be used in log files etc.
        /// </summary>
        /// <param name="strPrefix">Prepended before the date/time stamp</param>
        /// <param name="strPostfix">Appended after the date/time stamp</param>
        /// <returns></returns>
        static public string strDateTimeFilename(   in string strPrefix,
                                                    in string strPostfix)
        {
            return strPrefix + DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss") + strPostfix;
        }

        static public void SetMatrixRow(    ref Matrix4x4 mat, uint n,
                                            float f1, float f2, float f3, float f4)
        {
            // An insane person wrote Matrix4x4

            switch (n)
            {
                case 0:
                    mat.M11 = f1;
                    mat.M12 = f2;
                    mat.M13 = f3;
                    mat.M14 = f4;
                    break;
                case 1:
                    mat.M21 = f1;
                    mat.M22 = f2;
                    mat.M23 = f3;
                    mat.M24 = f4;
                    break;
                case 2:
                    mat.M31 = f1;
                    mat.M32 = f2;
                    mat.M33 = f3;
                    mat.M34 = f4;
                    break;
                case 3:
                    mat.M41 = f1;
                    mat.M42 = f2;
                    mat.M43 = f3;
                    mat.M44 = f4;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Matrix 4x4 row index i 0..3");
            }
        }

        static public Matrix4x4 matLookAt(  Vector3 vecEye,
                                            Vector3 vecLookAt)
        {
            Vector3 vecZ = new Vector3(0.0f, 0.0f, 1.0f);

            Vector3 vecView = Vector3.Normalize(vecEye - vecLookAt);
            Vector3 vecRight = Vector3.Normalize(Vector3.Cross(vecZ, vecView));
            Vector3 vecUp = Vector3.Cross(vecView, vecRight);

            Matrix4x4 mat = new Matrix4x4();

            SetMatrixRow(ref mat, 0, vecRight.X, vecUp.X, vecView.X, 0f);
            SetMatrixRow(ref mat, 1, vecRight.Y, vecUp.Y, vecView.Y, 0f);
            SetMatrixRow(ref mat, 2, vecRight.Z, vecUp.Z, vecView.Z, 0f);

            SetMatrixRow(ref mat, 3, -Vector3.Dot(vecRight, vecEye),
                               -Vector3.Dot(vecUp, vecEye),
                               -Vector3.Dot(vecView, vecEye),
                               1.0f);

            return mat;
        }

    }

    /// <summary>
    /// Creates a temporary folder with an arbitrary filename
    /// in the system's default temp directory, which is guaranteed to be
    /// writable (we don't check this, but the system should guarantee it)
    /// Use the "using" syntax to automatically cleanup after the object
    /// runs out of scope. It will clean up all the files inside and then
    /// delete the temporary folder.
    /// Note: It intentionally doesn't delete any subdirectories you may
    /// create. If you create subdirs, please clean them up yourself.
    /// We intentionally do not recursively wipe out everything out of an
    /// abundance of caution.
    ///
    /// Access the temp folder using oFolder.strFolder
    /// 
    /// </summary>
    public class TempFolder : IDisposable
    {
        public TempFolder()
        {
            strFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(strFolder);
        }

        public string strFolder;

        ~TempFolder()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool bDisposing)
        {
            if (m_bDisposed)
            {
                return;
            }

            try
            {
                // We will cleanup all files, but will not cleanup subfolders
                // this would require recursive delete, and I am afraid it
                // could wipe out an entire disk, if used erroneously,
                // however unlikely

                string[] astrFiles = Directory.GetFiles(strFolder);
                foreach (string strFile in astrFiles)
                {
                    File.Delete(strFile);
                }

                // Remove the temp directory
                Directory.Delete(strFolder);
            }
            catch (Exception)
            {
                // Failed to cleanup, hopefully the system will do it for us at some point
            }

            m_bDisposed = true;
        }

        bool m_bDisposed = false;
    }
}