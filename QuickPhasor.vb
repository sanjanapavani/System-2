
Public Class QuickPhasor
    Public Width As Integer
    Dim rr(), gg(), bb() As Single
    Dim C(2), S(2) As Single
    Dim Histogram(,) As Single
    Dim imgWidth, imgHeight As Integer
    Public Plot As FastBMP
    Dim Gauss(,) As Single
    Dim Re, Im, sum As Single
    Dim img() As Long

    Dim Mask(,) As Single
    Public Image2Phasor()() As Single
    Public Remin, Remax As Integer
    Dim pbox As PictureBox
    Dim Max As Single


    Public Sub New(Width As Integer, imgWidth As Integer, imgHeight As Integer)
        Me.pbox = pbox
        Me.Width = Width

        Me.imgWidth = imgWidth
        Me.imgHeight = imgHeight


        C(0) = 1
        C(1) = -0.5
        C(2) = -0.5

        S(0) = 0
        S(1) = Math.Sqrt(3) / 2
        S(2) = -Math.Sqrt(3) / 2

        ReDim Gauss(Width, Width)
        ReDim Mask(Width - 1, Width - 1)
        load_colormap()

        ReDim Histogram(Width - 1, Width - 1)

    End Sub

    Public Sub MakeHistogram(imagein As FastBMP, MakeNew As Boolean)


        Plot = New FastBMP(Width, Width, Imaging.PixelFormat.Format24bppRgb)
        Dim offset As Integer = imagein.stride - imagein.width * 3


        ReDim Image2Phasor(1)
        ReDim Image2Phasor(0)(imgWidth * imgHeight - 1)
        ReDim Image2Phasor(1)(imgWidth * imgHeight - 1)




        Dim x, y, ii As Integer
        If MakeNew Then
            ReDim Histogram(Width - 1, Width - 1)
            Max = 0
        End If
        Remin = Width


        Dim p As Integer = 0
        For y = 0 To imagein.height - 1
            For x = 0 To imagein.width - 1
                sum = 0
                Re = 0
                Im = 0


                For j = 0 To 2

                    sum += imagein.bytes(p + j)
                    Re += imagein.bytes(p + j) * C(j)
                    Im += imagein.bytes(p + j) * S(j)
                Next



                If sum > 0 Then
                    '   sum = (sum ^ 0.5) * 10
                    Re = Int((Re / sum + 1) * Width / 2)
                    Im = Int((Im / sum + 1) * Width / 2)
                    If Re < Remin Then Remin = Re
                    If Re > Remax Then Remax = Re
                    If Re > 0 And Re < Width And Im > 0 And Im < Width Then
                        Histogram(Re, Im) += 1

                        Image2Phasor(0)(ii) = Re
                        Image2Phasor(1)(ii) = Im
                    End If

                End If


                ii += 1

                p += 3

            Next
            p += offset
        Next

        For i = 0 To Width - 1
            For j = 0 To Width - 1
                Histogram(i, j) = Math.Sqrt(Histogram(i, j))
                If Histogram(i, j) > Max Then Max = Histogram(i, j)
            Next
        Next

        Dim index As Integer
        For y = 0 To Width - 1
            For x = 0 To Width - 1
                index = Histogram(x, y) * 1000 / Max
                Plot.FillOriginalPixel(x, y, rr(index), gg(index), bb(index))
            Next
        Next

        Plot.MakeNewFromBytes()


    End Sub





    Public Sub Segment(imagein As FastBMP, ByRef imageout As FastBMP)
        imageout = New FastBMP(imagein.width, imagein.height, Imaging.PixelFormat.Format24bppRgb)
        Dim offset As Integer = imagein.stride - imagein.width * 3

        Dim u As Single
        Dim p, i As Integer
        Dim sum As Single
        Dim RGB(2) As Single

        p = 0



        Dim smooth As Single

        For y = 0 To imagein.height - 1
            For x = 0 To imagein.width - 1

                sum = 0
                RGB(0) = imagein.bytes(p)
                RGB(1) = imagein.bytes(p + 1)
                RGB(2) = imagein.bytes(p + 2)

                sum = (RGB(0) + RGB(1) + RGB(2)) / 3
                smooth = (Mask(Image2Phasor(0)(i), Image2Phasor(1)(i)) * sum)

                imageout.FillOriginalPixel(x, y, smooth, smooth, smooth)
                ' To make sure it is stretched enough and to avoid the flatfielding artefact, ( some times flatfielding goes as low as 0.2) 


                p += 3
                i += 1
            Next
            p += offset
        Next



        imageout.Unlock()
        imageout.lock()



    End Sub
    Public Sub CreateMask(x1 As Single, x2 As Single, y2 As Single)

        Plot.Reset()
        Plot.GR.DrawLine(New Pen(Color.White), x1, 0, x1, Width)
        Plot.GR.DrawLine(New Pen(Color.White), x2, 0, x2, Width)
        Plot.GR.DrawLine(New Pen(Color.White), 0, y2, Width, y2)

        For y = 0 To Width - 1
            For x = 0 To Width - 1
                'Mask(x, y) = line(x1, x2, x) * line(0, y2, y)
                Mask(x, y) = Sigmoid(x, x2, 1) * Sigmoid(y, y2, 1)
                'Mask(x, y) = line(x1, x2, x)
            Next
        Next
        saveSinglePage32("c:\test\mask.tif", Mask)
    End Sub
    Public Function Sigmoid(x As Single, b As Single, a As Single)
        Return 1 / (1 + Math.Exp((x - b) * a))
    End Function
    Public Function line(x1 As Integer, x2 As Integer, x As Integer)
        Dim m As Single = 1 / (x1 - x2)
        Dim b As Single = -m * x2
        Dim y As Single

        If x <= x1 Then y = 1
        If x > x1 And x < x2 Then y = m * x + b
        If x >= x2 Then y = 0

        Return y
    End Function
    Public Function SigmoidR(x As Single, b As Single, a As Single)
        Return 1 / (1 + Math.Exp((x - b) * a))
    End Function









    Public Sub load_colormap()
        ReDim rr(1000), gg(1000), bb(1000)

        FileOpen(1, "rgb.txt", OpenMode.Input)

        Dim t As Integer
        Do Until EOF(1)
            t += 1
            Input(1, rr(t))
            Input(1, gg(t))
            Input(1, bb(t))
        Loop
        FileClose(1)



    End Sub
End Class
