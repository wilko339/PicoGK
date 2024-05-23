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

using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace PicoGK
{
    public partial class Library
    {
        public static void InitLibrary(float voxelSize, float meshAdaptivity)
        {
            fVoxelSizeMM = voxelSize;
            fMeshAdaptivity = meshAdaptivity;

            _InitLibrary(voxelSize,  meshAdaptivity);
        }
        /// <summary>
        /// Returns the library name (from the C++ side)
        /// </summary>
        /// <returns>The name of the dynamically loaded C++ library</returns>
        public static string strName()
        {
            StringBuilder oBuilder = new StringBuilder(Library.nStringLength);
            _GetName(oBuilder);
            return oBuilder.ToString();
        }

        /// <summary>
        /// Returns the library version (from the C++ side)
        /// </summary>
        /// <returns>The library version of the C++ library</returns>
        public static string strVersion()
        {
            StringBuilder oBuilder = new StringBuilder(Library.nStringLength);
            _GetVersion(oBuilder);
            return oBuilder.ToString();
        }

        /// <summary>
        /// Returns internal build info, such as build date/time
        /// of the C++ library
        /// </summary>
        /// <returns>Internal build info of the C++ library</returns>
        public static string strBuildInfo()
        {
            StringBuilder oBuilder = new StringBuilder(Library.nStringLength);
            _GetBuildInfo(oBuilder);
            return oBuilder.ToString();
        }

        /// <summary>
        /// This is an internal helper that tests if the data types have the
        /// memory layout that we assume, so we don't run into interoperability issues
        /// with the C++ side
        /// </summary>
        private static void TestAssumptions()
        {
            // Test a few assumptions
            // Built in data type Vector3 is implicit,
            // so should be compatible with our own
            // structs, but let's be sure

            Vector3     vec3    = new Vector3    ();
            Vector2     vec2    = new Vector2    ();
            Matrix4x4   mat4    = new Matrix4x4  ();
            Coord       xyz     = new Coord      (0, 0, 0);
            Triangle    tri     = new Triangle   (0, 0, 0);
            ColorFloat  clr     = new ColorFloat (0f);
            BBox2       oBB2    = new BBox2      ();
            BBox3       oBB3    = new BBox3();

            Debug.Assert(sizeof(bool)           == 1);                  // 8 bit for bool assumed
            Debug.Assert(Marshal.SizeOf(vec3)   == ((32 * 3) / 8));     // 3 x 32 bit float
            Debug.Assert(Marshal.SizeOf(vec2)   == ((32 * 2) / 8));     // 2 x 32 bit float
            Debug.Assert(Marshal.SizeOf(mat4)   == ((32 * 16) / 8));    // 4 x 4 x 32 bit float 
            Debug.Assert(Marshal.SizeOf(xyz)    == ((32 * 3) / 8));     // 3 x 32 bit integer
            Debug.Assert(Marshal.SizeOf(tri)    == ((32 * 3) / 8));     // 3 x 32 bit integer
            Debug.Assert(Marshal.SizeOf(clr)    == ((32 * 4) / 8));     // 4 x 32 bit float
            Debug.Assert(Marshal.SizeOf(oBB2)   == ((32 * 2 * 2) / 8)); // 2 x vec2
            Debug.Assert(Marshal.SizeOf(oBB3)   == ((32 * 3 * 2) / 8)); // 2 x vec3

            // If any of these assert, then something is wrong with the
            // memory layout, and the interface to compatible C libraries
            // will fail - this should never happen, as all these types
            // are well-defined
        }

        public static Vector3 vecVoxelsToMm(    int x,
                                                int y,
                                                int z)
        {
            Vector3 vecMm = new Vector3();
            Vector3 vecVoxels   = new Vector3(  (float) x,
                                                (float) y,
                                                (float) z);
            _VoxelsToMm(    in vecVoxels,
                            ref vecMm);

            return vecMm;
        }

         public static void MmToVoxels( Vector3 vecMm,
                                        out int x,
                                        out int y,
                                        out int z)
        {
            Vector3 vecResult   = Vector3.Zero;

            _VoxelsToMm(    in vecMm,
                            ref vecResult);

            x = (int) (vecResult.X + 0.5f);
            y = (int) (vecResult.Y + 0.5f);
            z = (int) (vecResult.Z + 0.5f);
        }

        public static float fMeshAdaptivity;
        public static bool bTriangulateMeshes = false;

        public static   float   fVoxelSizeMM = 0.0f;
        public static   string  strLogFolder = "";
        public static   string  strSrcFolder = "";

        private static object   oMtxLog     = new object();
        private static object   oMtxViewer  = new object();
    }
}