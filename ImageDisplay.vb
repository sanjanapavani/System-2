Imports System.Threading
Imports System.Windows.Forms.DataVisualization.Charting
Imports AForge.Imaging
Imports MathNet.Numerics


Public Class ImageDisplay


    Public Width, Height As Integer
    Dim Bayer As AForge.Imaging.Filters.BayerFilter
    Dim Bucketsize As Integer = 10
    Dim rawImage(Bucketsize)() As Byte
    Public zoom As Boolean
    Public BmpPreview(Bucketsize) As FastBMP
    Public EmptyPreview As FastBMP
    Public f As Integer
    Public GainR, GainG, GainB As Single
    Public busy As Boolean
    Public HistBin As Integer = 255
    Public ib, ic As Single
    Public RequestIbIc As Integer
    Dim HistoChart As Chart
    Dim ImageSize As Integer

    Public AcqusitionType As AcqusitionTypes


    Public Histogram() As Single


    Public Sub New(W As Integer, H As Integer, ByRef HistoChart As Chart)
        ' Validate parameters
        If W <= 0 Or H <= 0 Then
            Throw New ArgumentException("Width and height must be positive integers.")
        End If

        If Bucketsize <= 0 Then
            Throw New ArgumentException("Bucketsize must be a positive integer.")
        End If

        Try
            ' Initialize properties
            Me.Width = W
            Me.Height = H
            Me.HistoChart = HistoChart
            ReDim Histogram(HistBin)

            ' Get Raw Data
            ImageSize = W * H * 3 - 1
            ReDim rawImage(Bucketsize)

            For i = 0 To Bucketsize - 1
                ' Ensure parameters passed to FastBMP are valid
                BmpPreview(i) = New FastBMP(W, H, Imaging.PixelFormat.Format24bppRgb)
                ReDim rawImage(i)(W * H * 3 - 1)
            Next

            ' Initialize EmptyPreview
            EmptyPreview = New FastBMP(W, H, Imaging.PixelFormat.Format24bppRgb)

        Catch ex As Exception
            ' Handle errors gracefully
            MessageBox.Show($"Error initializing FastBMP: {ex.Message}", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Throw
        End Try
    End Sub

    Public Sub AdjustBrightness()
        Dim C As Integer
        Do Until RequestIbIc = 3
            C += 1
            If C > 10 Then Exit Do
        Loop
        'MsgBox(C)
        RequestIbIc = 0
    End Sub

    Public Sub PlotHistogram()

        HistoChart.Series(0).Points.Clear()

        For j = 0 To HistBin - 1
            HistoChart.Series(0).Points.AddXY(j + 1, Histogram(j))
        Next


    End Sub

    Public Sub MakePreview(ByRef rawin As Byte(), Gained As Boolean)
        If RequestIbIc = 2 Then RequestIbIc = 3
        f += 1
        If f = Bucketsize - 1 Then f = 0
        Buffer.BlockCopy(rawin, 0, rawImage(f), 0, rawin.GetLength(0))

        BmpPreview(f).MakeFromBytes(rawImage(f))
        If RequestIbIc = 1 Then SetIbIc(True) : RequestIbIc = 2
        '  PlotHistogram()
    End Sub

    Public Sub ApplyBrightness(rawin As Byte(), CCMAtrix As Single, ByRef bmp As Bitmap)
        Dim p As Integer
        Dim r, g, b As Integer
        Dim rawtemp(rawin.GetLongLength(0)) As Byte

        For y = 0 To Height - 1
            For x = 0 To Width - 1
                b = rawin(p) * CCMAtrix
                g = rawin(p + 1) * CCMAtrix
                r = rawin(p + 2) * CCMAtrix
                If b > 255 Then b = 255
                If g > 255 Then g = 255
                If r > 255 Then r = 255

                rawtemp(p) = b
                rawtemp(p + 1) = g
                rawtemp(p + 2) = r
                p += 3
            Next
        Next

        byteToBitmap(rawtemp, bmp)

    End Sub

    Public Sub SetColorGain(R As Single, G As Single, B As Single, Imagingtype As AcqusitionTypes, Optional saving As Boolean = True)
        GainR = R
        GainB = B
        GainG = G
        If saving Then
            Select Case Imagingtype
                Case AcqusitionTypes.Slide, AcqusitionTypes.EDF_Slide
                    Setting.Sett("GainB", B)
                    Setting.Sett("GainG", G)
                    Setting.Sett("GainR", R)

                Case AcqusitionTypes.FiBi, AcqusitionTypes.EDF_FiBi
                    Setting.Sett("GainB_FiBi", B)
                    Setting.Sett("GainG_FiBi", G)
                    Setting.Sett("GainR_FiBi", R)

            End Select
        End If
        Camera.SetColorGain(R, G, B)
    End Sub



    Public Sub MakeHistogram()


        Array.Clear(Histogram, 0, HistBin + 1)
        Try
            For i = 0 To ImageSize - 1 Step 8
                Histogram(rawImage(f)(i) / Camera.CCMAtrix) += 1
            Next
        Catch ex As Exception

        End Try
        Dim max As Single = Histogram.Max

        For j = 0 To HistBin
            Histogram(j) = Histogram(j) / max
        Next

        ' We never have zero intensity  this make  it possible  to find the min on the SetIbIc using the min function
        Histogram(0) = Histogram(1)
    End Sub



    Public Sub MakeHistogram(bytes As Byte())


        Array.Clear(Histogram, 0, HistBin + 1)
        Try
            For i = 0 To ImageSize - 1 Step 8
                Histogram(bytes(i) / Camera.CCMAtrix) += 1
            Next
        Catch ex As Exception

        End Try
        Dim max As Single = Histogram.Max

        For j = 0 To HistBin - 1
            Histogram(j) = Histogram(j) / max
        Next

        ' We never have zero intensity  this make  it possible  to find the min on the SetIbIc using the min function
        Histogram(0) = Histogram(1)
    End Sub


    Public Sub SetIbIc(WithHistogram As Boolean)


        If WithHistogram Then MakeHistogram()

        CalculateIbIc()

        Camera.SetMatrix(255 / ic)


    End Sub
    Sub CalculateIbIc()
        Dim Hmax As Integer = 0
        Dim HCum(HistBin) As Integer
        Dim HCumMax, HCumMin As Integer
        'Accumulates the histogram bins
        Dim i As Integer
        For i = 0 To HistBin
            For ii = 0 To i
                HCum(i) += Histogram(ii) / HistBin
            Next
        Next

        HCumMax = HCum.Max
        HCumMin = HCum.Min

        For i = HistBin To 0 Step -1
            If Histogram(i) > HCumMax Then Exit For
        Next
        ic = i * 1.5
        If ic > HistBin Then ic = HistBin


        For i = 0 To ic
            If Histogram(i) > HCumMin Then Exit For
        Next
        ib = i


        Camera.SetMatrix(255 / ic)

    End Sub




End Class
