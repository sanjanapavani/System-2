Imports System.Drawing.Imaging
Imports System.Threading

Imports MathNet.Numerics
Module AutoExposureFinder
    Dim nImages As Integer = 6
    Dim nImagesCropped As Integer
    Dim Spline As Interpolation.LinearSpline
    Public AutoExposure As Single
    Public Sub EstimateAutoExposure()
        Dim CurrentExposure As Single = Camera.exp
        Dim ExpImage(nImages)() As Byte
        Dim i As Integer = 0
        For i = 0 To nImages
            ReDim ExpImage(i)(Camera.W * Camera.H * 3 - 1)
        Next

        Camera.ResetMatrix()

        Dim ExposureArray() As Single = {0.05, 0.2, 0.4, 0.8, 1.2, 1.6}

        Form1.UpdateLED(True)
        Thread.Sleep(50)

        For i = 0 To nImages - 1
            ExposureArray(i) = ExposureArray(i) * 10
            Camera.SetExposure(ExposureArray(i), False)
            Camera.Capture(ExpImage(i))
        Next
        Form1.UpdateLED(Form1.CheckBoxLED.Checked)

        Dim MaxJ As Integer = Camera.W * Camera.H * 3 - 1
        Dim Max As Byte

        Dim Ix(nImages - 1), IY(nImages - 1) As Double

        Dim bmp As New Bitmap(Camera.W, Camera.H, Imaging.PixelFormat.Format24bppRgb)

        For i = 0 To nImages - 1
            byteToBitmap(ExpImage(i), bmp)
            bmp.Save("c:\temp\" + i.ToString + ".bmp", ImageFormat.Bmp)
            Array.Sort(ExpImage(i))
            Ix(i) = ExpImage(i)(MaxJ - 100)
            IY(i) = ExposureArray(i)
            Console.WriteLine(Ix(i).ToString)

        Next

        For i = 0 To nImages - 1
            If Ix(i) > 250 Then Exit For
        Next
        nImagesCropped = i

        Dim IxCropped(nImagesCropped - 1), IYCropped(nImagesCropped - 1) As Double
        For i = 0 To nImagesCropped - 1
            IxCropped(i) = Ix(i)
            IYCropped(i) = IY(i)
        Next
        If nImagesCropped >= 2 Then
            Spline = Interpolate.Linear(IxCropped, IYCropped)
            AutoExposure = Math.Round(Spline.Interpolate(250) * 100) / 100
        Else
            MsgBox("Cannot Estimate Exposure") : AutoExposure = CurrentExposure
        End If

        If AutoExposure > 30 Or AutoExposure < 0 Then MsgBox("Cannot Estimate Exposure") : AutoExposure = CurrentExposure
        Console.WriteLine("Auto Exposure: " + AutoExposure.ToString)


        Form1.UpdateLED(True)
        Camera.SetExposure(AutoExposure, False)
        Camera.CaptureBmp.Save("c:\temp\Autoexposure.bmp", Imaging.ImageFormat.Bmp)
        Form1.UpdateLED(Form1.CheckBoxLED.Checked)
    End Sub
End Module
