Imports xiApi.NET

Imports System.Windows
Imports AForge.Imaging
Imports Google.Protobuf.WellKnownTypes

Public Class XimeaXIC
    Public cam As New xiCam

    Public readoutnoise As Single
    Public Name As String
    Public W As Integer
    Public H As Integer
    Dim ROI As Rectangle
    Public OriginalW, OriginalH As Integer

    Public status As Boolean
    Public FFsetup As Boolean
    Public Wbinned, Hbinned As Integer
    Public Type As String
    Public busy As Boolean
    Public ready As Boolean
    Public gain As Single
    Public exp As Single
    Public BmpRef As Bitmap
    Public bit_scale As Byte = 2 ^ 4
    Public timeout As Integer
    Public CCMAtrix As Single
    Public Bytes As Byte()
    Public TRG_MODE As Integer
    Public ExposureChanged As Integer = 2
    Public MatrixResetRequested As Boolean
    Public MatrixResetChanged As Boolean
    Public Cropped As Boolean
    Public readout_time As Single
    Public Dostop As Boolean
    Public Captured As Boolean = False
    Public isAutoExposure As Boolean = False



    Public Sub New(Devicenumber As Integer, Gain As Single, GainR As Single, GainG As Single, GainB As Single, exposure As Single, Optional ROIW As Integer = 0, Optional ROIH As Integer = 0)
        Dim numCams As Integer = cam.GetNumberDevices()
        status = False
        If numCams > 0 Then

            ' open first device
            cam.OpenDevice(Devicenumber)

            Try
                cam.SetParam(PRM.LIMIT_BANDWIDTH_MODE, 1)
            Catch ex As Exception

            End Try


            cam.SetParam(PRM.LIMIT_BANDWIDTH, 3184)
            Name = cam.GetParamString(PRM.DEVICE_NAME)
            cam.SetParam(PRM.BUFFER_POLICY, BUFF_POLICY.SAFE)
            cam.SetParam(PRM.IMAGE_DATA_FORMAT, IMG_FORMAT.RGB24)

            '    cam.SetParam(PRM.TRG_SELECTOR, 1)
            cam.SetParam(PRM.ACQ_TIMING_MODE, ACQ_TIMING_MODE.FREE_RUN)
            cam.SetParam(PRM.TRG_SOURCE, TRG_SOURCE.OFF)
            cam.SetParam(PRM.VERTICAL_FLIP, 1)
            ' cam.SetParam(PRM.HORIZONTAL_FLIP, 1)
            cam.SetParam(PRM.SENSOR_DATA_BIT_DEPTH, BIT_DEPTH.BPP_10)
            cam.SetParam(PRM.OUTPUT_DATA_BIT_DEPTH, BIT_DEPTH.BPP_8)


            cam.SetParam(PRM.SHARPNESS, 0)

            setGain(Gain)
            SetColorGain(GainR, GainG, GainB)


            SetExposure(exposure, True)
            ResetMatrix()


            timeout = 5000
            readout_time = 10
            busy = False
            status = True

            OriginalW = cam.GetParamInt(PRM.WIDTH)
            OriginalH = cam.GetParamInt(PRM.HEIGHT)

            If ROIW <> 0 Then SetROI(New Rectangle((OriginalW - ROIW) / 2, (OriginalH - ROIH) / 2, ROIW, ROIH)) Else ReSetROI()
            'ReSetROI()

            SetDataMode(Colortype.RGB)


            StartAcqusition()

            status = True
        End If



    End Sub


    Public Sub SetBurstMode(mode As Boolean, nframes As Integer)

        If mode Then
            StopAcqusition()

            cam.SetParam(PRM.TRG_SOURCE, TRG_SOURCE.SOFTWARE)
            cam.SetParam(PRM.TRG_SELECTOR, TRG_SELECTOR.FRAME_BURST_START)
            'cam.SetParam(PRM.TRG_SELECTOR, TRG_SELECTOR.FRAME_BURST_ACTIVE)
            cam.SetParam(PRM.GPO_MODE, GPO_MODE.EXPOSURE_PULSE)
            cam.SetParam(PRM.ACQ_FRAME_BURST_COUNT, nframes)
            cam.SetParam(PRM.BUFFERS_QUEUE_SIZE, nframes + 1)
            StartAcqusition()
        Else
            StopAcqusition()
            cam.SetParam(PRM.ACQ_TIMING_MODE, ACQ_TIMING_MODE.FREE_RUN)
            cam.SetParam(PRM.TRG_SOURCE, TRG_SOURCE.OFF)

            cam.SetParam(PRM.ACQ_FRAME_BURST_COUNT, 1)
            cam.SetParam(PRM.BUFFERS_QUEUE_SIZE, 2)
            StartAcqusition()
        End If

    End Sub


    Public Sub SensorType(type As Integer)
        cam.SetParam(PRM.SENSOR_TAPS, type)
    End Sub
    Public Sub StartAcqusition()
        cam.StartAcquisition()
    End Sub

    Public Sub StopAcqusition()
        cam.StopAcquisition()
    End Sub
    Public Sub SetROI()
        cam.SetParam(PRM.DOWNSAMPLING, 1)
        cam.SetParam(PRM.OFFSET_X, 0)
        cam.SetParam(PRM.OFFSET_Y, 0)
        cam.SetParam(PRM.WIDTH, ROI.Width)
        cam.SetParam(PRM.HEIGHT, ROI.Height)
        cam.SetParam(PRM.OFFSET_X, ROI.X)
        cam.SetParam(PRM.OFFSET_Y, ROI.Y)
        W = ROI.Width
        H = ROI.Height
        Cropped = True
    End Sub
    Public Sub SetROI(ROI As Rectangle)
        Me.ROI = ROI

        cam.SetParam(PRM.DOWNSAMPLING, 1)
        cam.SetParam(PRM.OFFSET_X, 0)
        cam.SetParam(PRM.OFFSET_Y, 0)
        cam.SetParam(PRM.WIDTH, ROI.Width)
        cam.SetParam(PRM.HEIGHT, ROI.Height)
        If ROI.X Mod 2 = 1 Then ROI.X += 1
        If ROI.Y Mod 2 = 1 Then ROI.Y += 1

        cam.SetParam(PRM.OFFSET_X, ROI.X)
        cam.SetParam(PRM.OFFSET_Y, ROI.Y)
        W = ROI.Width
        H = ROI.Height
        Cropped = True
    End Sub
    Public Sub ReSetROI()
        cam.SetParam(PRM.DOWNSAMPLING, 1)
        cam.SetParam(PRM.OFFSET_X, 0)
        cam.SetParam(PRM.OFFSET_Y, 0)
        cam.SetParam(PRM.WIDTH, OriginalW)
        cam.SetParam(PRM.HEIGHT, OriginalH)
        W = OriginalW
        H = OriginalH
        BmpRef = New Bitmap(W, H, Imaging.PixelFormat.Format24bppRgb)
        Cropped = False
    End Sub
    Public Sub SetMatrix(CCMAtrix As Single)
        If CCMAtrix > 8 Then CCMAtrix = 8
        If CCMAtrix < 1 Then CCMAtrix = 1
        cam.SetParam(PRM.CC_MATRIX_00, CCMAtrix)
        cam.SetParam(PRM.CC_MATRIX_11, CCMAtrix)
        cam.SetParam(PRM.CC_MATRIX_22, CCMAtrix)
        Me.CCMAtrix = CCMAtrix
    End Sub

    Public Sub SetBlueOffset(offsset As Single)
        cam.SetParam(PRM.CC_MATRIX_23, offsset)
    End Sub

    Public Sub ResetBlueOffset()
        cam.SetParam(PRM.CC_MATRIX_23, 0)
    End Sub
    Public Sub ResetMatrix()
        cam.SetParam(PRM.CC_MATRIX_00, 1)
        cam.SetParam(PRM.CC_MATRIX_11, 1)
        cam.SetParam(PRM.CC_MATRIX_22, 1)
        CCMAtrix = 1
    End Sub
    Public Sub SetColorGain(R As Single, G As Single, B As Single)
        cam.SetParam(PRM.WB_KB, B)
        cam.SetParam(PRM.WB_KG, G)
        cam.SetParam(PRM.WB_KR, R)


    End Sub
    Public Sub SetBinning(size As Integer)


        cam.SetParam(PRM.DOWNSAMPLING, size)
        cam.SetParam(PRM.DOWNSAMPLING_TYPE, BINNING_MODE.SUM)
        Wbinned = cam.GetParamInt(PRM.WIDTH)
        Hbinned = cam.GetParamInt(PRM.HEIGHT)
        BmpRef = New Bitmap(Wbinned, Hbinned, Imaging.PixelFormat.Format24bppRgb)
        ReDim Bytes(Wbinned * Hbinned - 1)


    End Sub
    Public Sub SetPolicyToSafe()
        cam.SetParam(PRM.BUFFER_POLICY, BUFF_POLICY.SAFE)
    End Sub
    Public Sub SetPolicyToUNSafe()
        cam.SetParam(PRM.BUFFER_POLICY, BUFF_POLICY.UNSAFE)
    End Sub
    Public Sub Capture_Threaded()
        Dim Thread1 As New System.Threading.Thread(AddressOf CaptureWithoutTrigg)
        Thread1.Start()

    End Sub
    Public Sub capture(Optional Trigger As Boolean = True)
        Try
            If Trigger Then cam.SetParam(PRM.TRG_SOFTWARE, 1)
            cam.GetImageByteArray(Bytes, timeout)
        Catch ex As Exception

        End Try


    End Sub
    Public Sub CaptureWithoutTrigg()
        Captured = False
        cam.GetImageByteArray(Bytes, timeout)
        Captured = True
    End Sub

    Public Sub Capture(ByRef Bytes As Byte(), Optional Trigger As Boolean = True)

        If Trigger Then cam.SetParam(PRM.TRG_SOFTWARE, 1)
        cam.GetImageByteArray(Bytes, timeout)

    End Sub


    Public Sub Trigger()
        cam.SetParam(PRM.TRG_SOFTWARE, 1)
    End Sub


    Public Sub TriggerOff()
        cam.SetParam(PRM.TRG_SOFTWARE, 0)
    End Sub
    Public Sub Transfer()
        cam.GetImageByteArray(Bytes, timeout)
    End Sub


    Public Sub Transfer(ByRef Bytes)
        cam.GetImageByteArray(Bytes, timeout)
    End Sub

    Public Sub WaitUntillReady()
        Do Until ready

        Loop
    End Sub
    Public Sub Capture(ByRef framein)
        Try
            cam.SetParam(PRM.TRG_SOFTWARE, 1)
            cam.GetImageByteArray(framein, timeout)

        Catch ex As Exception

        End Try

    End Sub
    Public Sub Trigg()
        cam.SetParam(PRM.TRG_SOFTWARE, 1)
    End Sub
    Public Sub SetTrigger(mode As Boolean)
        If mode = True Then cam.SetParam(PRM.TRG_SOURCE, TRG_SOURCE.SOFTWARE) Else cam.SetParam(PRM.TRG_SOURCE, TRG_SOURCE.OFF)
    End Sub

    Public Sub SetFlatField(filename As String, bfilename As String)
        'cam.SetParam(PRM.FFC_FLAT_FIELD_FILE_NAME, filename)
        'cam.SetParam(PRM.FFC_DARK_FIELD_FILE_NAME, bfilename)
        'cam.SetParam(PRM.FFC, 1)
        'FFsetup = True
    End Sub

    Public Sub Flatfield(value As Integer)
        'cam.SetParam(PRM.FFC, value)

    End Sub


    'Public Sub CaptureXIMG()
    '    Dim XIimage As xiApi.XI_IMG
    '    cam.SetParam(PRM.TRG_SOFTWARE, 1)
    '    cam.GetXI_IMG()
    'End Sub

    Public Function captureBmp(Optional trigg As Boolean = True) As Bitmap
        If trigg Then cam.SetParam(PRM.TRG_SOFTWARE, 1)
        cam.GetBitmap(BmpRef, timeout)
        Return BmpRef
    End Function

    Public Sub SetDataMode(type As Colortype)
        'StopAcqusition()

        Select Case type
            Case Colortype.RGB
                cam.SetParam(PRM.IMAGE_DATA_FORMAT, IMG_FORMAT.RGB24)

                BmpRef = New Bitmap(W, H, Imaging.PixelFormat.Format24bppRgb)
                ReDim Bytes(W * H * 3 - 1)
                SetPolicyToSafe()
            Case Colortype.Grey
                cam.SetParam(PRM.IMAGE_DATA_FORMAT, IMG_FORMAT.RAW8)

                BmpRef = New Bitmap(W, H, Imaging.PixelFormat.Format8bppIndexed)
                ReDim Bytes(W * H - 1)
                SetPolicyToSafe()
        End Select

        'StartAcqusition()
    End Sub

    Public Sub setGain(Gain As Single)
        Me.gain = Gain
        cam.SetParam(PRM.GAIN_SELECTOR, GAIN_SELECTOR_TYPE.GAIN_SELECTOR_ANALOG_ALL)
        cam.SetParam(PRM.GAIN, Gain)
        cam.SetParam(PRM.GAIN_SELECTOR, GAIN_SELECTOR_TYPE.GAIN_SELECTOR_DIGITAL_ALL)
        cam.SetParam(PRM.GAIN, 0)


        Setting.Sett("Gain", Gain)

    End Sub

    Public Sub setGammaY(G As Single)

        cam.SetParam(PRM.GAMMAY, G)
        Setting.Sett("GAMMAY", G)
    End Sub

    Public Sub setGammaC(G As Single)

        cam.SetParam(PRM.GAMMAC, G)
        Setting.Sett("GAMMAC", G)
    End Sub

    Public Sub SetExposure(ex As Single, save As Boolean)
        If ex = 0 Then MsgBox("Richard : Please Enter a valid value for exposure time.") : Exit Sub
        cam.SetParam(PRM.EXPOSURE, ex * 1000)
        timeout = ex * 1000
        If save Then Setting.Sett("exposure", ex)
        exp = ex

    End Sub

    Public Sub SetExposure()
        'Sets exposure time in microseconds.
        If exp = 0 Then MsgBox("Richard : Please Enter a valid value for exposure time." + vbCrLf + "Expsure is fixed!") : Exit Sub
        cam.SetParam(PRM.EXPOSURE, exp * 1000)
        timeout = exp * 1000

    End Sub

    Public Sub ToggleAutoExposure(enable As Boolean, Optional targetLevel As Integer = 50, Optional exposurePriority As Single = 0)
        If enable Then
            cam.SetParam(PRM.AEAG, 1) ' Enable auto exposure and auto gain
            cam.SetParam(PRM.AEAG_LEVEL, targetLevel) ' Set target brightness level (0-100)
            cam.SetParam(PRM.EXP_PRIORITY, exposurePriority) ' Set exposure priority (1 for exposure, 0 for gain)
            cam.SetParam(PRM.AE_MAX_LIMIT, 60000)
        Else
            cam.SetParam(PRM.AEAG, 0) ' Disable auto exposure and auto gain
        End If
    End Sub

End Class
