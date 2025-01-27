Imports xiApi.NET

Imports System.Windows
Public Class XimeaXIq
    Public cam As New xiCam

    Public readoutnoise As Single
    Public Name As String
    Public W As Integer
    Public H As Integer
    Public status As Boolean
    Public FFsetup As Boolean
    Public Wbinned, Hbinned As Integer
    Public FF() As Single
    Public ready As Boolean
    Public busy As Boolean
    Public gain As Single
    Public exp As Single
    Public CCMAtrix As Single
    Public BmpRef As Bitmap
    Public bit_scale As Byte = 2 ^ 4
    Dim ROI As Rectangle
    Public OriginalW, OriginalH As Integer
    Public readout_time As Single

    Public timeout As Integer
    Public Cropped As Boolean
    Public Bytes As Byte()
    Public TRG_MODE As Integer
    Public ExposureChanged As Boolean
    Public Dostop As Boolean




    Public Sub New()
        Dim numCams As Integer = cam.GetNumberDevices()
        status = False
        If numCams > 0 Then

            ' open first device
            cam.OpenDevice(0)


            Name = cam.GetParamString(PRM.DEVICE_NAME)
            cam.SetParam(PRM.BUFFER_POLICY, BUFF_POLICY.SAFE)
            cam.SetParam(PRM.IMAGE_DATA_FORMAT, IMG_FORMAT.RGB24)

            '    cam.SetParam(PRM.TRG_SELECTOR, 1)
            cam.SetParam(PRM.ACQ_TIMING_MODE, ACQ_TIMING_MODE.FREE_RUN)


            cam.SetParam(PRM.TRG_SOURCE, TRG_SOURCE.OFF)
            cam.SetParam(PRM.OUTPUT_DATA_BIT_DEPTH, BIT_DEPTH.BPP_8)

            cam.SetParam(PRM.HORIZONTAL_FLIP, 0)
            cam.SetParam(PRM.VERTICAL_FLIP, 0)

            OriginalW = cam.GetParamInt(PRM.WIDTH)
            OriginalH = cam.GetParamInt(PRM.HEIGHT)
            W = OriginalW
            H = OriginalH
            ReDim Bytes(W * H * 3 - 1)
            timeout = 50

            cam.SetParam(PRM.SHARPNESS, 0)
            gain = Setting.Gett("Gain")
            setGain(gain)
            SetColorGain(Setting.Gett("GainR"), Setting.Gett("GainG"), Setting.Gett("GainB"))
            setGammaY(Setting.Gett("GAMMAY"))
            setGammaC(Setting.Gett("GAMMAC"))
            exp = Setting.Gett("exposureb")

            cam.SetParam(PRM.CC_MATRIX_23, 0)
            Dim val As Integer
            cam.GetParam(PRM.EXPOSURE, Val)

            SetExposure(exp, True)
            ResetMatrix()
            Cropped = False
            BmpRef = New Bitmap(W, H, Imaging.PixelFormat.Format24bppRgb)
            busy = False
            status = True
            readout_time = 20
            StartAcqusition()

        End If



    End Sub

    Public Sub StartAcqusition()
        cam.StartAcquisition()
    End Sub

    Public Sub StopAcqusition()
        cam.StopAcquisition()
    End Sub

    Public Sub SetROI()
        If Not Cropped Then Return
        cam.SetParam(PRM.DOWNSAMPLING, 1)
        cam.SetParam(PRM.OFFSET_X, 0)
        cam.SetParam(PRM.OFFSET_Y, 0)
        cam.SetParam(PRM.WIDTH, ROI.Width)
        cam.SetParam(PRM.HEIGHT, ROI.Height)
        cam.SetParam(PRM.OFFSET_X, ROI.X)
        cam.SetParam(PRM.OFFSET_Y, ROI.Y)
        W = ROI.Width
        H = ROI.Height

    End Sub

    Public Sub ReSetROI()
        If Not Cropped Then Return
        cam.SetParam(PRM.DOWNSAMPLING, 1)
        cam.SetParam(PRM.OFFSET_X, 0)
        cam.SetParam(PRM.OFFSET_Y, 0)
        cam.SetParam(PRM.WIDTH, OriginalW)
        cam.SetParam(PRM.HEIGHT, OriginalH)
        W = OriginalW
        H = OriginalH


        Cropped = False

    End Sub
    Public Sub SetROI(ROI As Rectangle)
        Me.ROI = ROI

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
    Public Sub Capture_Threaded()
        Dim Thread1 As New System.Threading.Thread(AddressOf CaptureBmp)
        Thread1.Start()


    End Sub
    Public Sub ResetMatrix()
        cam.SetParam(PRM.CC_MATRIX_00, 1)
        cam.SetParam(PRM.CC_MATRIX_11, 1)
        cam.SetParam(PRM.CC_MATRIX_22, 1)
        CCMAtrix = 1
    End Sub
    Public Sub SetMatrix(CCMAtrix As Single)
        If CCMAtrix > 8 Then CCMAtrix = 8
        If CCMAtrix < 1 Then CCMAtrix = 1
        cam.SetParam(PRM.CC_MATRIX_00, CCMAtrix)
        cam.SetParam(PRM.CC_MATRIX_11, CCMAtrix)
        cam.SetParam(PRM.CC_MATRIX_22, CCMAtrix)
        Me.CCMAtrix = CCMAtrix
    End Sub
    Public Sub capture(Optional Trigger As Boolean = False)

        If Trigger Then cam.SetParam(PRM.TRG_SOFTWARE, 1)
        cam.GetImageByteArray(Bytes, timeout)

    End Sub
    Public Sub Capture(ByRef Bytes As Byte(), Optional Trigger As Boolean = False)

        If Trigger Then cam.SetParam(PRM.TRG_SOFTWARE, 1)
        cam.GetImageByteArray(Bytes, timeout)

    End Sub
    Public Sub SetTrigger()
        cam.SetParam(PRM.TRG_SOFTWARE, 1)
    End Sub
    Public Sub SetFlatField(filename As String, bfilename As String)

        Try
            SetDataMode(Colortype.RGB)
            cam.SetParam(PRM.FFC_FLAT_FIELD_FILE_NAME, filename)
            cam.SetParam(PRM.FFC_DARK_FIELD_FILE_NAME, bfilename)
            cam.SetParam(PRM.FFC, 1)
            FFsetup = True
        Catch ex As Exception
            cam.SetParam(PRM.FFC, 0)
            FFsetup = False
        End Try



    End Sub


    Public Sub Flatfield(value As Integer)
        cam.SetParam(PRM.FFC, value)


    End Sub
    Public Sub capture_binned(ByRef framein)
        Try
            cam.SetParam(PRM.TRG_SOFTWARE, 1)
            cam.GetImageByteArray(framein, timeout)

        Catch ex As Exception

        End Try

    End Sub



    Public Function CaptureBmp(Optional Trigger As Boolean = True) As Bitmap
        If Trigger Then cam.SetParam(PRM.TRG_SOFTWARE, 1)
        cam.GetBitmap(BmpRef, timeout)
        Return BmpRef
    End Function

    Public Sub SetDataMode(type As Colortype)
        Select Case type
            Case Colortype.RGB
                cam.SetParam(PRM.IMAGE_DATA_FORMAT, IMG_FORMAT.RGB24)
            Case Colortype.Grey
                cam.SetParam(PRM.IMAGE_DATA_FORMAT, IMG_FORMAT.RAW8)
        End Select

    End Sub




    'Public Sub SetColorGain(R As Integer, G As Integer, B As Integer)
    '    cam.SetParam(PRM.WB_KB, B)
    '    cam.SetParam(PRM.WB_KG, G)
    '    cam.SetParam(PRM.WB_KR, R)
    '    Setting.Sett("GainB", B)
    '    Setting.Sett("GainG", G)
    '    Setting.Sett("GainR", R)


    'End Sub

    Public Sub setGain(Gai As Single)
        gain = Gai
        cam.SetParam(PRM.GAIN, gain)
        Setting.Sett("Gain", gain)

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
        timeout = exp * 8000

    End Sub


End Class
