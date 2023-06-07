﻿using AuremCore.BN256.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.BN256.Native
{
    internal class Native: IDisposable
    {
        public static Native Instance;

        private static IntPtr _handle = IntPtr.Zero;

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                NativeLibrary.Free(_handle);

                disposedValue = true;
            }
        }
        ~Native()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public delegate int ScalarBitlenDelegate(ref Scalar k);
        public delegate int ScalarBitDelegate(ref Scalar k, int i);
        public delegate void GFpMulDelegate([In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] c,
                                            [In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] a,
                                            [In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] b);
        public delegate void NewGFpDelegate([In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] a, long x);
        public delegate void GFpNegDelegate([In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] c,
            [In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] a);
        public delegate void RandomG1Delegate(ref G1 g1, ref Scalar k);
        public delegate void ScalarBaseMultG1Delegate(ref G1 gk, ref Scalar k);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate G1 ScalarBaseMultG1_1Delegate(ref Scalar k);
        public delegate void ScalarMultG1Delegate(ref G1 ak, ref G1 a, ref Scalar k);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate G1 ScalarMultG1_1Delegate(ref G1 a, ref Scalar k);
        public delegate void AddG1Delegate(ref G1 ab, ref G1 a, ref G1 b);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate G1 AddG1_1Delegate(ref G1 a, ref G1 b);
        public delegate void NegG1Delegate(ref G1 na, ref G1 a);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate G1 NegG1_1Delegate(ref G1 a);
        public delegate void MarshalG1Delegate(ref G1Enc res, ref G1 a);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate G1Enc MarshalG1_1Delegate(ref G1 a);
        public delegate void UnmarshalG1Delegate(ref G1 a, ref G1Enc e);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate G1 UnmarshalG1_1Delegate(ref G1Enc e);
        public delegate void RandomG2Delegate(ref G2 g2, ref Scalar k);
        public delegate void ScalarBaseMultG2Delegate(ref G2 gk, ref Scalar k);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate G2 ScalarBaseMultG2_1Delegate(ref Scalar k);
        public delegate void ScalarMultG2Delegate(ref G2 ak, ref G2 a, ref Scalar k);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate G2 ScalarMultG2_1Delegate(ref G2 a, ref Scalar k);
        public delegate void AddG2Delegate(ref G2 ab, ref G2 a, ref G2 b);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate G2 AddG2_1Delegate(ref G2 a, ref G2 b);
        public delegate void NegG2Delegate(ref G2 na, ref G2 a);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate G2 NegG2_1Delegate(ref G2 a);
        public delegate void MarshalG2Delegate(ref G2Enc res, ref G2 a);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate G2Enc MarshalG2_1Delegate(ref G2 a);
        public delegate void UnmarshalG2Delegate(ref G2 a, ref G2Enc e);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate G2 UnmarshalG2_1Delegate(ref G2Enc e);
        public delegate void RandomGTDelegate(ref GT gt, ref Scalar k);
        public delegate void ScalarBaseMultGTDelegate(ref GT gk, ref Scalar k);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate GT ScalarBaseMultGT_1Delegate(ref Scalar k);
        public delegate void ScalarMultGTDelegate(ref GT ak, ref GT a, ref Scalar k);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate GT ScalarMultGT_1Delegate(ref GT a, ref Scalar k);
        public delegate void AddGTDelegate(ref GT ab, ref GT a, ref GT b);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate GT AddGT_1Delegate(ref GT a, ref GT b);
        public delegate void NegGTDelegate(ref GT na, ref GT a);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate GT NegGT_1Delegate(ref GT a);
        public delegate void MarshalGTDelegate(ref GTEnc res, ref GT a);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate GTEnc MarshalGT_1Delegate(ref GT a);
        public delegate void UnmarshalGTDelegate(ref GT a, ref GTEnc e);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate GT UnmarshalGT_1Delegate(ref GTEnc e);
        public delegate void PairDelegate(ref GT gt, ref G1 p, ref G2 q);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate GT Pair_1Delegate(ref G1 p, ref G2 q);
        public delegate void MillerDelegate(ref GT gt, ref G1 p, ref G2 q);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate GT Miller_1Delegate(ref G1 p, ref G2 q);
        public delegate void FinalizeDelegate(ref GT gt);

        public ScalarBitlenDelegate ScalarBitlen;
        public ScalarBitDelegate ScalarBit;
        public GFpMulDelegate GFpMul;
        public NewGFpDelegate NewGFp;
        public GFpNegDelegate GFpNeg;
        public RandomG1Delegate RandomG1;
        public ScalarBaseMultG1Delegate ScalarBaseMultG1;
        public ScalarBaseMultG1_1Delegate ScalarBaseMultG1_1;
        public ScalarMultG1Delegate ScalarMultG1;
        public ScalarMultG1_1Delegate ScalarMultG1_1;
        public AddG1Delegate AddG1;
        public AddG1_1Delegate AddG1_1;
        public NegG1Delegate NegG1;
        public NegG1_1Delegate NegG1_1;
        public MarshalG1Delegate MarshalG1;
        public MarshalG1_1Delegate MarshalG1_1;
        public UnmarshalG1Delegate UnmarshalG1;
        public UnmarshalG1_1Delegate UnmarshalG1_1;
        public RandomG2Delegate RandomG2;
        public ScalarBaseMultG2Delegate ScalarBaseMultG2;
        public ScalarBaseMultG2_1Delegate ScalarBaseMultG2_1;
        public ScalarMultG2Delegate ScalarMultG2;
        public ScalarMultG2_1Delegate ScalarMultG2_1;
        public AddG2Delegate AddG2;
        public AddG2_1Delegate AddG2_1;
        public NegG2Delegate NegG2;
        public NegG2_1Delegate NegG2_1;
        public MarshalG2Delegate MarshalG2;
        public MarshalG2_1Delegate MarshalG2_1;
        public UnmarshalG2Delegate UnmarshalG2;
        public UnmarshalG2_1Delegate UnmarshalG2_1;
        public RandomGTDelegate RandomGT;
        public ScalarBaseMultGTDelegate ScalarBaseMultGT;
        public ScalarBaseMultGT_1Delegate ScalarBaseMultGT_1;
        public ScalarMultGTDelegate ScalarMultGT;
        public ScalarMultGT_1Delegate ScalarMultGT_1;
        public AddGTDelegate AddGT;
        public AddGT_1Delegate AddGT_1;
        public NegGTDelegate NegGT;
        public NegGT_1Delegate NegGT_1;
        public MarshalGTDelegate MarshalGT;
        public MarshalGT_1Delegate MarshalGT_1;
        public UnmarshalGTDelegate UnmarshalGT;
        public UnmarshalGT_1Delegate UnmarshalGT_1;
        public PairDelegate Pair;
        public Pair_1Delegate Pair_1;
        public MillerDelegate Miller;
        public Miller_1Delegate Miller_1;
        public FinalizeDelegate GTFinalize;

        public delegate void TestIfWorksDelegate(ref int a, int b);
        public TestIfWorksDelegate TestIfWorks;

        static Native()
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.X86 && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException("AuremCore cannot support 32 bit windows");
            }

            bool success;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                success = NativeLibrary.TryLoad("bn256.dll", typeof(bn256).Assembly, DllImportSearchPath.AssemblyDirectory, out _handle);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                success = NativeLibrary.TryLoad("bn256.so", typeof(bn256).Assembly, DllImportSearchPath.AssemblyDirectory, out _handle);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                success = NativeLibrary.TryLoad("bn256.dylib", typeof(bn256).Assembly, DllImportSearchPath.AssemblyDirectory, out _handle);
            }
            else
            {
                success = NativeLibrary.TryLoad("bn256.dll", typeof(bn256).Assembly, DllImportSearchPath.AssemblyDirectory, out _handle);
            }

            if (!success)
            {
                throw new PlatformNotSupportedException("Failed to load \"bn256\" on this platform");
            }

            Instance = new Native();

            Console.WriteLine(_handle.ToString());
            if (NativeLibrary.TryGetExport(_handle, "TestIfWorks", out IntPtr _TestIfWorksHandle))
            {
                Instance.TestIfWorks = Marshal.GetDelegateForFunctionPointer<TestIfWorksDelegate>(_TestIfWorksHandle);
            }
            else
            {
                Instance.TestIfWorks = (ref int a, int b) => { throw new Exception("failed to load this endpoint. Internal fatal exception in native library. "); }; 
            }

            // begin loading endpoints
            if (NativeLibrary.TryGetExport(_handle, "ScalarBitlen", out IntPtr _ScalarBitlenHandle))
            {
                Instance.ScalarBitlen = Marshal.GetDelegateForFunctionPointer<ScalarBitlenDelegate>(_ScalarBitlenHandle);
            }
            else
            {
                Instance.ScalarBitlen = (ref Scalar k) => { throw new EntryPointNotFoundException("failed to find endpoint \"ScalarBitlen\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "ScalarBit", out IntPtr _ScalarBitHandle))
            {
                Instance.ScalarBit = Marshal.GetDelegateForFunctionPointer<ScalarBitDelegate>(_ScalarBitHandle);
            }
            else
            {
                Instance.ScalarBit = (ref Scalar k, int i) => { throw new EntryPointNotFoundException("failed to find endpoint \"ScalarBit\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "GFpMul", out IntPtr _GFpMulHandle))
            {
                Instance.GFpMul = Marshal.GetDelegateForFunctionPointer<GFpMulDelegate>(_GFpMulHandle);
            }
            else
            {
                Instance.GFpMul = (ulong[] c, ulong[] a, ulong[] b) => { throw new EntryPointNotFoundException("failed to find endpoint \"GFpMul\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "NewGFp", out IntPtr _NewGFpHandle))
            {
                Instance.NewGFp = Marshal.GetDelegateForFunctionPointer<NewGFpDelegate>(_NewGFpHandle);
            }
            else
            {
                Instance.NewGFp = (ulong[] a, long x) => { throw new EntryPointNotFoundException("failed to find endpoint \"NewGFp\" in library \"bn256\""); };
            } // GFpNeg
            if (NativeLibrary.TryGetExport(_handle, "GFpNeg", out IntPtr _GFpNegHandle))
            {
                Instance.GFpNeg = Marshal.GetDelegateForFunctionPointer<GFpNegDelegate>(_GFpNegHandle);
            }
            else
            {
                Instance.GFpNeg = (ulong[] c, ulong[] a) => { throw new EntryPointNotFoundException("failed to find endpoint \"GFpNeg\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "RandomG1", out IntPtr _RandomG1Handle))
            {
                Instance.RandomG1 = Marshal.GetDelegateForFunctionPointer<RandomG1Delegate>(_RandomG1Handle);
            }
            else
            {
                Instance.RandomG1 = (ref G1 g1, ref Scalar k) => { throw new EntryPointNotFoundException("failed to find endpoint \"RandomG1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "ScalarBaseMultG1", out IntPtr _ScalarBaseMultG1Handle))
            {
                Instance.ScalarBaseMultG1 = Marshal.GetDelegateForFunctionPointer<ScalarBaseMultG1Delegate>(_ScalarBaseMultG1Handle);
            }
            else
            {
                Instance.ScalarBaseMultG1 = (ref G1 gk, ref Scalar k) => { throw new EntryPointNotFoundException("failed to find endpoint \"ScalarBaseMultG1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "ScalarBaseMultG1_1", out IntPtr _ScalarBaseMultG1_1Handle))
            {
                Instance.ScalarBaseMultG1_1 = Marshal.GetDelegateForFunctionPointer<ScalarBaseMultG1_1Delegate>(_ScalarBaseMultG1_1Handle);
            }
            else
            {
                Instance.ScalarBaseMultG1_1 = (ref Scalar k) => { throw new EntryPointNotFoundException("failed to find endpoint \"ScalarBaseMultG1_1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "ScalarMultG1", out IntPtr _ScalarMultG1Handle))
            {
                Instance.ScalarMultG1 = Marshal.GetDelegateForFunctionPointer<ScalarMultG1Delegate>(_ScalarMultG1Handle);
            }
            else
            {
                Instance.ScalarMultG1 = (ref G1 ak, ref G1 a, ref Scalar k) => { throw new EntryPointNotFoundException("failed to find endpoint \"ScalarMultG1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "ScalarMultG1_1", out IntPtr _ScalarMultG1_1Handle))
            {
                Instance.ScalarMultG1_1 = Marshal.GetDelegateForFunctionPointer<ScalarMultG1_1Delegate>(_ScalarMultG1_1Handle);
            }
            else
            {
                Instance.ScalarMultG1_1 = (ref G1 a, ref Scalar k) => { throw new EntryPointNotFoundException("failed to find endpoint \"ScalarMultG1_1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "AddG1", out IntPtr _AddG1Handle))
            {
                Instance.AddG1 = Marshal.GetDelegateForFunctionPointer<AddG1Delegate>(_AddG1Handle);
            }
            else
            {
                Instance.AddG1 = (ref G1 ab, ref G1 a, ref G1 b) => { throw new EntryPointNotFoundException("failed to find endpoint \"AddG1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "AddG1_1", out IntPtr _AddG1_1Handle))
            {
                Instance.AddG1_1 = Marshal.GetDelegateForFunctionPointer<AddG1_1Delegate>(_AddG1_1Handle);
            }
            else
            {
                Instance.AddG1_1 = (ref G1 a, ref G1 b) => { throw new EntryPointNotFoundException("failed to find endpoint \"AddG1_1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "NegG1", out IntPtr _NegG1Handle))
            {
                Instance.NegG1 = Marshal.GetDelegateForFunctionPointer<NegG1Delegate>(_NegG1Handle);
            }
            else
            {
                Instance.NegG1 = (ref G1 na, ref G1 a) => { throw new EntryPointNotFoundException("failed to find endpoint \"NegG1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "NegG1_1", out IntPtr _NegG1_1Handle))
            {
                Instance.NegG1_1 = Marshal.GetDelegateForFunctionPointer<NegG1_1Delegate>(_NegG1_1Handle);
            }
            else
            {
                Instance.NegG1_1 = (ref G1 a) => { throw new EntryPointNotFoundException("failed to find endpoint \"NegG1_1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "MarshalG1", out IntPtr _MarshalG1Handle))
            {
                Instance.MarshalG1 = Marshal.GetDelegateForFunctionPointer<MarshalG1Delegate>(_MarshalG1Handle);
            }
            else
            {
                Instance.MarshalG1 = (ref G1Enc res, ref G1 a) => { throw new EntryPointNotFoundException("failed to find endpoint \"MarshalG1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "MarshalG1_1", out IntPtr _MarshalG1_1Handle))
            {
                Instance.MarshalG1_1 = Marshal.GetDelegateForFunctionPointer<MarshalG1_1Delegate>(_MarshalG1_1Handle);
            }
            else
            {
                Instance.MarshalG1_1 = (ref G1 a) => { throw new EntryPointNotFoundException("failed to find endpoint \"MarshalG1_1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "UnmarshalG1", out IntPtr _UnmarshalG1Handle))
            {
                Instance.UnmarshalG1 = Marshal.GetDelegateForFunctionPointer<UnmarshalG1Delegate>(_UnmarshalG1Handle);
            }
            else
            {
                Instance.UnmarshalG1 = (ref G1 a, ref G1Enc e) => { throw new EntryPointNotFoundException("failed to find endpoint \"UnmarshalG1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "UnmarshalG1_1", out IntPtr _UnmarshalG1_1Handle))
            {
                Instance.UnmarshalG1_1 = Marshal.GetDelegateForFunctionPointer<UnmarshalG1_1Delegate>(_UnmarshalG1_1Handle);
            }
            else
            {
                Instance.UnmarshalG1_1 = (ref G1Enc e) => { throw new EntryPointNotFoundException("failed to find endpoint \"UnmarshalG1_1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "RandomG2", out IntPtr _RandomG2Handle))
            {
                Instance.RandomG2 = Marshal.GetDelegateForFunctionPointer<RandomG2Delegate>(_RandomG2Handle);
            }
            else
            {
                Instance.RandomG2 = (ref G2 g2, ref Scalar k) => { throw new EntryPointNotFoundException("failed to find endpoint \"RandomG2\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "ScalarBaseMultG2", out IntPtr _ScalarBaseMultG2Handle))
            {
                Instance.ScalarBaseMultG2 = Marshal.GetDelegateForFunctionPointer<ScalarBaseMultG2Delegate>(_ScalarBaseMultG2Handle);
            }
            else
            {
                Instance.ScalarBaseMultG2 = (ref G2 gk, ref Scalar k) => { throw new EntryPointNotFoundException("failed to find endpoint \"ScalarBaseMultG2\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "ScalarBaseMultG2_1", out IntPtr _ScalarBaseMultG2_1Handle))
            {
                Instance.ScalarBaseMultG2_1 = Marshal.GetDelegateForFunctionPointer<ScalarBaseMultG2_1Delegate>(_ScalarBaseMultG2_1Handle);
            }
            else
            {
                Instance.ScalarBaseMultG2_1 = (ref Scalar k) => { throw new EntryPointNotFoundException("failed to find endpoint \"ScalarBaseMultG2_1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "ScalarMultG2", out IntPtr _ScalarMultG2Handle))
            {
                Instance.ScalarMultG2 = Marshal.GetDelegateForFunctionPointer<ScalarMultG2Delegate>(_ScalarMultG2Handle);
            }
            else
            {
                Instance.ScalarMultG2 = (ref G2 ak, ref G2 a, ref Scalar k) => { throw new EntryPointNotFoundException("failed to find endpoint \"ScalarMultG2\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "ScalarMultG2_1", out IntPtr _ScalarMultG2_1Handle))
            {
                Instance.ScalarMultG2_1 = Marshal.GetDelegateForFunctionPointer<ScalarMultG2_1Delegate>(_ScalarMultG2_1Handle);
            }
            else
            {
                Instance.ScalarMultG2_1 = (ref G2 a, ref Scalar k) => { throw new EntryPointNotFoundException("failed to find endpoint \"ScalarMultG2_1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "AddG2", out IntPtr _AddG2Handle))
            {
                Instance.AddG2 = Marshal.GetDelegateForFunctionPointer<AddG2Delegate>(_AddG2Handle);
            }
            else
            {
                Instance.AddG2 = (ref G2 ab, ref G2 a, ref G2 b) => { throw new EntryPointNotFoundException("failed to find endpoint \"AddG2\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "AddG2_1", out IntPtr _AddG2_1Handle))
            {
                Instance.AddG2_1 = Marshal.GetDelegateForFunctionPointer<AddG2_1Delegate>(_AddG2_1Handle);
            }
            else
            {
                Instance.AddG2_1 = (ref G2 a, ref G2 b) => { throw new EntryPointNotFoundException("failed to find endpoint \"AddG2_1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "NegG2", out IntPtr _NegG2Handle))
            {
                Instance.NegG2 = Marshal.GetDelegateForFunctionPointer<NegG2Delegate>(_NegG2Handle);
            }
            else
            {
                Instance.NegG2 = (ref G2 na, ref G2 a) => { throw new EntryPointNotFoundException("failed to find endpoint \"NegG2\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "NegG2_1", out IntPtr _NegG2_1Handle))
            {
                Instance.NegG2_1 = Marshal.GetDelegateForFunctionPointer<NegG2_1Delegate>(_NegG2_1Handle);
            }
            else
            {
                Instance.NegG2_1 = (ref G2 a) => { throw new EntryPointNotFoundException("failed to find endpoint \"NegG2_1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "MarshalG2", out IntPtr _MarshalG2Handle))
            {
                Instance.MarshalG2 = Marshal.GetDelegateForFunctionPointer<MarshalG2Delegate>(_MarshalG2Handle);
            }
            else
            {
                Instance.MarshalG2 = (ref G2Enc res, ref G2 a) => { throw new EntryPointNotFoundException("failed to find endpoint \"MarshalG2\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "MarshalG2_1", out IntPtr _MarshalG2_1Handle))
            {
                Instance.MarshalG2_1 = Marshal.GetDelegateForFunctionPointer<MarshalG2_1Delegate>(_MarshalG2_1Handle);
            }
            else
            {
                Instance.MarshalG2_1 = (ref G2 a) => { throw new EntryPointNotFoundException("failed to find endpoint \"MarshalG2_1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "UnmarshalG2", out IntPtr _UnmarshalG2Handle))
            {
                Instance.UnmarshalG2 = Marshal.GetDelegateForFunctionPointer<UnmarshalG2Delegate>(_UnmarshalG2Handle);
            }
            else
            {
                Instance.UnmarshalG2 = (ref G2 a, ref G2Enc e) => { throw new EntryPointNotFoundException("failed to find endpoint \"UnmarshalG2\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "UnmarshalG2_1", out IntPtr _UnmarshalG2_1Handle))
            {
                Instance.UnmarshalG2_1 = Marshal.GetDelegateForFunctionPointer<UnmarshalG2_1Delegate>(_UnmarshalG2_1Handle);
            }
            else
            {
                Instance.UnmarshalG2_1 = (ref G2Enc e) => { throw new EntryPointNotFoundException("failed to find endpoint \"UnmarshalG2_1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "RandomGT", out IntPtr _RandomGTHandle))
            {
                Instance.RandomGT = Marshal.GetDelegateForFunctionPointer<RandomGTDelegate>(_RandomGTHandle);
            }
            else
            {
                Instance.RandomGT = (ref GT gt, ref Scalar k) => { throw new EntryPointNotFoundException("failed to find endpoint \"RandomGT\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "ScalarBaseMultGT", out IntPtr _ScalarBaseMultGTHandle))
            {
                Instance.ScalarBaseMultGT = Marshal.GetDelegateForFunctionPointer<ScalarBaseMultGTDelegate>(_ScalarBaseMultGTHandle);
            }
            else
            {
                Instance.ScalarBaseMultGT = (ref GT gk, ref Scalar k) => { throw new EntryPointNotFoundException("failed to find endpoint \"ScalarBaseMultGT\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "ScalarBaseMultGT_1", out IntPtr _ScalarBaseMultGT_1Handle))
            {
                Instance.ScalarBaseMultGT_1 = Marshal.GetDelegateForFunctionPointer<ScalarBaseMultGT_1Delegate>(_ScalarBaseMultGT_1Handle);
            }
            else
            {
                Instance.ScalarBaseMultGT_1 = (ref Scalar k) => { throw new EntryPointNotFoundException("failed to find endpoint \"ScalarBaseMultGT_1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "ScalarMultGT", out IntPtr _ScalarMultGTHandle))
            {
                Instance.ScalarMultGT = Marshal.GetDelegateForFunctionPointer<ScalarMultGTDelegate>(_ScalarMultGTHandle);
            }
            else
            {
                Instance.ScalarMultGT = (ref GT ak, ref GT a, ref Scalar k) => { throw new EntryPointNotFoundException("failed to find endpoint \"ScalarMultGT\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "ScalarMultGT_1", out IntPtr _ScalarMultGT_1Handle))
            {
                Instance.ScalarMultGT_1 = Marshal.GetDelegateForFunctionPointer<ScalarMultGT_1Delegate>(_ScalarMultGT_1Handle);
            }
            else
            {
                Instance.ScalarMultGT_1 = (ref GT a, ref Scalar k) => { throw new EntryPointNotFoundException("failed to find endpoint \"ScalarMultGT_1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "AddGT", out IntPtr _AddGTHandle))
            {
                Instance.AddGT = Marshal.GetDelegateForFunctionPointer<AddGTDelegate>(_AddGTHandle);
            }
            else
            {
                Instance.AddGT = (ref GT ab, ref GT a, ref GT b) => { throw new EntryPointNotFoundException("failed to find endpoint \"AddGT\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "AddGT_1", out IntPtr _AddGT_1Handle))
            {
                Instance.AddGT_1 = Marshal.GetDelegateForFunctionPointer<AddGT_1Delegate>(_AddGT_1Handle);
            }
            else
            {
                Instance.AddGT_1 = (ref GT a, ref GT b) => { throw new EntryPointNotFoundException("failed to find endpoint \"AddGT_1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "NegGT", out IntPtr _NegGTHandle))
            {
                Instance.NegGT = Marshal.GetDelegateForFunctionPointer<NegGTDelegate>(_NegGTHandle);
            }
            else
            {
                Instance.NegGT = (ref GT na, ref GT a) => { throw new EntryPointNotFoundException("failed to find endpoint \"NegGT\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "NegGT_1", out IntPtr _NegGT_1Handle))
            {
                Instance.NegGT_1 = Marshal.GetDelegateForFunctionPointer<NegGT_1Delegate>(_NegGT_1Handle);
            }
            else
            {
                Instance.NegGT_1 = (ref GT a) => { throw new EntryPointNotFoundException("failed to find endpoint \"NegGT_1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "MarshalGT", out IntPtr _MarshalGTHandle))
            {
                Instance.MarshalGT = Marshal.GetDelegateForFunctionPointer<MarshalGTDelegate>(_MarshalGTHandle);
            }
            else
            {
                Instance.MarshalGT = (ref GTEnc res, ref GT a) => { throw new EntryPointNotFoundException("failed to find endpoint \"MarshalGT\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "MarshalGT_1", out IntPtr _MarshalGT_1Handle))
            {
                Instance.MarshalGT_1 = Marshal.GetDelegateForFunctionPointer<MarshalGT_1Delegate>(_MarshalGT_1Handle);
            }
            else
            {
                Instance.MarshalGT_1 = (ref GT a) => { throw new EntryPointNotFoundException("failed to find endpoint \"MarshalGT_1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "UnmarshalGT", out IntPtr _UnmarshalGTHandle))
            {
                Instance.UnmarshalGT = Marshal.GetDelegateForFunctionPointer<UnmarshalGTDelegate>(_UnmarshalGTHandle);
            }
            else
            {
                Instance.UnmarshalGT = (ref GT a, ref GTEnc e) => { throw new EntryPointNotFoundException("failed to find endpoint \"UnmarshalGT\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "UnmarshalGT_1", out IntPtr _UnmarshalGT_1Handle))
            {
                Instance.UnmarshalGT_1 = Marshal.GetDelegateForFunctionPointer<UnmarshalGT_1Delegate>(_UnmarshalGT_1Handle);
            }
            else
            {
                Instance.UnmarshalGT_1 = (ref GTEnc e) => { throw new EntryPointNotFoundException("failed to find endpoint \"UnmarshalGT_1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "Pair", out IntPtr _PairHandle))
            {
                Instance.Pair = Marshal.GetDelegateForFunctionPointer<PairDelegate>(_PairHandle);
            }
            else
            {
                Instance.Pair = (ref GT gt, ref G1 p, ref G2 q) => { throw new EntryPointNotFoundException("failed to find endpoint \"Pair\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "Pair_1", out IntPtr _Pair_1Handle))
            {
                Instance.Pair_1 = Marshal.GetDelegateForFunctionPointer<Pair_1Delegate>(_Pair_1Handle);
            }
            else
            {
                Instance.Pair_1 = (ref G1 p, ref G2 q) => { throw new EntryPointNotFoundException("failed to find endpoint \"Pair_1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "Miller", out IntPtr _MillerHandle))
            {
                Instance.Miller = Marshal.GetDelegateForFunctionPointer<MillerDelegate>(_MillerHandle);
            }
            else
            {
                Instance.Miller = (ref GT gt, ref G1 p, ref G2 q) => { throw new EntryPointNotFoundException("failed to find endpoint \"Miller\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "Miller_1", out IntPtr _Miller_1Handle))
            {
                Instance.Miller_1 = Marshal.GetDelegateForFunctionPointer<Miller_1Delegate>(_Miller_1Handle);
            }
            else
            {
                Instance.Miller_1 = (ref G1 p, ref G2 q) => { throw new EntryPointNotFoundException("failed to find endpoint \"Miller_1\" in library \"bn256\""); };
            }
            if (NativeLibrary.TryGetExport(_handle, "Finalize", out IntPtr _FinalizeHandle))
            {
                Instance.GTFinalize = Marshal.GetDelegateForFunctionPointer<FinalizeDelegate>(_FinalizeHandle);
            }
            else
            {
                Instance.GTFinalize = (ref GT gt) => { throw new EntryPointNotFoundException("failed to find endpoint \"Finalize\" in library \"bn256\""); };
            }
        }
    }
}
