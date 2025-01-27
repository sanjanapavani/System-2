Imports System.Drawing.Imaging
Imports System.IO
Imports AForge.Imaging.Filters
Public Class test
    Public Width, Height As Integer
    Public Bmp, BmpVimba, bmpf As Bitmap
    Public ZmapBmp As FastBMP
    Public Exposure As Single
    Public R As Rectangle

    Dim Deivative As CentralDerivitavie
    Dim BLure As FFTW_VB_Real

    Dim Preview_Z As Single
    Public Z As Integer
    Dim Zsteps As Single
    Dim Pbar As ProgressBar
    Dim MaxMap() As Single
    Public Zx() As Single
    Public Zmap(,) As Single
    Dim C(), S() As Single
    Dim Flip As Mirror
    Dim Cropfilter As Crop
    Public X0, Y0, W, H As Integer
    Public GreyEdge()() As Single
    Public GreyEdge2D()(,) As Single

    ' create video source
    Public Sub New(Z As Integer, Pbar As ProgressBar)

        X0 = Setting.Gett("Preview_X")
        Y0 = Setting.Gett("Preview_Y")
        W = Setting.Gett("Preview_W")
        H = Setting.Gett("Preview_H")
        R = New Rectangle(X0, Y0, W, H)



        Width = 1600
        Height = 1234

        W = Width
        H = Height


        Flip = New Mirror(True, True)
        Cropfilter = New Crop(R)

        Deivative = New CentralDerivitavie(W, H)
        BLure = New FFTW_VB_Real(H, W)
        BLure.MakeGaussianReal(0.01, BLure.MTF, 2)
        Preview_Z = Setting.Gett("Preview_Z")
        Me.Z = Z
        Me.Zsteps = 0.1
        Me.Pbar = Pbar
        Pbar.Maximum = Z
        ReDim GreyEdge(Z - 1)
        ReDim GreyEdge2D(Z - 1)
        ReDim C(Z - 1)
        ReDim S(Z - 1)
        ReDim Zx(Z - 1)
        For zz = 0 To Z - 1
            ReDim GreyEdge(zz)(W * H - 1)
            ReDim GreyEdge2D(zz)(H - 1, W - 1)
            C(zz) = Math.Cos(2 * Math.PI / Z * zz)
            S(zz) = Math.Sin(2 * Math.PI / Z * zz)
        Next
        load_colormap()
    End Sub
    Public Sub EstimateProfile()
        Dim i As Integer
        Dim BmpVimbaFast As New FastBMP(Width, Height, PixelFormat.Format24bppRgb)
        Dim Bmpgrey As New FastBMP(Width, Height, PixelFormat.Format8bppIndexed)
        Dim BmpEdge As New Bitmap(Width, Height, PixelFormat.Format8bppIndexed)
        Dim BmpEdgeColor As New Bitmap(Width, Height, PixelFormat.Format24bppRgb)
        Dim Edge As New HomogenityEdgeDetector
        Dim GR As Graphics
        GR = Graphics.FromImage(BmpEdgeColor)
        Pbar.Value = 0
        Pbar.Maximum = Z
        Dim Dir As String = Path.GetDirectoryName("C:\temp\Images\")
        Dim JpgFiles = New DirectoryInfo(Dir).GetFiles("*.bmp")
        Dim Edgebytes(W * H * 3 - 1) As Byte
        Dim Offset As Integer
        Dim Intensity As Single
        For zz = 0 To Z - 1
            'For zz = 30 - 20 To 30 + 20 - 1
            Zx(zz) = zz * Zsteps + Preview_Z
            'If zz > 0 Then Stage.MoveRelative(Stage.Zaxe, Zsteps, False)

            BmpVimbaFast = New FastBMP(New Bitmap(JpgFiles(zz).FullName))
            Bmpgrey.MakeFromBytes(BmpVimbaFast.GetGraysacleArray())

            BmpEdge = Edge.Apply(Bmpgrey.bmp)
            GR.DrawImageUnscaled(BmpEdge, 0, 0)
            'BmpEdgeColor.Save("c:\temp\" + zz.ToString("D4") + ".bmp")
            Offset = BitmapToBytes(BmpEdgeColor, Edgebytes) - W * 3

            Dim k As Integer = 0
            Dim j As Integer = 1

            For yy = 0 To H - 1
                For xx = 0 To W - 1

                    GreyEdge(zz)(k) = Edgebytes(j)
                    GreyEdge(zz)(k) = GreyEdge(zz)(k) / (Bmpgrey.bytes(k) + 1)

                    k += 1
                    j += 3
                Next
                j += Offset
            Next


            '    Buffer.BlockCopy(GreyEdge(zz), 0, GreyEdge2D(zz), 0, W * H * 4)
            'saveSinglePage32("c:\temp\" + zz.ToString("D4") + "_not B.tif", GreyEdge2D(zz))
            'Deivative.AnalyzeX(BmpVimbaFast.Greyimage, GreyEdge(zz))
            'BLure.UpLoad(GreyEdge(zz))
            'BLure.Process_FT_MTF()
            'BLure.DownLoad(GreyEdge(zz))


            Buffer.BlockCopy(GreyEdge(zz), 0, GreyEdge2D(zz), 0, W * H * 4)
            Buffer.BlockCopy(Bin2DArray(2, GreyEdge2D(zz)), 0, GreyEdge(zz), 0, W * H * 4)
            'BmpVimbaFast.bmp.Save("c:\temp\images\" + zz.ToString("D4") + ".bmp")
            ' saveSinglePage32("c:\temp\" + zz.ToString("D4") + ".tif", GreyEdge(zz), Width, Height)
            'saveSinglePage32("c:\temp\" + zz.ToString("D4") + ".tif", GreyEdge2D(zz))
            Pbar.Increment(1)
            Application.DoEvents()


        Next
        ' BmpVimbaFast = New FastBMP(New Bitmap("c:\temp\0000.bmp"))


        Dim max, maxZ As Single
        ReDim MaxMap(W * H - 1)
        Dim MaxmapPhasor(W * H - 1) As Single
        Dim maxi As Integer = W * H - 1
        Dim Cs, Ss As Single
        Dim Cm As Single
        For i = 0 To maxi
            max = 0
            Cs = 0
            Ss = 0
            Intensity = 0
            Cm = 0
            For Zi = 0 To Z - 1
                If GreyEdge(Zi)(i) > max Then max = GreyEdge(Zi)(i) : maxZ = Zi : MaxMap(i) = maxZ
                Cs += C(Zi) * GreyEdge(Zi)(i)
                Ss += S(Zi) * GreyEdge(Zi)(i)
                Cm += GreyEdge(Zi)(i) * Zi
                Intensity += GreyEdge(Zi)(i)
            Next

            MaxmapPhasor(i) = ((Math.Atan2(Ss, Cs))) / (Math.PI * 2) * Z * Zsteps
            If Ss < 0 Then
                MaxmapPhasor(i) = (Math.PI * 2 + (Math.Atan2(Ss, Cs))) / (Math.PI * 2) * Z * Zsteps
            End If
            If Intensity = 0 Then Intensity = 1
            MaxmapPhasor(i) = Cm / Intensity

        Next
        Dim MAxmap2D(H - 1, W - 1) As Single

        ReDim Zmap(W - 1, H - 1)
        Buffer.BlockCopy(MaxmapPhasor, 0, MAxmap2D, 0, W * H * 4)


        For yy = 0 To H - 1
            For xx = 0 To W - 1
                Zmap(xx, yy) = MAxmap2D(yy, xx)
            Next
        Next

        'saveSinglePage32("c:\temp\maxmap2d.tif", MAxmap2D)

        'ZmapBmp = New FastBMP(W, H, Imaging.PixelFormat.Format24bppRgb)

        'Zsteps = 0.1
        'Dim p As Integer = 0
        'Dim r, g, b As Byte
        'For yy = 0 To H - 1
        '    For xx = 0 To W - 1
        '        r = rr((Zmap(xx, yy) - Preview_Z) / (Z * Zsteps) * 1000)
        '        g = gg((Zmap(xx, yy) - Preview_Z) / (Z * Zsteps) * 1000)
        '        b = bb((Zmap(xx, yy) - Preview_Z) / (Z * Zsteps) * 1000)
        '        ZmapBmp.FillOriginalPixel(xx, yy, r, g, b)

        '    Next

        'Next
        'ZmapBmp.MakeNewFromBytes()


        'saveSinglePage32("c:\temp\MaxMap.tif", MaxMap, W, H)
        SaveSinglePage32("c:\temp\PhasorMAp.tif", MaxmapPhasor, W, H)
        Pbar.Value = 0

    End Sub


    Public Function Bin2DArray(bin As Integer, Arrayin(,) As Single) As Single(,)
        Dim DimX As Integer = Arrayin.GetUpperBound(0)
        Dim DimY As Integer = Arrayin.GetUpperBound(1)


        Dim ArrayT(DimX + bin * 2, DimY + bin * 2) As Single
        Dim ArrayOut(DimX, DimY) As Single



        For y = 0 To DimY
            For x = 0 To DimX

                ArrayT(x + bin, y + bin) = Arrayin(x, y)

            Next
        Next

        For y = 0 To DimY
            For x = 0 To DimX

                For yy = y - bin To y + bin
                    For xx = x - bin To x + bin


                        ArrayOut(x, y) += ArrayT(xx + bin, yy + bin) / (bin * 2 + 1) ^ 2

                    Next
                Next

            Next
        Next
        Return ArrayOut
    End Function



End Class
