
Imports AForge.Imaging.Filters
Imports AForge.Video.DirectShow
Imports AForge.Imaging
Imports AForge.Math.Geometry
Imports AVT.VmbAPINET
Imports System.Drawing.Imaging


Public Class PreviewVimba

    Public Width, Height As Integer
    Public Bmp, BmpVimba, bmpf As Bitmap
    Public ZmapBmp As FastBMP
    Public Exposure As Single
    Public R As Rectangle
    Dim mySys As Vimba
    Dim mCameras As CameraCollection
    Dim mCamera As Camera
    Dim frame As Frame
    Dim features As FeatureCollection
    Dim Deivative As CentralDerivitavie
    Dim BLure As FFTW_VB_Real
    Dim expFeature As Feature
    Public PreviewZ0, PreviewX0, PreviewY0 As Single
    Public Z As Integer
    Dim Zsteps As Single
    Dim Pbar As ProgressBar
    Dim MaxMap() As Single
    Public Zx() As Single
    Public Zmap(,) As Single
    Dim C(), S() As Single
    Dim Flip As Mirror
    Dim Cropfilter As Crop
    Dim Canvas As Integer
    Dim LoadX, LoadY, LoadZ As Single

    Public X0, Y0, ROI_W, ROI_H As Integer
    Public GreyEdge()() As Single
    Public GreyEdge2D()(,) As Single
    Public Scale As Single

    ' create video source
    Public Sub New(Z As Integer, Zsteps As Single, Pbar As ProgressBar)
        'Z has to be an odd number to have the symetric acqusition.
        mySys = New Vimba
        mySys.Startup()
        mCameras = mySys.Cameras
        mCamera = mCameras(0)
        mCamera.Open(VmbAccessModeType.VmbAccessModeFull)
        features = mCamera.Features

        X0 = Setting.Gett("Preview_X")
        Y0 = Setting.Gett("Preview_Y")
        ROI_W = Setting.Gett("Preview_W")
        ROI_H = Setting.Gett("Preview_H")
        Canvas = 0

        R = New Rectangle(X0 - Canvas, Y0 - Canvas, ROI_W + Canvas * 2, ROI_H + Canvas * 2)

        mCamera.AcquireSingleImage(frame, 1000)
        frame.Fill(BmpVimba)

        Width = BmpVimba.Width
        Height = BmpVimba.Height

        Flip = New Mirror(True, True)
        Cropfilter = New Crop(R)

        Deivative = New CentralDerivitavie(ROI_W, ROI_H)
        BLure = New FFTW_VB_Real(ROI_H, ROI_W)
        BLure.MakeGaussianReal(0.008, BLure.MTF, 2)
        PreviewZ0 = Setting.Gett("Previewz0")
        PreviewX0 = Setting.Gett("Previewx0")
        PreviewY0 = Setting.Gett("Previewy0")
        Me.Z = Z
        Me.Zsteps = Zsteps
        Me.Pbar = Pbar
        Pbar.Maximum = Z
        ReDim GreyEdge(Z - 1)
        ReDim GreyEdge2D(Z - 1)
        ReDim C(Z - 1)
        ReDim S(Z - 1)
        ReDim Zx(Z - 1)

        LoadX = Setting.Gett("loadx")
        LoadY = Setting.Gett("loady")
        LoadZ = Setting.Gett("loadz")

        For zz = 0 To Z - 1
            ReDim GreyEdge(zz)(ROI_W * ROI_H - 1)
            ReDim GreyEdge2D(zz)(ROI_H - 1, ROI_W - 1)
            C(zz) = Math.Cos(2 * 2 * Math.PI / Z * zz)
            S(zz) = Math.Sin(2 * 2 * Math.PI / Z * zz)
        Next
        load_colormap()
    End Sub

    Public Function Capture(exposure As Integer, focus As Integer) As Bitmap

        mCamera.AcquireSingleImage(frame, 1000)
        frame.Fill(BmpVimba)

        Width = BmpVimba.Width
        Height = BmpVimba.Height

        Bmp = Cropfilter.Apply(BmpVimba)
        Flip.ApplyInPlace(Bmp)

        'Dim fbmp As New FastBMP(Bmp)
        'Dim R, G, B As Single
        'For y = 0 To fbmp.height - 1
        '    For x = 0 To fbmp.width - 1
        '        fbmp.GetPixel(x, y, R, G, B)
        '        B = B * 3

        '        If B > 255 Then B = 255

        '        fbmp.FillOriginalPixel(x, y, R, G, B)
        '    Next
        'Next
        'fbmp.Reset()


        Return Bmp

    End Function


    Public Function Capture() As Bitmap

        mCamera.AcquireSingleImage(frame, 500)
        frame.Fill(BmpVimba)

        Width = BmpVimba.Width
        Height = BmpVimba.Height

        Bmp = Cropfilter.Apply(BmpVimba)

        Flip.ApplyInPlace(Bmp)

        Return Bmp

    End Function




    Public Function CaptureWhole(exposure As Integer, focus As Integer) As Bitmap

        mCamera.AcquireSingleImage(frame, 500)
        frame.Fill(BmpVimba)
        Bmp = BmpVimba.Clone()
        Return Bmp
    End Function
    Public Function CaptureROI(exposure As Integer, focus As Integer) As Bitmap

        'mCamera.AcquireSingleImage(frame, 500)
        'frame.Fill(BmpVimba)


        'Dim Bmpf = New FastBMP(BmpVimba)
        'Bmp = New Bitmap(Bmpf.width, Bmpf.height, System.Drawing.Imaging.PixelFormat.Format24bppRgb)
        'Bmp = Bmpf.bmp.Clone(New Rectangle(0, 0, Bmpf.width, Bmpf.height), System.Drawing.Imaging.PixelFormat.Format24bppRgb)

        'Width = Bmp.Width
        'Height = Bmp.Height
        ''trying the quickPhasor
        'Dim Phasor As New QuickPhasor(400, Width, Height)
        'Phasor.MakeHistogram(Bmpf, True)
        'Phasor.CreateMask(0, 175, 175)
        'Dim segmented As New FastBMP(Bmp.Width, Bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb)
        'Phasor.Segment(Bmpf, segmented)
        '' now detecting the rectangle
        'Dim blobCounter As New AForge.Imaging.BlobCounter

        'blobCounter.FilterBlobs = True
        'blobCounter.MinHeight = 300
        'blobCounter.MinWidth = 500

        'blobCounter.ProcessImage(segmented.bmp)

        'Dim blobs As Blob() = blobCounter.GetObjectsInformation()

        'Dim g As Graphics = Graphics.FromImage(segmented.bmp)
        'g.DrawRectangle(New Pen(Brushes.Red, 0.5), blobs(0).Rectangle)
        'R = blobs(0).Rectangle

        'Dim CropFilter As New Crop(R)
        'Bmp = CropFilter.Apply(segmented.bmp)


        'Setting.Sett("Preview_X", R.X)
        'Setting.Sett("Preview_Y", R.Y)
        'Setting.Sett("Preview_W", R.Width)
        'Setting.Sett("Preview_H", R.Height)


        'segmented.bmp.Save("C:\test\segmented.jpg")
        'Phasor.Plot.bmp.Save("C:\test\PhasorPlot.png")
        'Bmp.Save("C:\test\bmptissue.png")

        'Return Bmp
        ''Form1.PictureBox_Phasor.Image = Phasor.Plot.bmp

    End Function
    Public Sub StopPreview()
        mCamera.Close()
        mySys.Shutdown()

    End Sub

    Public Sub SetExposure(exp As Single)
        expFeature = features("ExposureTime")
        expFeature.FloatValue = exp * 1000

    End Sub
    Public Sub MovetoLoad()


        Stage.SetSpeed(Stage.Yaxe, 20)

        Stage.MoveAbsolute(Stage.Zaxe, LoadZ)
        Stage.MoveAbsolute(Stage.Yaxe, LoadY)
        Stage.MoveAbsolute(Stage.Xaxe, LoadX)

        Stage.SetSpeed(Stage.Yaxe, 80)

    End Sub
    Public Sub MovetoPreview()

        Stage.SetSpeed(Stage.Yaxe, 20)

        Stage.MoveAbsolute(Stage.Zaxe, PreviewZ0)
        Stage.MoveAbsolute(Stage.Yaxe, PreviewY0)
        Stage.MoveAbsolute(Stage.Xaxe, PreviewX0)

        Stage.SetSpeed(Stage.Yaxe, 80)
    End Sub
    Public Function GetProfile(X As Integer, Y As Integer, CursorWidth As Integer, CursorHeight As Integer) As Single
        X = X * Scale
        Y = Y * Scale
        CursorWidth = CursorWidth * Scale
        CursorHeight = CursorHeight * Scale
        Dim Profile(CursorWidth * CursorHeight - 1) As Single
        Dim Focus As Single = Setting.Gett("Focus")

        'MaxmapPhasor(i) = 762.45 * X * X - 2892.5 * X + 2747.6
        'MaxmapPhasor(i) = MaxmapPhasor(i) + (Focus - Preview_Z)
        Dim j As Integer = 0
        For yy = 0 To CursorHeight - 1
            For xx = 0 To CursorWidth - 1
                Profile(j) = Zmap(X + xx, Y + yy)
                j += 1
            Next
        Next

        SaveSinglePage32("c:\temp\Porfile.tif", Profile, CursorWidth, CursorHeight)
        Array.Sort(Profile)
        Dim Zstart As Single
        Dim Z0 As Single = Profile(CursorWidth * CursorHeight / 3)
        Zstart = -9.6247 * Z0 + 42.08
        Zstart = Zstart + (Focus - PreviewZ0) - 1
        Return Zstart
        'Console.WriteLine("Profile: " + Porfile(100).ToString)

    End Function
    Public Sub EstimateProfile(Optional Zofsset As Single = 0)
        Dim i As Integer
        Dim BmpVimbaFast As New FastBMP(ROI_W, ROI_H, PixelFormat.Format24bppRgb)
        Dim Bmpgrey As New FastBMP(ROI_W, ROI_H, PixelFormat.Format8bppIndexed)
        Dim BmpEdge As New Bitmap(ROI_W, ROI_H, PixelFormat.Format8bppIndexed)
        Dim BmpEdgeColor As New Bitmap(ROI_W, ROI_H, PixelFormat.Format24bppRgb)
        Dim Edge As New HomogenityEdgeDetector
        Dim GR As Graphics
        GR = Graphics.FromImage(BmpEdgeColor)

        Dim Edgebytes(ROI_W * ROI_H * 3 - 1) As Byte

        Dim Offset As Integer = 0
        Pbar.Value = 0
        Stage.MoveAbsolute(Stage.Zaxe, PreviewZ0 - (Z - 1) * Zsteps / 2 + Zofsset, False)

        For zz = 0 To Z - 1
            Zx(zz) = zz * Zsteps + PreviewZ0

            If zz > 0 Then Stage.MoveRelative(Stage.Zaxe, Zsteps, False)


            BmpVimbaFast = New FastBMP(Capture)
            BmpVimbaFast.bmp.Save("c:\temp\EDOF\" + zz.ToString("D4") + ".bmp")

            'Bmpgrey.MakeFromBytes(BmpVimbaFast.GetGraysacleArray())

            'BmpEdge = Edge.Apply(Bmpgrey.bmp)
            'GR.DrawImageUnscaled(BmpEdge, 0, 0)
            'BmpEdgeColor.Save("c:\temp\EDOF\Edged\" + zz.ToString("D4") + ".bmp")
            'Offset = BitmapToBytes(BmpEdgeColor, Edgebytes) - ROI_W * 3

            'Dim k As Integer = 0
            'Dim j As Integer = 1

            'For yy = 0 To ROI_H - 1
            '    For xx = 0 To ROI_W - 1

            '        GreyEdge(zz)(k) = Edgebytes(j)
            '        GreyEdge(zz)(k) = GreyEdge(zz)(k) / (Bmpgrey.bytes(k) + 1)

            '        k += 1
            '        j += 3
            '    Next
            '    j += Offset
            'Next


            'BmpVimbaFast.GetGraysacleArray()

            'Deivative.AnalyzeX(BmpVimbaFast.Greyimage, GreyEdge(zz))
            'BLure.UpLoad(GreyEdge(zz))
            'BLure.Process_FT_MTF()
            'BLure.DownLoad(GreyEdge(zz))
            'Buffer.BlockCopy(GreyEdge(zz), 0, GreyEdge2D(zz), 0, W * H * 4)
            'BmpVimbaFast.bmp.Save("c:\temp\images\" + zz.ToString("D4") + ".bmp", ImageFormat.Bmp)
            'saveSinglePage32("c:\temp\" + zz.ToString("D4") + ".tif", GreyEdge(zz), ROI_W, ROI_H)
            '  saveSinglePage32("c:\temp\" + zz.ToString("D4") + ".tif", GreyEdge2D(zz))
            Pbar.Increment(1)
            Application.DoEvents()
        Next

        Pbar.Value = 0



        Dim max, maxZ As Single
        ReDim MaxMap(ROI_W * ROI_H - 1)
        Dim MaxmapPhasor(ROI_W * ROI_H - 1) As Single
        Dim MaxmapPhasorRaw(ROI_W * ROI_H - 1) As Single
        Dim maxi As Integer = ROI_W * ROI_H - 1
        Dim Cs, Ss As Single
        Dim Cm As Single
        Dim Intensity As Single = 0

        Dim Focus As Single = Setting.Gett("Focus")

        For i = 0 To maxi
            max = 0
            Cs = 0
            Ss = 0
            Cm = 0
            Intensity = 0
            For Zi = 0 To Z - 1
                If GreyEdge(Zi)(i) > max Then max = GreyEdge(Zi)(i) : maxZ = Zi : MaxMap(i) = maxZ
                Cs += C(Zi) * GreyEdge(Zi)(i)
                Ss += S(Zi) * GreyEdge(Zi)(i)
                Cm += GreyEdge(Zi)(i) * Zi * Zsteps
                Intensity += GreyEdge(Zi)(i)
            Next
            'MaxmapPhasor(i) = ((Math.Atan2(Ss, Cs))) / (Math.PI * 2) * Z * Zsteps
            'If Ss < 0 Then
            '    MaxmapPhasor(i) = (Math.PI * 2 + (Math.Atan2(Ss, Cs))) / (Math.PI * 2) * Z * Zsteps
            'End If
            If Intensity = 0 Then Intensity = 1


            MaxmapPhasor(i) = Cm / Intensity


        Next
        Dim MAxmap2D(ROI_H - 1, ROI_W - 1) As Single

        ReDim Zmap(ROI_W - 1, ROI_H - 1)
        Buffer.BlockCopy(MaxmapPhasor, 0, MAxmap2D, 0, ROI_W * ROI_H * 4)


        For yy = 0 To ROI_H - 1
            For xx = 0 To ROI_W - 1
                Zmap(xx, yy) = MAxmap2D(yy, xx)
            Next
        Next

        'saveSinglePage32("c:\temp\maxmap2d.tif", MAxmap2D)

        'ZmapBmp = New FastBMP(ROI_W, ROI_H, Imaging.PixelFormat.Format24bppRgb)


        'Dim p As Integer = 0
        'Dim r, g, b As Byte
        'For yy = 0 To ROI_H - 1
        '    For xx = 0 To ROI_W - 1
        '        r = rr((Zmap(xx, yy) - Preview_Z) / (Z * Zsteps) * 1000)
        '        g = gg((Zmap(xx, yy) - Preview_Z) / (Z * Zsteps) * 1000)
        '        b = bb((Zmap(xx, yy) - Preview_Z) / (Z * Zsteps) * 1000)
        '        ZmapBmp.FillOriginalPixel(xx, yy, r, g, b)

        '    Next

        'Next
        'ZmapBmp.MakeNewFromBytes()


        'saveSinglePage32("c:\temp\Zmap" + Zofsset.ToString + ".tif", Zmap)
        'saveSinglePage32("c:\temp\MaxMapPhasor" + Preview_Z.ToString + ".tif", MaxmapPhasor, ROI_W, ROI_H)
        'saveSinglePage32("c:\temp\MaxMapPhasorRaw" + Preview_Z.ToString + ".tif", MaxmapPhasorRaw, ROI_W, ROI_H)
        Pbar.Value = 0
    End Sub

End Class
