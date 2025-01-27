
Imports AForge.Imaging.Filters
Imports AForge.Video.DirectShow
Imports AForge.Imaging
Imports AForge.Math.Geometry
Imports AVT.VmbAPINET
Imports System.Drawing.Imaging


Public Class PreviewXimea

    Public Width, Height As Integer
    Public Bmp, BmpXimea, bmpf As Bitmap
    Public ZmapBmp As FastBMP
    Public Exposure As Single
    Public R As Rectangle


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
    Dim Camera As XimeaXIC
    Public X0, Y0, ROI_W, ROI_H As Integer
    Public GreyEdge()() As Single
    Public GreyEdge2D()(,) As Single
    Public Scale As Single
    Dim Zprofiler As ZstackStructureFrameBurst
    ' create video source
    Public Sub New(Z As Integer, Zsteps As Single, Pbar As ProgressBar)
        'Z has to be an odd number to have the symetric acqusition.


        Camera = New XimeaXIC(1, 0, 1, 1, 1, 500)
        Zprofiler = New ZstackStructureFrameBurst(Camera.W, Camera.H, 500, 50, 2, 0.1, 0.1, 1, Camera)
        X0 = Setting.Gett("Preview_X")
        Y0 = Setting.Gett("Preview_Y")
        ROI_W = Setting.Gett("Preview_W")
        ROI_H = Setting.Gett("Preview_H")
        Canvas = 0

        R = New Rectangle(X0 - Canvas, Y0 - Canvas, ROI_W + Canvas * 2, ROI_H + Canvas * 2)

        Width = Camera.W
        Height = Camera.H

        Flip = New Mirror(True, True)
        Cropfilter = New Crop(R)


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


        load_colormap()
    End Sub

    Public Function Capture(exposure As Integer, focus As Integer) As Bitmap


        BmpXimea = Camera.captureBmp(False)

        Width = BmpXimea.Width
        Height = BmpXimea.Height

        'Bmp = Cropfilter.Apply(BmpXimea)
        'Flip.ApplyInPlace(Bmp)

        Dim fbmp As New FastBMP(BmpXimea)
        Dim R, G, B As Single
        For y = 0 To fbmp.height - 1
            For x = 0 To fbmp.width - 1
                fbmp.GetPixel(x, y, R, G, B)
                'B = B * 3

                'If B > 255 Then B = 255

                fbmp.FillOriginalPixel(x, y, G, G, G)
            Next
        Next
        fbmp.Reset()


        Return fbmp.bmp

    End Function


    Public Function Capture() As Bitmap


        BmpXimea = Camera.captureBmp(False)

        Width = BmpXimea.Width
        Height = BmpXimea.Height

        Bmp = Cropfilter.Apply(BmpXimea)

        Flip.ApplyInPlace(Bmp)

        Return Bmp

    End Function




    Public Function CaptureWhole(exposure As Integer, focus As Integer) As Bitmap


        BmpXimea = Camera.captureBmp(False)
        Bmp = BmpXimea.Clone()
        Return Bmp
    End Function

    Public Sub StopPreview()
        Camera.StopAcqusition()

    End Sub

    Public Sub SetExposure(exp As Single)
        Camera.SetExposure(exp, False)

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

    Public Sub EstimateProfile(offset As Single)

        Dim ccMatrix As Single = Camera.CCMAtrix
        Camera.ResetMatrix()

        Camera.SetBurstMode(True, Zprofiler.Z)
        Zprofiler.AcquireSmooth(True, 1)

        Do Until Zprofiler.WrapUpDone

        Loop
        Dim bmp As New Bitmap(Camera.W, Camera.H, Imaging.PixelFormat.Format24bppRgb)
        byteToBitmap(Zprofiler.OutputBytes, bmp)
        Camera.SetBurstMode(False, Zprofiler.Z)

        bmp.Save("c:\temp\Zprofiler.jpg")
    End Sub
End Class
