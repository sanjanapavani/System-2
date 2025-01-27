Imports System.Numerics
Imports System.Runtime.InteropServices
Public Class FFTW_VB

    Public Enum fftw_flags As UInteger
        ''' <summary>
        ''' Tells FFTW to find an optimized plan by actually computing several FFTs and measuring their execution time. 
        ''' Depending on your machine, this can take some time (often a few seconds). Default (0x0). 
        ''' </summary>
        Measure = 0

        ''' <summary>
        ''' Specifies that an out-of-place transform is allowed to overwrite its 
        ''' input array with arbitrary data; this can sometimes allow more efficient algorithms to be employed.
        ''' </summary>
        DestroyInput = 1
        ''' <summary>
        ''' Rarely used. Specifies that the algorithm may not impose any unusual alignment requirements on the input/output 
        ''' arrays (i.e. no SIMD). This flag is normally not necessary, since the planner automatically detects 
        ''' misaligned arrays. The only use for this flag is if you want to use the guru interface to execute a given 
        ''' plan on a different array that may not be aligned like the original. 
        ''' </summary>
        Unaligned = 2
        ''' <summary>
        ''' Not used.
        ''' </summary>
        ConserveMemory = 4
        ''' <summary>
        ''' Like Patient, but considers an even wider range of algorithms, including many that we think are 
        ''' unlikely to be fast, to produce the most optimal plan but with a substantially increased planning time. 
        ''' </summary>
        Exhaustive = 8
        ''' <summary>
        ''' Specifies that an out-of-place transform must not change its input array. 
        ''' </summary>
        ''' <remarks>
        ''' This is ordinarily the default, 
        ''' except for c2r and hc2r (i.e. complex-to-real) transforms for which DestroyInput is the default. 
        ''' In the latter cases, passing PreserveInput will attempt to use algorithms that do not destroy the 
        ''' input, at the expense of worse performance; for multi-dimensional c2r transforms, however, no 
        ''' input-preserving algorithms are implemented and the planner will return null if one is requested.
        ''' </remarks>
        PreserveInput = 16
        ''' <summary>
        ''' Like Measure, but considers a wider range of algorithms and often produces a “more optimal” plan 
        ''' (especially for large transforms), but at the expense of several times longer planning time 
        ''' (especially for large transforms).
        ''' </summary>
        Patient = 32
        ''' <summary>
        ''' Specifies that, instead of actual measurements of different algorithms, a simple heuristic is 
        ''' used to pick a (probably sub-optimal) plan quickly. With this flag, the input/output arrays 
        ''' are not overwritten during planning. 
        ''' </summary>
        Estimate = 64
    End Enum


    <DllImport("libfftw3f-3.dll", EntryPoint:="fftwf_plan_dft_2d", ExactSpelling:=True, CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Function dft_2d(nx As Integer, ny As Integer, input As IntPtr, output As IntPtr,
           ByRef direction As Integer, flags As fftw_flags) As IntPtr
    End Function


    <DllImport("libfftw3f-3.dll",
     EntryPoint:="fftwf_plan_dft_1d",
     ExactSpelling:=True,
     CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Function dft_1d(n As Integer, input As IntPtr, output As IntPtr,
             direction As Integer, flags As fftw_flags) As IntPtr
    End Function


    <DllImport("libfftw3f-3.dll",
     EntryPoint:="fftwf_malloc",
     ExactSpelling:=True,
     CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Function malloc(ByRef length As Integer) As IntPtr


    End Function


    <DllImport("libfftw3f-3.dll",
         EntryPoint:="fftwf_execute",
         ExactSpelling:=True,
         CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Sub execute(plan As IntPtr)
    End Sub

    <DllImport("libfftw3f-3.dll",
             EntryPoint:="fftwf_plan_dft_r2c_2d",
             ExactSpelling:=True,
             CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Function dft_r2c_2d(nx As Integer, ny As Integer, input As IntPtr, output As IntPtr, flags As fftw_flags) As IntPtr
    End Function

    <DllImport("libfftw3f-3.dll",
     EntryPoint:="fftwf_free",
     ExactSpelling:=True,
     CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Sub free(mem As IntPtr)
    End Sub



    <DllImport("libfftw3f-3.dll",
         EntryPoint:="fftwf_destroy_plan",
         ExactSpelling:=True,
         CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Sub destroy_plan(plan As IntPtr)
    End Sub

    Dim fplan As New IntPtr
    Dim hin, hout As New GCHandle
    Dim fout() As Single
    Dim fin() As Single
    Dim nx, ny As Integer
    Dim MTF() As Single
    Public Sub CreatPlan(nx As Integer, ny As Integer)

    End Sub

    Public Sub New(DimX As Integer, DimY As Integer, MTFin() As Single)
        nx = DimX
        ny = DimY
        ReDim fout(nx * ny * 2 - 1)
        ReDim fin(nx * ny * 2 - 1)
        'pointers to the FFTW plan objects
        hin = GCHandle.Alloc(fin, GCHandleType.Pinned)
        hout = GCHandle.Alloc(fout, GCHandleType.Pinned)
        fplan = dft_2d(ny, nx, hin.AddrOfPinnedObject(), hout.AddrOfPinnedObject(), -1, 64)
        MTF = MTFin
    End Sub


    Public Sub New(DimX As Integer, DimY As Integer)
        nx = DimX
        ny = DimY
        ReDim fout(nx * ny * 2 - 1)
        ReDim fin(nx * ny * 2 - 1)
        'pointers to the FFTW plan objects
        hin = GCHandle.Alloc(fin, GCHandleType.Pinned)
        hout = GCHandle.Alloc(fout, GCHandleType.Pinned)
        fplan = dft_2d(ny, nx, hin.AddrOfPinnedObject(), hout.AddrOfPinnedObject(), -1, 64)

    End Sub




    Public Function DFT2D_MTF(ByVal finput() As Single) As Byte()

        Array.Copy(finput, fin, finput.GetUpperBound(0))

        execute(fplan)

        Dim nmax As Integer = nx * ny * 2 - 1
        'Multiplying the MTF which also includes the Lukosz bound
        For n = 0 To nmax Step 2

            fin(n) = (fout(n) * MTF(n) - fout(n + 1) * MTF(n + 1))
            '- is for cmplex conjugate to make the image flipped properly
            fin(n + 1) = -(fout(n + 1) * MTF(n) + fout(n) * MTF(n + 1))
        Next

        execute(fplan)


        Dim pmax = nx * ny - 1
        Dim foutbyte(pmax) As Byte

        Dim C As Single
        Dim p As Integer = 0
        For p = 0 To pmax
            C = fout(p * 2)
            If C < 255 And C > 0 Then foutbyte(p) = C
            If C > 255 Then foutbyte(p) = 255
        Next




        Return foutbyte
    End Function



    Public Function DFT2D_MTF(ByVal finput(,) As Single) As Single(,)

        'Copying into a linear array 
        Dim i As Integer = 0
        For y = 0 To ny - 1
            For x = 0 To nx - 1
                fin(i) = finput(x, y)
                fin(i + 1) = 0
                i += 2
            Next
        Next


        '  Array.Copy(finput, fin, finput.GetUpperBound(0))

        execute(fplan)

        Dim nmax As Integer = nx * ny * 2 - 1
        'Multiplying the MTF which also includes the Lukosz bound
        For n = 0 To nmax Step 2

            fin(n) = (fout(n) * MTF(n) - fout(n + 1) * MTF(n + 1))
            '- is for cmplex conjugate to make the image flipped properly
            fin(n + 1) = -(fout(n + 1) * MTF(n) + fout(n) * MTF(n + 1))
        Next

        execute(fplan)


        Dim fout2D(nx - 1, ny - 1) As Single
        i = 0
        For y = 0 To ny - 1
            For x = 0 To nx - 1
                fout2D(x, y) = fout(i)

                i += 2
            Next
        Next


        Return fout2D
    End Function

    Public Function DFT2D(fin() As Single, nx As Integer, ny As Integer) As Single()
        Dim hin, hout As GCHandle
        Dim fplan As IntPtr
        Dim fout(nx * ny * 2 - 1) As Single
        'pointers to the FFTW plan objects
        hin = GCHandle.Alloc(fin, GCHandleType.Pinned)
        hout = GCHandle.Alloc(fout, GCHandleType.Pinned)
        fplan = dft_2d(nx, ny, hin.AddrOfPinnedObject(), hout.AddrOfPinnedObject(), -1, 64)
        execute(fplan)
        Return fout
    End Function



    Public Function DFT2DC(fin() As Single, nx As Integer, ny As Integer) As Complex()
        Dim hin, hout As GCHandle
        Dim fplan As IntPtr
        Dim fout(nx * ny * 2 - 1) As Single
        Dim foutC(nx * ny - 1) As Complex
        'pointers to the FFTW plan objects
        hin = GCHandle.Alloc(fin, GCHandleType.Pinned)
        hout = GCHandle.Alloc(fout, GCHandleType.Pinned)
        fplan = dft_2d(ny, nx, hin.AddrOfPinnedObject(), hout.AddrOfPinnedObject(), -1, 64)
        execute(fplan)
        Dim nmax As Integer = nx * ny - 1

        For n = 0 To nmax
            foutC(n) = New Complex(fout(2 * n), -fout(2 * n + 1))
        Next
        Return foutC
    End Function


    Public Function DFT2DM(fin() As Single) As Single()
        Dim hin, hout As GCHandle
        Dim fplan As IntPtr
        Dim fout(nx * ny * 2 - 1) As Single
        Dim foutC(nx * ny - 1) As Single
        'pointers to the FFTW plan objects
        hin = GCHandle.Alloc(fin, GCHandleType.Pinned)
        hout = GCHandle.Alloc(fout, GCHandleType.Pinned)
        fplan = dft_2d(ny, nx, hin.AddrOfPinnedObject(), hout.AddrOfPinnedObject(), -1, 64)
        execute(fplan)
        Dim nmax As Integer = nx * ny - 1

        For n = 0 To nmax
            foutC(n) = (fout(2 * n) ^ 2 + fout(2 * n + 1) ^ 2)
        Next
        Return foutC
    End Function
End Class
