Public Class ZstackStructure
    Public W, H, Z As Integer
    Public loopZ As Integer
    Dim Pattern2D(,), ScalingUpPattern(), ScalingDownPattern() As Integer
    Dim GreenBytes(), GreenEdgeBytes()() As Single
    Public MaxMapValue() As Single
    Public MaxMapPosition() As Single
    Public ActivelyCapturing As Boolean = False
    Public MaxMap2D(,) As Single
    Dim BLure As FFTW_VB_Real
    Public bytes()() As Byte

    Dim zc As Integer
    Dim current As Long
    Dim ticks As Long
    Public ImageBeingCaptured As Boolean
    Dim processDone() As Integer
    Public Imagecreated() As Integer
    Public direction As Integer
    Dim Deivative As CentralDerivitavie
    Dim StepSize As Single = 0.01
    Dim Range As Single
    Dim Scale As Integer
    Dim Zstart As Single
    Dim myB As Integer
    Public TimeStamp() As Single
    Public TimeStampProcessed() As Single
    Public WrapUpDone As Boolean
    Public OutputBytes() As Byte
    Dim watch As Stopwatch
    Dim MoveThread As System.Threading.Thread

    Public Sub New(W As Integer, H As Integer, Range As Single, Stepsize As Single, scale As Integer)
        Me.W = W
        Me.H = H
        Me.Scale = scale
        Me.Range = (Range / 1000)
        Me.StepSize = (Stepsize / 1000)
        Z = Int(Range / Stepsize)

        ReDim TimeStamp(Z - 1)
        ReDim TimeStampProcessed(Z - 1)
        ReDim GreenEdgeBytes(Z - 1)
        ReDim bytes(Z - 1)
        ReDim processDone(Z - 1)
        ReDim Imagecreated(Z - 1)
        watch = New Stopwatch

        For zi = 0 To Z - 1

            ReDim GreenEdgeBytes(zi)(W / scale * H / scale - 1)
            ReDim bytes(zi)(W * H * 3 - 1)
        Next
        ReDim GreenBytes(W / scale * H / scale - 1)
        BLure = New FFTW_VB_Real(W / scale, H / scale)
        BLure.MakeGaussianReal(0.01, BLure.MTF, 2)
        Deivative = New CentralDerivitavie(W / scale, H / scale)

        'For some stupid reason, the 2D rotates when it copied to a 1D array. It is wiered.... 
        ReDim Pattern2D(H - 1, W - 1)
        ReDim ScalingUpPattern(W * H - 1)
        ReDim ScalingDownPattern(W / scale * H / scale - 1)
        Dim j As Integer = 0
        For y = 0 To H - 1 Step scale
            For x = 0 To W - 1 Step scale

                For yb = y To y + 1
                    For xb = x To x + 1
                        Pattern2D(yb, xb) = j
                    Next
                Next
                j += 1

            Next
        Next
        Buffer.BlockCopy(Pattern2D, 0, ScalingUpPattern, 0, W * H * 4)

        Dim i As Integer = 0
        Dim p As Integer = 1
        For Y = 0 To H / scale - 1
            For X = 0 To W / scale - 1
                ScalingDownPattern(i) = p
                p = p + 6
                i += 1
            Next
            p = Y * W * 3 * scale + 1
        Next
        ReDim MaxMapValue(W / scale * H / scale - 1)
        ReDim MaxMapPosition(W / scale * H / scale - 1)

        ReDim MaxMap2D(W / scale - 1, H / scale - 1)
        direction = 1
    End Sub
    Public Sub Clear()
        ReDim GreenEdgeBytes(Z - 1)
        ReDim bytes(Z - 1)
        For zi = 0 To Z - 1
            ReDim GreenEdgeBytes(zi)(W / Scale * H / Scale - 1)
            ReDim bytes(zi)(W * H * 3 - 1)
        Next
        ReDim GreenBytes(W / Scale * H / Scale - 1)
    End Sub
    Public Sub Upload(bytesin() As Byte, zi As Integer)

        Buffer.BlockCopy(bytesin, 0, bytes(zi), 0, bytesin.Length)
        zc = zi
    End Sub


    Public Sub PrepareAcquire(retrn As Boolean, direction As Integer, myB As Integer)
        WrapUpDone = False
        Me.myB = myB
        ticks = (Camera.exp * Stopwatch.Frequency / 1000)

        Me.direction = direction
        If retrn Then direction = 1
        Array.Clear(Imagecreated, 0, Z)
        Array.Clear(processDone, 0, Z)
        Dim AcquireThread As New System.Threading.Thread(AddressOf AcquireSingle)
        SuperScan.Activeb = myB
        ActivelyCapturing = True
        loopZ = -1
        AcquireThread.Start()

        Dim Thread As New System.Threading.Thread(AddressOf ProcessThreaded)
        Thread.Start()



        watch.Start()
    End Sub
    Public Sub AcquireSingle()




        Do
            Do

            Loop Until ImageBeingCaptured
            ImageBeingCaptured = False
            MakeDelay()
            Stage.MoveRelativeAsync(Stage.Zaxe, StepSize * direction, False)

        Loop Until loopZ = Z - 1
        ActivelyCapturing = False




    End Sub

    Public Sub Acquire(retrn As Boolean, direction As Integer)
        WrapUpDone = False
        ticks = (Camera.exp * Stopwatch.Frequency / 1000)
        Zstart = Stage.Z
        'MakeDelay()
        Me.direction = direction
        If retrn Then direction = 1
        Array.Clear(Imagecreated, 0, Z)
        Array.Clear(processDone, 0, Z)

        Dim Thread As New System.Threading.Thread(AddressOf ProcessThreaded)
        '    Dim ThreadMove As New System.Threading.Thread(AddressOf MoveThreaded)

        Thread.Start()

        For loopZ = 0 To Z - 1
            Camera.SetTrigger()
            MakeDelay()


            Stage.MoveRelativeAsync(Stage.Zaxe, StepSize * direction, False)
            Camera.cam.GetImageByteArray(bytes(loopZ), Camera.timeout)
            'Try
            '    Camera.cam.GetImageByteArray(bytes(loopZ), Camera.timeout)
            'Catch ex As Exception

            'End Try

            ''Camera.TriggerOff()
            Imagecreated(loopZ) = 1
        Next
        If retrn Then Stage.MoveRelativeAsync(Stage.Zaxe, -StepSize * Z, False)


    End Sub
    Public Sub MoveThreaded()
        Stage.MoveRelative(Stage.Zaxe, StepSize * direction, False)
    End Sub
    Public Sub ProcessThreaded()

        Dim i As Integer
        ReDim OutputBytes(W * H * 3 - 1)
        Dim max, maxZ As Single
        Dim maxi As Integer = W / Scale * H / Scale - 1

        ReDim MaxMapValue(W / Scale * H / Scale - 1)
        ReDim MaxMapPosition(W / Scale * H / Scale - 1)

        Dim Zi As Integer = 0

        For Zi = 0 To Z - 1
            Do Until Imagecreated(Zi) = 1
                'Application.DoEvents()
            Loop
            GetColorBytes(bytes(Zi), GreenBytes, W / Scale, H / Scale)
            Deivative.AnalyzeX(GreenBytes, GreenEdgeBytes(Zi))
            BLure.UpLoad(GreenEdgeBytes(Zi))
            BLure.Process_FT_MTF()
            BLure.DownLoad(GreenEdgeBytes(Zi))

            For i = 0 To maxi
                If GreenEdgeBytes(Zi)(i) > MaxMapValue(i) Then MaxMapValue(i) = GreenEdgeBytes(Zi)(i) : maxZ = Zi : MaxMapPosition(i) = maxZ
            Next

            processDone(Zi) = 1
        Next
        Wrapup()
    End Sub

    Public Function Wrapup() As Byte()
        Dim i As Integer


        'SaveSinglePageTiff16("c:\temp\maxmap.tif", MaxMap, W / 2, H / 2)
        'saveSinglePage32("c:\temp\maxtest.tif", maxtest, W / 2, H / 2)
        'For Zi = 0 To Z - 1
        '    saveSinglePage32("c:\temp\d\" + Zi.ToString, GreenEdgeBytes(Zi), W / 2, H / 2)
        '    SaveSinglePageTiff16("c:\temp\i\" + Zi.ToString, bytes(Zi), W, H)
        'Next
        i = 0
        Dim index As Integer
        Dim j As Integer
        Dim maxj = W * H * 3 - 1
        ' 78 ms 
        For j = 0 To maxj Step 3
            index = MaxMapPosition(ScalingUpPattern(i))
            OutputBytes(j) = bytes(index)(j)
            OutputBytes(j + 1) = bytes(index)(j + 1)
            OutputBytes(j + 2) = bytes(index)(j + 2)
            i += 1
        Next
        WrapUpDone = True
        Return OutputBytes
    End Function
    Public Sub Process()
        GetColorBytes(bytes(zc), GreenBytes, W / Scale, H / Scale)
        Deivative.AnalyzeX(GreenBytes, GreenEdgeBytes(zc))
        BLure.UpLoad(GreenEdgeBytes(zc))
        BLure.Process_FT_MTF()
        BLure.DownLoad(GreenEdgeBytes(zc))
        processDone(zc) = 1


    End Sub
    Public Sub MakeDelay()

        current = System.Diagnostics.Stopwatch.GetTimestamp()

        While (System.Diagnostics.Stopwatch.GetTimestamp() - current) < ticks
            Application.DoEvents()
        End While

    End Sub


    Public Sub ProcessAll(bytesin()() As Byte)

        Me.bytes = bytes

        For zi = 0 To Z - 1
            GetColorBytes(bytes(zi), GreenBytes, W / Scale, H / Scale)
            Deivative.AnalyzeX(GreenBytes, GreenEdgeBytes(zi))
            BLure.UpLoad(GreenEdgeBytes(zi))
            BLure.Process_FT_MTF()
            BLure.DownLoad(GreenEdgeBytes(zi))
        Next

    End Sub

    Public Function EstimateZ() As Single(,)
        ReDim OutputBytes(W * H * 3 - 1)
        Dim max, maxZ As Single
        Dim X As Integer = W / Scale
        Dim Y As Integer = H / Scale
        Dim maxi As Integer = W / Scale * H / Scale - 1
        Dim i As Integer
        ReDim MaxMapPosition(W / Scale * H / Scale - 1)

        Do Until (processDone.Sum = Z)

        Loop


        If direction = 1 Then
            i = 0
            For yy = 0 To Y - 1
                For xx = 0 To X - 1
                    max = 0
                    For Zi = 0 To Z - 1
                        If GreenEdgeBytes(Zi)(i) > max Then max = GreenEdgeBytes(Zi)(i) : maxZ = Zi : MaxMapPosition(i) = maxZ : MaxMap2D(xx, yy) = maxZ * StepSize + Zstart
                    Next
                    i += 1
                Next
            Next
        Else
            i = 0
            For yy = 0 To Y - 1
                For xx = 0 To X - 1
                    max = 0
                    For Zi = 0 To Z - 1
                        If GreenEdgeBytes(Zi)(i) > max Then max = GreenEdgeBytes(Zi)(i) : maxZ = Zi : MaxMapPosition(i) = maxZ : MaxMap2D(xx, yy) = Zstart - maxZ * StepSize
                    Next
                    i += 1
                Next
            Next


        End If

        Return MaxMap2D
    End Function
    Sub GetColorBytes(BytesIn() As Byte, ByRef BytesOut() As Single, Wb As Integer, Hb As Integer)
        'to read green 
        ReDim BytesOut(Wb * Hb - 1)
        Dim max As Integer = Wb * Hb - 1
        For i = 0 To max
            BytesOut(i) = BytesIn(ScalingDownPattern(i))
        Next
    End Sub
End Class
