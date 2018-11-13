using System;
using System.Runtime.InteropServices;

namespace FFTW {

    public static class FFTW {

        [Flags]
        public enum FFTW_Flags {
            FFTW_MEASURE         = 0,
            FFTW_DESTROY_INPUT   = 1 << 0,
            FFTW_UNALIGNED       = 1 << 1,
            FFTW_CONSERVE_MEMORY = 1 << 2,
            FFTW_EXHAUSTIVE      = 1 << 3,
            FFTW_PREPARE_INPUT   = 1 << 4,
            FFTW_PATIENT         = 1 << 5,
            FFTW_ESTIMATE        = 1 << 6,
            FFTW_WISDOM_ONLY     = 1 << 21
        }

        [Flags]
        public enum FFTW_R2RFlags {
            FFTW_R2HC = 0,
            FFTW_HC2R = 1,
            FFTW_HDT  = 2
        }

        public const string LIB_FFTW_64 = @"libfftw3-3-64.dll";
        public const string LIB_FFTW_32 = @"libfftw3-3-32.dll";


        //

        [DllImport(LIB_FFTW_32, EntryPoint = "fftw_malloc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr fftw_malloc_32(int size);

        [DllImport(LIB_FFTW_64, EntryPoint = "fftw_malloc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr fftw_malloc_64(int size);

        public static IntPtr fftw_malloc(int size) {
            if (Environment.Is64BitProcess)
                return fftw_malloc_64(size);
            else
                return fftw_malloc_32(size);
        }

        //

        [DllImport(LIB_FFTW_32, EntryPoint = "fftw_alloc_real", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr fftw_alloc_real_32(int size);

        [DllImport(LIB_FFTW_64, EntryPoint = "fftw_alloc_real", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr fftw_alloc_real_64(int size);

        public static IntPtr fftw_alloc_real(int size) {
            if (Environment.Is64BitProcess)
                return fftw_alloc_real_64(size);
            else
                return fftw_alloc_real_32(size);
        }

        //


        [DllImport(LIB_FFTW_32, EntryPoint = "fftw_alloc_complex", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr fftw_alloc_complex_32(int size);

        [DllImport(LIB_FFTW_64, EntryPoint = "fftw_alloc_complex", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr fftw_alloc_complex_64(int size);

        public static IntPtr fftw_alloc_complex(int size) {
            if (Environment.Is64BitProcess)
                return fftw_alloc_complex_64(size);
            else
                return fftw_alloc_complex_32(size);
        }

        //

        [DllImport(LIB_FFTW_32, EntryPoint = "fftw_free", CallingConvention = CallingConvention.Cdecl)]
        public static extern void fftw_free_32(IntPtr pData);

        [DllImport(LIB_FFTW_64, EntryPoint = "fftw_free", CallingConvention = CallingConvention.Cdecl)]
        public static extern void fftw_free_64(IntPtr pData);

        public static void fftw_free(IntPtr ptr) {
            if (Environment.Is64BitProcess)
                fftw_free_64(ptr);
            else
                fftw_free_32(ptr);
        }

        //


        [DllImport(LIB_FFTW_32, EntryPoint = "fftw_plan_dft", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr fftw_plan_dft_32(int rank, ref int n, IntPtr pIn, IntPtr pOut, int sign, uint flags);

        [DllImport(LIB_FFTW_64, EntryPoint = "fftw_plan_dft", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr fftw_plan_dft_64(int rank, ref int n, IntPtr pIn, IntPtr pOut, int sign, uint flags);

        public static IntPtr fftw_plan_dft(int rank, ref int n, IntPtr pIn, IntPtr pOut, int sign, uint flags) {
            if (Environment.Is64BitProcess)
                return fftw_plan_dft_64(rank, ref n, pIn, pOut, sign, flags);
            else
                return fftw_plan_dft_32(rank, ref n, pIn, pOut, sign, flags);
        }

        //

        [DllImport(LIB_FFTW_32, EntryPoint = "fftw_plan_dft_r2c", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr fftw_plan_dft_r2c_32(int rank, ref int n, IntPtr pIn, IntPtr pOut, uint flags);

        [DllImport(LIB_FFTW_64, EntryPoint = "fftw_plan_dft_r2c", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr fftw_plan_dft_r2c_64(int rank, ref int n, IntPtr pIn, IntPtr pOut, uint flags);

        private static object _lock_plan = new object();
        public static IntPtr fftw_plan_dft_r2c(int rank, ref int n, IntPtr pIn, IntPtr pOut, uint flags) {
            lock (_lock_plan) {
                if (Environment.Is64BitProcess)
                    return fftw_plan_dft_r2c_64(rank, ref n, pIn, pOut, flags);
                else
                    return fftw_plan_dft_r2c_32(rank, ref n, pIn, pOut, flags);
            }
        }

        //

        [DllImport(LIB_FFTW_32, EntryPoint = "fftw_plan_dft_r2r", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr fftw_plan_dft_r2r_32(int rank, ref int n, IntPtr pIn, IntPtr pOut, ref uint kind, uint flags);

        [DllImport(LIB_FFTW_64, EntryPoint = "fftw_plan_dft_r2r", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr fftw_plan_dft_r2r_64(int rank, ref int n, IntPtr pIn, IntPtr pOut, ref uint kind, uint flags);

        public static IntPtr fftw_plan_dft_r2r(int rank, ref int n, IntPtr pIn, IntPtr pOut, ref uint kind, uint flags) {
            lock (_lock_plan) {
                if (Environment.Is64BitProcess)
                    return fftw_plan_dft_r2r_64(rank, ref n, pIn, pOut, ref kind, flags);
                else
                    return fftw_plan_dft_r2r_32(rank, ref n, pIn, pOut, ref kind, flags);
            }
        }

        //

        [DllImport(LIB_FFTW_32, EntryPoint = "fftw_execute", CallingConvention = CallingConvention.Cdecl)]
        public static extern void fftw_execute_32(IntPtr plan);

        [DllImport(LIB_FFTW_64, EntryPoint = "fftw_execute", CallingConvention = CallingConvention.Cdecl)]
        public static extern void fftw_execute_64(IntPtr plan);

        public static void fftw_execute(IntPtr plan) {
            if (Environment.Is64BitProcess)
                fftw_execute_64(plan);
            else
                fftw_execute_32(plan);
        }

        //

        [DllImport(LIB_FFTW_32, EntryPoint = "fftw_execute_r2r", CallingConvention = CallingConvention.Cdecl)]
        public static extern void fftw_execute_r2r_32(IntPtr plan, IntPtr i, IntPtr o);

        [DllImport(LIB_FFTW_64, EntryPoint = "fftw_execute_r2r", CallingConvention = CallingConvention.Cdecl)]
        public static extern void fftw_execute_r2r_64(IntPtr plan, IntPtr i, IntPtr o);

        public static void fftw_execute_r2r(IntPtr plan, IntPtr i, IntPtr o) {
            if (Environment.Is64BitProcess)
                fftw_execute_r2r_64(plan, i, o);
            else
                fftw_execute_r2r_32(plan, i, o);
        }

        //
    }

}
