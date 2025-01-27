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
    Public Exposure As Single
    Public R As Rectangle
    Dim videoDevices As New FilterInfoCollection(FilterCategory.VideoInputDevice)
    ' create video source
    Dim videoSource As New VideoCaptureDevice(videoDevices(0).MonikerString)

    Public Sub New()


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
        R = New Rectangle(X, Y, W, H)

        '   videoSource.SetCameraProperty(CameraControlProperty.Exposure, Form1.Textbox_exposure.Text, CameraControlFlags.Manual)
        '   videoSource.SetCameraProperty(CameraControlProperty.Exposure, 10, CameraControlFlags.Manual)
        V.Start()
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




        bmpf = New Bitmap(V.GetCurrentVideoFrame)
        Bmp = New Bitmap(Bmpf.width, Bmpf.height, System.Drawing.Imaging.PixelFormat.Format24bppRgb)
        Bmp = bmpf.Clone(New Rectangle(0, 0, bmpf.Width, bmpf.Height), System.Drawing.Imaging.PixelFormat.Format24bppRgb)


        Width = Bmp.Width
        Height = Bmp.Height
        'BmpTissue = New FastBMP(CInt(Width * 2 / 3), Height, Imaging.PixelFormat.Format32bppArgb)

        'Dim Cropfilter As New Crop(New Rectangle(0, Height / 6, Width, Height * (2 / 3)))
        'BmpTissue = New FastBMP(Cropfilter.Apply(Bmp))

        'X = (1.4 / 5.5) * BmpTissue.width
        'W = (2.5 / 5.5) * BmpTissue.width

        'BmpTissue.GR.DrawRectangle(New Pen(Color.DarkBlue), New Rectangle(X, 0, W, W))

        'Bmp.Save("D:\bmp.png")

        'Dim Cropfilter As New Crop(New Rectangle(X, Y, W, H))

        Dim X As Integer = Setting.Gett("Preview_X")
        Dim Y As Integer = Setting.Gett("Preview_Y")
        Dim W As Integer = Setting.Gett("Preview_W")
        Dim H As Integer = Setting.Gett("Preview_H")
        R = New Rectangle(X, Y, W, H)

        Dim CropFilter As New Crop(R)
        Bmp = CropFilter.Apply(Bmp)
        'Dim crop As New Rectangle(X, Y, W, H)
        'Dim croppedImage = New Bitmap(crop.Width, crop.Height, Bmp.PixelFormat)
        'Using grp = Graphics.FromImage(croppedImage)
        '    grp.DrawImage(Bmp, New Rectangle(crop.X, crop.Y, crop.Width, crop.Height), crop, GraphicsUnit.Pixel)
        '    Bmp.Dispose()
        '    Bmp = croppedImage
        'End Using
        'Bmp.Save("C:\test\bmptissue_cropped.png")
        'Dim FlipFilter As New Mirror(True, True)
        'Bmp = Cropfilter.Apply(Bmp)
        'Dim bmpCropped As Bitmap = New Bitmap(Cropfilter.Apply(Bmp))
        'Bmp = FlipFilter.Apply(Bmp)
        'Bmp.Save("C:\temp\" & Now.ToString("R").Replace(":", " ") & ".jpg")



        Return Bmp

    End Function
    Public Function CaptureWhole(exposure As Integer, focus As Integer) As Bitmap
        Dim flags As CameraControlFlags
        Dim minVal, maxVal, stepSize, defaultVal, Pfocus As Integer



        videoSource.SetCameraProperty(CameraControlProperty.Exposure, exposure, flags)
        videoSource.GetCameraPropertyRange(CameraControlProperty.Focus, minVal, maxVal, stepSize, defaultVal, flags)
        videoSource.GetCameraProperty(CameraControlProperty.Focus, Pfocus, flags)
        videoSource.SetCameraProperty(CameraControlProperty.Focus, focus, CameraControlFlags.Manual)

        V.Start()
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
        Dim Phasor As New QuickPhasor(400, Width, Height)
        Phasor.MakeHistogram(Bmpf, True)
        Phasor.CreateMask(0, 175, 175)
        Dim segmented As New FastBMP(Bmp.Width, Bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb)
        Phasor.Segment(Bmpf, segmented)
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
        Phasor.Plot.bmp.Save("C:\test\PhasorPlot.png")
        Bmp.Save("C:\test\bmptissue.png")

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



End Class
