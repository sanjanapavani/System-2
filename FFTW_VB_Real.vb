Imports System.Numerics
Imports System.Runtime.InteropServices
Public Class FFTW_VB_Real

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
    <DllImport("libfftw3f-3.dll",
             EntryPoint:="fftwf_plan_dft_c2r_2d",
             ExactSpelling:=True,
             CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Function dft_c2r_2d(nx As Integer, ny As Integer, input As IntPtr, output As IntPtr, flags As fftw_flags) As IntPtr
    End Function

    Dim fplan_Forward As New IntPtr
    Dim fplan_backward As New IntPtr
    Dim hin_Forward, hout_Forward As New GCHandle
    Dim hin_Backward, hout_Backward As New GCHandle
    Public fout() As Single
    Public fin() As Single
    Dim fin_UpperBound, fout_Backward_UpperBound As Integer
    Public fout_Backward() As Single
    Public fin_Backward() As Single

    Dim fin2D(,) As Single
    Dim nx, ny, nmax As Integer
    Public MTF() As Single
    Dim S() As Single
    Public done As Boolean

    Public Sub New(nX As Integer, nY As Integer)
        Me.nx = nX
        Me.ny = nY
        nmax = nX * (nY + 2) - 1
        ReDim fout(nX * (nY + 2) - 1)
        ReDim fin(nX * nY - 1)
        fin_UpperBound = nX * nY - 1
        fout_Backward_UpperBound = nX * nY - 1

        'pointers to the FFTW plan objects
        hin_Forward = GCHandle.Alloc(fin, GCHandleType.Pinned)
        hout_Forward = GCHandle.Alloc(fout, GCHandleType.Pinned)
        fplan_Forward = dft_r2c_2d(nX, nY, hin_Forward.AddrOfPinnedObject(), hout_Forward.AddrOfPinnedObject(), 0)

        ReDim fin_Backward(nX * (nY + 2) - 1)
        ReDim fout_Backward(nX * nY - 1)

        hin_Backward = GCHandle.Alloc(fin_Backward, GCHandleType.Pinned)
        hout_Backward = GCHandle.Alloc(fout_Backward, GCHandleType.Pinned)
        fplan_backward = dft_c2r_2d(nX, nY, hin_Backward.AddrOfPinnedObject(), hout_Backward.AddrOfPinnedObject(), 0)

        done = False
    End Sub

    Public Sub New(DimX As Integer, DimY As Integer, width As Single, grade As Integer)
        nx = DimX
        ny = DimY
        nmax = nx * (ny + 2) - 1
        ReDim fout(nx * (ny + 2) - 1)
        ReDim fin(nx * ny - 1)
        'pointers to the FFTW plan objects
        hin_Forward = GCHandle.Alloc(fin, GCHandleType.Pinned)
        hout_Forward = GCHandle.Alloc(fout, GCHandleType.Pinned)
        fplan_Forward = dft_r2c_2d(nx, ny, hin_Forward.AddrOfPinnedObject(), hout_Forward.AddrOfPinnedObject(), 0)

        ReDim fin_Backward(nx * (ny + 2) - 1)
        ReDim fout_Backward(nx * ny - 1)

        hin_Backward = GCHandle.Alloc(fin_Backward, GCHandleType.Pinned)
        hout_Backward = GCHandle.Alloc(fout_Backward, GCHandleType.Pinned)
        fplan_backward = dft_c2r_2d(nx, ny, hin_Backward.AddrOfPinnedObject(), hout_Backward.AddrOfPinnedObject(), 0)

        MakeGaussianReal(width, MTF, grade)
        done = False
    End Sub
    Public Sub DFT2D_MTF(fin2D(,) As Single, ByRef Fout2D(,) As Single)
        done = False
        ReDim Fout2D(nx - 1, ny - 1)
        ' 4 is there to account for single data type. Buffer copy is for byte arrays.
        Buffer.BlockCopy(fin2D, 0, fin, 0, nx * ny * 4)
        'pointers to the FFTW plan objects
        execute(fplan_Forward)

        Dim nmax As Integer = nx * (ny + 2) - 1
        Dim fc, sc As Complex
        For n = 0 To nmax Step 2
            fc = New Complex(fout(n), fout(n + 1))
            sc = New Complex(S(n), S(n + 1))

            fin_Backward(n) = Complex.Multiply(fc, sc).Real
            fin_Backward(n + 1) = Complex.Multiply(fc, sc).Imaginary
            'fin_Backward(n) = fout(n) * S(n)
        Next

        execute(fplan_backward)
        Buffer.BlockCopy(fout_Backward, 0, Fout2D, 0, nx * ny * 4)
        done = True
    End Sub

    Public Sub Process_FT_MTF()
        done = False
        execute(fplan_Forward)
        For n = 0 To nmax - 1
            fin_Backward(n) = fout(n) * MTF(n)
        Next
        execute(fplan_backward)
        done = True
    End Sub



    Public Sub UpLoad(Fexin() As Single)
        Buffer.BlockCopy(Fexin, 0, fin, 0, nx * ny * 4)

    End Sub
    Public Sub DownLoad_Normalized(ByRef foutX() As Single)
        ReDim foutX(fout_Backward_UpperBound)
        For i = 0 To fout_Backward_UpperBound
            foutX(i) = fout_Backward(i) / fin(i)
        Next
    End Sub
    Public Sub DownLoad(ByRef foutX() As Single)
        Buffer.BlockCopy(fout_Backward, 0, foutX, 0, nx * ny * 4)
    End Sub
    Public Sub MakeGaussianReal(qc As Single, ByRef G() As Single, d As Integer)

        qc = qc * nx

        Dim LG(nx - 1, ny - 1) As Single
        For x = 0 To nx - 1
            For y = 0 To ny - 1
                LG(x, y) = Math.Exp((-(x - nx / 2 - 0.5) ^ d - (y - ny / 2 - 0.5) ^ d) / (2 * qc ^ d))

            Next
        Next

        'saveSinglePage32("C:\temp\G.tif", LG)
        LG = fftshift(LG)
        'saveSinglePage32("C:\temp\G_shifted.tif", LG)

        ReDim G(nx * (ny + 2) - 1)

        Dim p As Integer = 0

        For i = 0 To nx - 1
            For j = 0 To ny / 2

                '(W * H) ^ 2 This is to take  into account the required normalization for FFT directions

                G(p) = (LG(i, j)) / (nx * ny)
                G(p + 1) = (LG(i, j)) / (nx * ny)

                p = p + 2
            Next

        Next

    End Sub


    Public Sub MakeGaussianwithFFT(qc As Single, ByRef G() As Single, d As Integer)
        Dim n As Integer
        qc = qc * nx

        Dim LG(nx * ny - 1) As Single
        For x = 0 To nx - 1
            For y = 0 To ny - 1
                LG(n) = Math.Exp((-(x - nx / 2 - 0.5) ^ d - (y - ny / 2 - 0.5) ^ d) / (2 * qc ^ d))
                n += 1
            Next
        Next
        n = 0
        Dim sum As Single = LG.Sum
        For x = 0 To nx - 1
            For y = 0 To ny - 1
                fin(n) = LG(n) / sum
                n += 1
            Next
        Next

        'saveSinglePage32("C:\temp\Gause.tif", LG, nx, ny)

        execute(fplan_Forward)
        Dim nmax As Integer = nx * (ny + 2) - 1
        ReDim G(nmax)
        For n = 0 To nmax - 1
            G(n) = fout(n) / (nx * ny)
        Next

        'Dim fout2D(nx - 1, ny - 1) As Single
        'Dim p As Integer = 0
        'For x = 0 To nx - 1
        '    For y = 0 To ny / 2

        '        ' foutC(x, y) = New Complex(fout(p), fout(p + 1)) / (nx * ny)
        '        fout2D(x, y) = Math.Log(fout(p) ^ 2 + fout(p + 1) ^ 2)
        '        p += 2
        '    Next
        'Next

        'saveSinglePage32("c:\temp\fourier.tif", fout2D)

    End Sub
    Public Sub MakeSobelX()
        Dim Sobel(,) As Single = {{1, 2, 1}, {0, 0, 0}, {-1, -2, -1}}
        Dim LG(nx - 1, ny - 1) As Single

        For y = 0 To 2
            For x = 0 To 2

                LG(x + nx / 2 - 1, y + ny / 2 - 1) = Sobel(y, x)
            Next
        Next

        Dim n As Integer = 0
        For y = 0 To ny - 1
            For x = 0 To nx - 1
                fin(n) = LG(x, y)
                n += 1
            Next
        Next

        execute(fplan_Forward)
        Dim nmax As Integer = nx * (ny + 2) - 1
        ReDim MTF(nmax)
        For n = 0 To nmax - 1
            MTF(n) = fout(n) / (nx * ny)
        Next
    End Sub

    Public Sub MakeSobelY()
        Dim Sobel(,) As Single = {{1, 0, -1}, {2, 0, -2}, {1, 0, -1}}
        Dim LG(nx - 1, ny - 1) As Single

        For y = 0 To 2
            For x = 0 To 2

                LG(x + nx / 2 - 1, y + ny / 2 - 1) = Sobel(y, x)
            Next
        Next

        Dim n As Integer = 0
        For y = 0 To ny - 1
            For x = 0 To nx - 1
                fin(n) = LG(x, y)
                n += 1
            Next
        Next

        execute(fplan_Forward)
        Dim nmax As Integer = nx * (ny + 2) - 1
        ReDim MTF(nmax)
        For n = 0 To nmax - 1
            MTF(n) = fout(n) / (nx * ny)
        Next
    End Sub
    Private Function fftshift(img(,) As Single) As Single(,)
        Dim DimX As Integer = img.GetLength(0)
        Dim DimY As Integer = img.GetLength(1)

        Dim tmp13 As Single, tmp24 As Single
        Dim DimX2, DimY2 As Integer
        DimX2 = DimX / 2 - 1
        ' half of row dimension
        DimY2 = DimY / 2 - 1
        ' half of column dimension
        ' interchange entries in 4 quadrants, 1 <--> 3 and 2 <--> 4

        For x = 0 To DimX2
            For y = 0 To DimY2
                tmp13 = img(x, y)
                img(x, y) = img(x + DimX2 + 1, y + DimY2 + 1)
                img(x + DimX2 + 1, y + DimY2 + 1) = tmp13

                tmp24 = img(x + DimX2 + 1, y)
                img(x + DimX2 + 1, y) = img(x, y + DimY2 + 1)
                img(x, y + DimY2 + 1) = tmp24
            Next
        Next
        Return img
    End Function
End Class
