Imports System.Windows.Forms.DataVisualization.Charting
Imports AForge.Imaging



Public Class ImageDisplayXiq

    Dim Bayer As AForge.Imaging.Filters.BayerFilter
    Dim W, H As Integer
    Public sampeling As Integer

    Dim rawImage As ByteImage
    Dim rawImageSampled As ByteImage
    Dim OutputImage As ByteImage

    Public zoom As Boolean
    Public BmpPreview As FastBMP
    Public bmp As FastBMP

    Public GainR, GainG, GainB As Single

    Dim HistBin As Integer = 255
    Public ib, ic As Single
    Dim Histogram() As Integer

    Public Sub New(Win As Integer, Hin As Integer, sin As Integer)
        W = Win
        H = Hin

        sampeling = sin
        rawImage.Width = W
        rawImage.Height = H
        ' Get Raw Data
        rawImage.Size = rawImage.Width * rawImage.Height
        ReDim rawImage.data(rawImage.Size - 1)

        ReDim OutputImage.data(rawImage.Size - 1)
        OutputImage.Width = W
        OutputImage.Height = H
        OutputImage.bmp8bit = New Bitmap(W, H, Imaging.PixelFormat.Format8bppIndexed)

        rawImage.bmp8bit = New Bitmap(W, H, Imaging.PixelFormat.Format8bppIndexed)

        rawImageSampled.AdjustedWidth = (W \ (2 * sampeling)) * sampeling * 2
        rawImageSampled.AdjustedHeight = (H \ (2 * sampeling)) * sampeling * 2
        rawImageSampled.Width = rawImageSampled.AdjustedWidth / sampeling
        rawImageSampled.Height = rawImageSampled.AdjustedHeight / sampeling
        rawImageSampled.Size = rawImageSampled.Width * rawImageSampled.Height
        ReDim rawImageSampled.data(rawImageSampled.Size - 1)
        rawImageSampled.bmp8bit = New Bitmap(rawImageSampled.Width, rawImageSampled.Height, Imaging.PixelFormat.Format8bppIndexed)
        rawImageSampled.Sampling = sampeling

        bmp = New FastBMP(W, H, Imaging.PixelFormat.Format24bppRgb)
        BmpPreview = New FastBMP(rawImageSampled.Width, rawImageSampled.Height, Imaging.PixelFormat.Format24bppRgb)

        Bayer = New Filters.BayerFilter
        Dim Pattern(1, 1) As Integer
        Pattern = {{RGB.G, RGB.R}, {RGB.B, RGB.G}}
        Bayer.BayerPattern = Pattern

        zoom = False

        SetColorGain(Setting.Gett("GainR"), Setting.Gett("GainG"), Setting.Gett("GainB"))

    End Sub
    Public Sub AdjustBrightness()
        SetIbIc()
    End Sub

    Public Sub PlotHistogram(chrt As Chart)

        chrt.Series(0).Points.Clear()

        For j = 0 To HistBin
            chrt.Series(0).Points.AddXY(j + 1, Histogram(j))
        Next

    End Sub
    'Public Sub DrawIbIc(chrt As Chart)
    '    chrt.Series(1).Points.Clear()
    '    chrt.Series(2).Points.Clear()
    '    chrt.Series(1).Points.AddXY(ib, Histogram.Max)
    '    chrt.Series(2).Points.AddXY(ic, Histogram.Max)
    'End Sub
    Public Function MakeFullsizeImage() As Bitmap

        Array.Copy(rawImage.data, OutputImage.data, rawImage.Size - 1)

        ApplyColorGain(OutputImage)

        byteToBitmap(OutputImage.data, OutputImage.bmp8bit)

        '  Bayer.Apply(OutputImage.bmp8bit)

        bmp = New FastBMP(Bayer.Apply(OutputImage.bmp8bit))

        Return bmp.bmp


    End Function

    Public Sub ApplyColorGain(ByRef rawImageIn As ByteImage)
        'This is for resampling the image with different Sampling index
        Dim i, j As Integer

        Dim Offset As Integer = 0

        Dim i1, i2 As Integer
        Dim R, G, B As Integer
        For j = 0 To rawImageIn.Height - 1 Step 2
            'reading RGRG line
            i1 = Offset : i2 = Offset + rawImageIn.Width - 2
            For i = i1 To i2 Step 2
                R = rawImageIn.data(i) * GainG
                If R > 255 Then R = 255

                G = rawImageIn.data(i + 1) * GainR
                If G > 255 Then G = 255

                rawImageIn.data(i) = R
                rawImageIn.data(i + 1) = G

            Next
            Offset += rawImageIn.Width
            i1 = Offset : i2 = Offset + rawImageIn.Width - 2
            'reading GBGB line
            For i = i1 To i2 Step 2

                G = rawImageIn.data(i) * GainB
                If G > 255 Then G = 255

                B = rawImageIn.data(i + 1) * GainG
                If B > 255 Then B = 255

                rawImageIn.data(i) = G
                rawImageIn.data(i + 1) = B

            Next
            Offset += rawImageIn.Width
        Next



    End Sub


    Public Sub ApplyColorGain(ByRef rawImageIn() As Byte, width As Integer, height As Integer)
        'This is for resampling the image with different Sampling index
        Dim i, j As Integer

        Dim Offset As Integer = 0

        Dim i1, i2 As Integer
        Dim R, G, B As Integer
        For j = 0 To height - 1 Step 2
            'reading RGRG line
            i1 = Offset : i2 = Offset + width - 2
            For i = i1 To i2 Step 2
                R = rawImageIn(i) * GainG
                If R > 255 Then R = 255

                G = rawImageIn(i + 1) * GainR
                If G > 255 Then G = 255

                rawImageIn(i) = R
                rawImageIn(i + 1) = G

            Next
            Offset += width
            i1 = Offset : i2 = Offset + width - 2
            'reading GBGB line
            For i = i1 To i2 Step 2

                G = rawImageIn(i) * GainB
                If G > 255 Then G = 255

                B = rawImageIn(i + 1) * GainG
                If B > 255 Then B = 255

                rawImageIn(i) = G
                rawImageIn(i + 1) = B

            Next
            Offset += width
        Next



    End Sub

    Public Sub ApplyColorGain(ByRef bmp As FastBMP)


        bmp.Unlock()
        Dim R, G, B As Integer
        Dim i As Integer
        For y = 0 To bmp.height - 1
            For x = 0 To bmp.width - 1
                R = bmp.byteCopy(i + 2) * GainR
                If R > 255 Then R = 255

                G = bmp.byteCopy(i + 1) * GainG
                If G > 255 Then G = 255

                B = bmp.byteCopy(i) * GainB
                If B > 255 Then B = 255

                bmp.SetPixel(x, y, B, G, R)

                i += 3

            Next

        Next
        bmp.lock()



    End Sub

    Public Sub Preview(rawin As Byte(), Gained As Boolean)
        rawImage.data = rawin

        If zoom Then
            ZoomImage(rawImage, rawImageSampled)
        Else
            If Gained Then
                ResampleImage(rawImage, rawImageSampled, GainR, GainG, GainB)
            Else
                ResampleImage(rawImage, rawImageSampled, 1, 1, 1)
            End If

        End If


        BayerInterpolate(CoolBright(rawImageSampled.data))

    End Sub


    Public Sub SetColorGain(R As Single, G As Single, B As Single)
        GainR = R
        GainB = B
        GainG = G

        Setting.Sett("GainB", B)
        Setting.Sett("GainG", G)
        Setting.Sett("GainR", R)
    End Sub





    Sub BayerInterpolate(bytein As Byte())
        byteToBitmap(bytein, rawImageSampled.bmp8bit)
        BmpPreview = New FastBMP(Bayer.Apply(rawImageSampled.bmp8bit))
    End Sub


    Public Sub ResampleImage(rawImageIn As ByteImage, ByRef rawImageOut As ByteImage, GainR As Single, GianG As Single, GainB As Single)
        'This is for resampling the image with different Sampling index
        Dim p, i, j As Integer
        'ShapeSampledImage(rawImageOut, rawImageIn.Width, rawImageIn.Height, rawImageOut.Sampling)
        Dim length As Integer = rawImageIn.Width * rawImageOut.AdjustedHeight
        ReDim rawImageOut.data(rawImageOut.Size - 1)
        ''The steps are snap.Format.Width * 2 * Sample, 2 is to take into accout the fact that we read two lines each time. First line is RGRGRG and second line is BGBGBG
        ''Sample is to skip the lines 
        Dim Offset As Integer = 0
        Dim Steps As Integer = rawImageOut.Sampling * 2
        Dim i1, i2 As Integer
        Dim R, G, B As Integer
        For j = 0 To rawImageOut.AdjustedHeight - 1 Step Steps
            'reading RGRG line
            i1 = Offset : i2 = Offset + rawImageOut.AdjustedWidth - Steps
            For i = i1 To i2 Step Steps
                R = rawImageIn.data(i) * GainG
                If R > 255 Then R = 255

                G = rawImageIn.data(i + 1) * GainR
                If G > 255 Then G = 255

                rawImageOut.data(p) = R
                rawImageOut.data(p + 1) = G
                p = p + 2
            Next
            Offset += rawImageIn.Width
            i1 = Offset : i2 = Offset + rawImageOut.AdjustedWidth - Steps
            'reading GBGB line
            For i = i1 To i2 Step Steps

                G = rawImageIn.data(i) * GainB
                If G > 255 Then G = 255

                B = rawImageIn.data(i + 1) * GainG
                If B > 255 Then B = 255

                rawImageOut.data(p) = G
                rawImageOut.data(p + 1) = B
                p = p + 2
            Next
            Offset += rawImageIn.Width * (rawImageOut.Sampling * 2 - 1)
        Next

    End Sub


    Private Sub ZoomImage(rawImageIn As ByteImage, ByRef rawImageOut As ByteImage)
        'This is for resampling the image with different Sampling index
        Dim p, i As Integer



        Dim length As Integer = rawImageIn.Width * rawImageOut.AdjustedHeight
        ReDim rawImageOut.data(rawImageOut.Size - 1)
        'This is the  intial offset. It goes to the top left corner of the image

        Dim Offset = rawImageIn.Width * (rawImageIn.Height - rawImageOut.Height) / 2 + (rawImageIn.Width - rawImageOut.Width) / 2
        For j = 0 To rawImageOut.Height - 1
            For i = Offset To Offset + rawImageOut.Width - 1
                rawImageOut.data(p) = rawImageIn.data(i)

                p = p + 1
            Next
            Offset += rawImageIn.Width
        Next
    End Sub




    Private Sub MakeHistogram()
        ReDim Histogram(HistBin)


        For i = 0 To rawImageSampled.Size - 1

            Histogram(rawImageSampled.data(i)) += 1

        Next

        ' We never have zero intensity  this make  it possible  to find the min on the SetIbIc using the min function
        Histogram(0) = Histogram(1)
    End Sub
    Public Sub SetIbIc()
        MakeHistogram()
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


    End Sub

    Public Function CoolBright(ByRef ByteImage As Byte()) As Byte()

        Dim length As Integer = ByteImage.Length
        Dim ByteImageStretched(length - 1) As Byte
        Dim c As Integer
        Dim p As Integer = 0
        If ib = ic Then ib = 0 : ic = 255
        For i = 0 To length - 1
            c = (ByteImage(i) - ib) / (ic - ib) * 255
            If c < 0 Then c = 0
            If c > 255 Then c = 255
            ByteImageStretched(i) = c
        Next

        Return ByteImageStretched
    End Function

End Class
