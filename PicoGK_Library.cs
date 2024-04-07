//
// SPDX-License-Identifier: Apache-2.0
//
// PicoGK ("peacock") is a compact software kernel for computational geometry,
// specifically for use in Computational Engineering Models (CEM).
//
// For more information, please visit https://picogk.org
// 
// PicoGK is developed and maintained by LEAP 71 - © 2023 by LEAP 71
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
using System.Threading;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;

namespace PicoGK
{
    public partial class Library
    {
        public static void InitLibrary(float voxelSize, float meshAdaptivity, bool triangulateMeshes, uint meshCoarseningFactor)
        {
            fVoxelSizeMM = voxelSize;
            fMeshAdaptivity = meshAdaptivity;
            bTriangulateMeshes = triangulateMeshes;
            iMeshCoarseningFactor = meshCoarseningFactor;

            _Init(voxelSize, triangulateMeshes, meshAdaptivity);
        }


        /// <summary>
        /// Set a coarsening factor for the intermediate meshes.
        /// </summary>
        /// <param name="meshCoarseningFactor"></param>
        public static void SetMeshCoarseningFactor(uint meshCoarseningFactor)
        {
            iMeshCoarseningFactor = meshCoarseningFactor;
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
        /// Checks whether the task started using Go() should continue, and returns true if that's the case or false otherwise.
        /// If your task can take a non-trivial amount of time, check this function periodically.
        /// If it returns false, exit the task function as soon as possible.
        /// </summary>
        /// <param name="bAppExitOnly">If true, the bContinueTask function will only take into consideration if the application is about to exit. Any pending EndTask() requests will be ignored.</param>
        /// <returns></returns>
        public static bool bContinueTask(bool bAppExitOnly = false)
        {
            return !m_bAppExit && (bAppExitOnly || m_bContinueTask);
        }

        /// <summary>
        /// Requests the task started by the Go() function to end.
        /// Note that it's the responsability of the task to call the bContinueTask() function periodically and to honor these requests.
        /// </summary>
        public static void EndTask()
        {
            m_bContinueTask = false;
        }

        /// <summary>
        /// Cancels any pending request to end the task.
        /// </summary>
        public static void CancelEndTaskRequest()
        {
            m_bContinueTask = true;
        }

        static bool m_bAppExit = false;
        static bool m_bContinueTask = true;

        /// <summary>
        /// Thread-safe loging function
        /// </summary>
        /// <param name="strFormat">
        /// Use like Console.Write and others, so you can do Library.Log($"My variable {fVariable}") etc.
        /// </param>
        /// <exception cref="Exception">
        /// Will throw an exception if called before you call Library::Go, which shouldn't happen
        /// </exception>
        public static void Log(in string strFormat, params object[] args)
        {
            lock (oMtxLog)
            {
                if (oTheLog == null)
                    throw new Exception("Trying to access Log before Library::Go() was called");

                oTheLog.Log(strFormat, args);
            }
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

            Vector3     vec3    = new Vector3();
            Vector2     vec2    = new Vector2();
            Matrix4x4   mat4    = new Matrix4x4();
            Coord       xyz     = new Coord(0, 0, 0);
            Triangle    tri     = new Triangle(0, 0, 0);
            Quad        quad    = new Quad(0, 0, 0, 0);
            ColorFloat  clr     = new ColorFloat(0f);
            BBox2       oBB2    = new BBox2();
            BBox3       oBB3    = new BBox3();

            Debug.Assert(sizeof(bool)           == 1);                  // 8 bit for bool assumed
            Debug.Assert(Marshal.SizeOf(vec3)   == ((32 * 3) / 8));     // 3 x 32 bit float
            Debug.Assert(Marshal.SizeOf(vec2)   == ((32 * 2) / 8));     // 2 x 32 bit float
            Debug.Assert(Marshal.SizeOf(mat4)   == ((32 * 16) / 8));    // 4 x 4 x 32 bit float 
            Debug.Assert(Marshal.SizeOf(xyz)    == ((32 * 3) / 8));     // 3 x 32 bit integer
            Debug.Assert(Marshal.SizeOf(tri)    == ((32 * 3) / 8));     // 3 x 32 bit integer
            Debug.Assert(Marshal.SizeOf(quad)   == ((32 * 3) / 8));     // 3 x 32 bit integer
            Debug.Assert(Marshal.SizeOf(clr)    == ((32 * 4) / 8));     // 4 x 32 bit float
            Debug.Assert(Marshal.SizeOf(oBB2)   == ((32 * 2 * 2) / 8)); // 2 x vec2
            Debug.Assert(Marshal.SizeOf(oBB3)   == ((32 * 3 * 2) / 8)); // 2 x vec3

            // If any of these assert, then something is wrong with the
            // memory layout, and the interface to compatible C libraries
            // will fail - this should never happen, as all these types
            // are well-defined
        }

        /// <summary>
        /// Logs the information from the library, usually the first line of
        /// defence, if something is misconfigured, for example the library path
        /// Also attempts to create all data types - if this blows up, then
        /// something is wrong with the C++/C# interplay
        /// </summary>
        /// <returns></returns>
        private static bool bSetup()
        {
            try
            {
                Log($"PicoGK:    {Library.strName()}");
                Log($"           {Library.strVersion()}");
                Log($"           {Library.strBuildInfo()}\n");
                Log($"VoxelSize: {Library.fVoxelSizeMM} (mm)");

                Log("Happy Computational Engineering!\n\n");
            }

            catch (Exception e)
            {
                Log("Failed to get PicoGK library info:\n\n{0}", e.Message);
                return false;
            }

            try
            {
                Lattice     lat     = new Lattice();
                Voxels      vox     = new Voxels();
                Mesh        msh     = new Mesh();
                Voxels      voxM    = new Voxels(msh);
                Voxels      voxL    = new Voxels(lat);
                PolyLine    oPoly   = new PolyLine("FF0000");
            }

            catch (Exception e)
            {
                Log("Failed to instantiate basic PicoGK types:\n\n{0}", e.Message);
                return false;
            }

            return true;
        }

        public static float fMeshAdaptivity;
        public static uint iMeshCoarseningFactor = 4;
        public static bool bTriangulateMeshes = false;

        public static   float   fVoxelSizeMM = 0.0f;
        public static   string  strLogFolder = "";
        public static   string  strSrcFolder = "";

        private static object   oMtxLog     = new object();
        private static object   oMtxViewer  = new object();
        private static LogFile oTheLog     = null;
    }
}