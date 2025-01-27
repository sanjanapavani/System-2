Imports AForge.Math
Imports AForge.Imaging.Filters
Imports AForge.Video.DirectShow
Imports AForge.Imaging
Imports AForge.Math.Geometry
Imports AForge

Public Class PreviewWebcam
    Dim V As AForge.Controls.VideoSourcePlayer
    Public Width, Height As Integer
    Public Bmp, bmpf, BmpLabel As Bitmap
    Public ZmapBmp As FastBMP
    Public Exposure As Single
    Public Preview_Z As Single
    Public Z As Integer
    Public R As Rectangle
    Public Scale As Single
    Public GreyEdge()() As Single
    Public GreyEdge2D()(,) As Single
    Public Zx() As Single
    Public Zmap(,) As Single
    Public X0, Y0, ROI_W, ROI_H As Integer
    Dim Cropfilter As Crop
    Dim Flip As Mirror
    Dim videoDevices As New FilterInfoCollection(FilterCategory.VideoInputDevice)
    ' create video source
    Dim videoSource As New VideoCaptureDevice(videoDevices(0).MonikerString)

    Public Sub New(Z As Integer, Zsteps As Single, Pbar As ProgressBar)


        videoSource.VideoResolution = videoSource.VideoCapabilities(1)
        videoSource.SnapshotResolution = videoSource.VideoCapabilities(1)
        Dim minVal, maxVal, stepSize, defaultVal As Integer
        Dim flags As CameraControlFlags
        videoSource.GetCameraPropertyRange(CameraControlProperty.Exposure, minVal, maxVal, stepSize, defaultVal, flags)
        videoSource.Start()
        'videoSource.VideoResolution.FrameSize = videoSource.VideoCapabilities(10).FrameSize

        V = New AForge.Controls.VideoSourcePlayer With {
            .VideoSource = videoSource
        }

        Dim X As Integer = Setting.Gett("Preview_X")
        Dim Y As Integer = Setting.Gett("Preview_Y")
        Dim W As Integer = Setting.Gett("Preview_W")
        Dim H As Integer = Setting.Gett("Preview_H")
        'Preview_Z = Setting.Gett("Preview_Z")

        R = New Rectangle(X, Y, W, H)
        Flip = New Mirror(False, False)
        Cropfilter = New Crop(R)
        '   videoSource.SetCameraProperty(CameraControlProperty.Exposure, Form1.Textbox_exposure.Text, CameraControlFlags.Manual)
        '   videoSource.SetCameraProperty(CameraControlProperty.Exposure, 10, CameraControlFlags.Manual)
        V.Start()
    End Sub

    Public Sub SetExposure(exp As Single)
        Dim flags As CameraControlFlags
        Dim minVal, maxVal, stepSize, defaultVal, Gfocus As Integer
        videoSource.SetCameraProperty(CameraControlProperty.Exposure, Exposure, flags)
        videoSource.GetCameraPropertyRange(CameraControlProperty.Focus, minVal, maxVal, stepSize, defaultVal, flags)
    End Sub
    Public Sub MovetoLoad()




        Stage.MoveAbsolute(Stage.Yaxe, 50.8)
        Stage.MoveAbsolute(Stage.Xaxe, 50)
        ' Stage.MoveAbsolute(Stage.Zaxe, 18)
        Stage.MoveAbsolute(Stage.Zaxe, Stage.Zfocous)



    End Sub
    Public Sub MovetoPreview()
        'Stage.SetSpeed(Stage.Yaxe, 20)
        Stage.MoveAbsolute(Stage.Zaxe, 2)
        Stage.MoveAbsolute(Stage.Xaxe, 25)
        Stage.MoveAbsolute(Stage.Yaxe, 0)

        'Stage.SetSpeed(Stage.Yaxe, 50)
    End Sub
    Public Function Capture(exposure As Integer, focus As Integer) As Bitmap
        Dim flags As CameraControlFlags
        Dim minVal, maxVal, stepSize, defaultVal, Gfocus As Integer

        videoSource.SetCameraProperty(CameraControlProperty.Exposure, exposure, flags)
        videoSource.GetCameraPropertyRange(CameraControlProperty.Focus, minVal, maxVal, stepSize, defaultVal, flags)

        If focus > maxVal Then focus = maxVal
        If focus < minVal Then focus = minVal

        videoSource.GetCameraProperty(CameraControlProperty.Focus, Gfocus, flags)
        videoSource.SetCameraProperty(CameraControlProperty.Focus, focus, CameraControlFlags.Manual)



        Threading.Thread.Sleep(500)
        bmpf = New Bitmap(V.GetCurrentVideoFrame)

        'Dim corners As New List(Of IntPoint)
        'corners.Add(New IntPoint(Setting.Gett("PreviewX1"), Setting.Gett("PreviewY1")))
        'corners.Add(New IntPoint(Setting.Gett("PreviewX2"), Setting.Gett("PreviewY2")))
        'corners.Add(New IntPoint(Setting.Gett("PreviewX3"), Setting.Gett("PreviewY3")))
        'corners.Add(New IntPoint(Setting.Gett("PreviewX4"), Setting.Gett("PreviewY4")))


        'Dim Quad As New QuadrilateralTransformation(corners)
        'Bmp = ConvertTo24bpp(Quad.Apply(bmpf))

        Dim ro As RotateNearestNeighbor = New RotateNearestNeighbor(90)

        Bmp = ConvertTo24bpp(bmpf)
        Bmp = Cropfilter.Apply(Bmp)
        Bmp = ro.Apply(Bmp)



        'Flip.ApplyInPlace(Bmp)
        'Bmp.Save("c:\temp\tt.jpg")
        Return Bmp

    End Function

    Public Shared Function ConvertTo24bpp(ByVal img As Bitmap) As Bitmap
        Dim bmp = New Bitmap(img.Width, img.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb)

        Using gr = Graphics.FromImage(bmp)
            gr.DrawImage(img, New Rectangle(0, 0, img.Width, img.Height))
        End Using

        Return bmp
    End Function
    Public Function CaptureWhole(exposure As Integer, focus As Integer) As Bitmap
        Dim flags As CameraControlFlags
        Dim minVal, maxVal, stepSize, defaultVal, Pfocus As Integer



        videoSource.SetCameraProperty(CameraControlProperty.Exposure, exposure, flags)
        videoSource.GetCameraPropertyRange(CameraControlProperty.Focus, minVal, maxVal, stepSize, defaultVal, flags)
        videoSource.GetCameraProperty(CameraControlProperty.Focus, Pfocus, flags)
        videoSource.SetCameraProperty(CameraControlProperty.Focus, focus, CameraControlFlags.Manual)
        V.Start()
        Threading.Thread.Sleep(500)

        Return V.GetCurrentVideoFrame
    End Function
    Public Function CaptureROI(exposure As Integer, focus As Integer) As Bitmap
        Dim flags As CameraControlFlags
        Dim minVal, maxVal, stepSize, defaultVal, Pfocus As Integer



        videoSource.SetCameraProperty(CameraControlProperty.Exposure, exposure, flags)
        videoSource.GetCameraPropertyRange(CameraControlProperty.Focus, minVal, maxVal, stepSize, defaultVal, flags)
        videoSource.GetCameraProperty(CameraControlProperty.Focus, Pfocus, flags)
        videoSource.SetCameraProperty(CameraControlProperty.Focus, focus, CameraControlFlags.Manual)

        V.Start()


        Dim Bmpf = New FastBMP(V.GetCurrentVideoFrame)
        Bmp = New Bitmap(Bmpf.width, Bmpf.height, System.Drawing.Imaging.PixelFormat.Format24bppRgb)
        Bmp = Bmpf.bmp.Clone(New Rectangle(0, 0, Bmpf.width, Bmpf.height), System.Drawing.Imaging.PixelFormat.Format24bppRgb)

        Width = Bmp.Width
        Height = Bmp.Height
        'trying the quickPhasor
        'Dim Phasor As New QuickPhasor(400, Width, Height)
        'Phasor.MakeHistogram(Bmpf, True)
        'Phasor.CreateMask(0, 175, 175)
        Dim segmented As New FastBMP(Bmp.Width, Bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb)
        'Phasor.Segment(Bmpf, segmented)
        ' now detecting the rectangle
        Dim blobCounter As New AForge.Imaging.BlobCounter

        blobCounter.FilterBlobs = True
        blobCounter.MinHeight = 300
        blobCounter.MinWidth = 500

        blobCounter.ProcessImage(segmented.bmp)

        Dim blobs As Blob() = blobCounter.GetObjectsInformation()

        Dim g As Graphics = Graphics.FromImage(segmented.bmp)
        g.DrawRectangle(New Pen(Brushes.Red, 0.5), blobs(0).Rectangle)
        R = blobs(0).Rectangle

        Dim CropFilter As New Crop(R)
        Bmp = CropFilter.Apply(segmented.bmp)


        Setting.Sett("Preview_X", R.X)
        Setting.Sett("Preview_Y", R.Y)
        Setting.Sett("Preview_W", R.Width)
        Setting.Sett("Preview_H", R.Height)


        segmented.bmp.Save("C:\test\segmented.jpg")
        'Phasor.Plot.bmp.Save("C:\test\PhasorPlot.png")
        'Bmp.Save("C:\test\bmptissue.png")

        Return Bmp
        'Form1.PictureBox_Phasor.Image = Phasor.Plot.bmp

    End Function
    Public Sub StopPreview()
        Try
            videoSource.Stop()
            V.Stop()
        Catch ex As Exception

        End Try



    End Sub

    Public Sub GetExposure()

        'videoSource.GetCameraProperty(CameraControlProperty.Exposure, Exposure, CameraControlFlags.Auto)
    End Sub

    Public Function GetProfile(X As Integer, Y As Integer, CursorWidth As Integer, CursorHeight As Integer) As Single

    End Function
    Public Sub EstimateProfile(Optional Zofsset As Single = 0)

    End Sub
End Class