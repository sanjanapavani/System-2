Imports xiApi.NET

Imports System.Windows
Public Class XimeaCropped
    Private cam As New xiCam

    Public readoutnoise As Single
    Public Name As String
    Public Dim_X As Integer
    Public Dim_Y As Integer
    Public status As Boolean
    Public FFsetup As Boolean
    Public Wbinned, Hbinned As Integer
    Public Type As String
    Public busy As Boolean
    Public gain As Single
    Public exp As Single


    Public BmpRef As Bitmap
    Public bit_scale As Byte = 2 ^ 4


    Private timeout As Integer

    Dim frameRAW As Byte()
    Public frame As Byte()

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
            cam.SetParam(PRM.IMAGE_DATA_FORMAT, IMG_FORMAT.RAW8)

            '    cam.SetParam(PRM.TRG_SELECTOR, 1)
            'cam.SetParam(PRM.ACQ_TIMING_MODE, ACQ_TIMING_MODE.frameRAW_RATE)
            cam.SetParam(PRM.TRG_SOURCE, TRG_SOURCE.SOFTWARE)
            '  cam.SetParam(PRM.VERTICAL_FLIP, 1)
            ' cam.SetParam(PRM.HORIZONTAL_FLIP, 1)


            cam.SetParam(PRM.SENSOR_TAPS, 4)

            gain = Setting.Gett("Gain")

            setGain(gain)
            'SetColorGain(Setting.Gett("GainR"), Setting.Gett("GainG"), Setting.Gett("GainB"))
            setGammaY(1)
            setGammaC(0)

            exp = Setting.Gett("exposure")
            SetExposure(exp, True)
            SetBinning(False, 1)

            timeout = 5000
            busy = False
            status = True

            '------------------------------------------------------------
            cam.StartAcquisition()

        End If



    End Sub

    Public Sub SetBinning(yes As Boolean, size As Integer)
        If yes Then
            cam.SetParam(PRM.DOWNSAMPLING, size)
            Wbinned = cam.GetParamInt(PRM.WIDTH) / 2
            Hbinned = cam.GetParamInt(PRM.HEIGHT) / 2

            BmpRef = New Bitmap(Wbinned, Hbinned, Imaging.PixelFormat.Format24bppRgb)
            ReDim frameRAW(Wbinned * Hbinned * 4)
            ReDim frame(Wbinned * Hbinned)
        Else
            cam.SetParam(PRM.DOWNSAMPLING, 1)
            Dim_X = cam.GetParamInt(PRM.WIDTH) / 2
            Dim_Y = cam.GetParamInt(PRM.HEIGHT) / 2
            BmpRef = New Bitmap(Dim_X * 2, Dim_Y * 2, Imaging.PixelFormat.Format24bppRgb)
            ReDim frameRAW(Dim_X * Dim_Y * 4)
            ReDim frame(Dim_X * Dim_Y)
        End If
    End Sub
    Public Sub Capture_Threaded()
        Dim Thread1 As New System.Threading.Thread(AddressOf captureBmp)
        Thread1.Start()


    End Sub


    Public Sub Capture()
        Try
            cam.SetParam(PRM.TRG_SOFTWARE, 1)
            cam.GetImageByteArray(frameRAW, timeout)

            Dim frame2D(Dim_X * 2 - 1, Dim_Y * 2 - 1) As Single
            Dim p As Integer
            Dim y1, y2, x1, x2 As Integer
            y1 = 0 : y2 = Dim_Y * 2 - 1
            x1 = 0 : x2 = Dim_X * 2 - 1

            For j = y1 To y2
                For i = x1 To x2
                    frame2D(i, j) = frameRAW(p)
                    p += 1
                Next
            Next

            p = 0

            y1 = 0 : y2 = Dim_Y - 1
            x1 = 0 : x2 = Dim_X - 1

            For j = y1 To y2
                For i = x1 To x2
                    frame(p) = frame2D(i, j)
                    p += 1
                Next
            Next


        Catch ex As Exception

        End Try



    End Sub


    Public Sub SetFlatField(filename As String)
        cam.SetParam(PRM.FFC_FLAT_FIELD_FILE_NAME, filename)
        cam.SetParam(PRM.FFC_DARK_FIELD_FILE_NAME, "dark.tif")
        cam.SetParam(PRM.FFC, 1)
        FFsetup = True
    End Sub


    Public Sub Flatfield(value As Integer)
        cam.SetParam(PRM.FFC, value)
    End Sub
    Public Sub Capture(ByRef frame() As Byte)

        cam.SetParam(PRM.TRG_SOFTWARE, 1)
        cam.GetImageByteArray(frameRAW, timeout)

        Dim frame2D(Dim_X * 2 - 1, Dim_Y * 2 - 1) As Byte
        Dim p As Integer
        Dim y1, y2, x1, x2 As Integer
        y1 = 0 : y2 = Dim_Y * 2 - 1
        x1 = 0 : x2 = Dim_X * 2 - 1

        For j = y1 To y2
            For i = x1 To x2
                frame2D(i, j) = frameRAW(p)
                p += 1
            Next
        Next

        p = 0

        y1 = 0 : y2 = Dim_Y - 1
        x1 = 0 : x2 = Dim_X - 1

        For j = y1 To y2
            For i = x1 To x2
                frame(p) = frame2D(i, j)
                p += 1
            Next
        Next

        p = 0
    End Sub



    Public Sub captureBmp()

        Try
            cam.SetParam(PRM.TRG_SOFTWARE, 1)
            cam.GetBitmap(BmpRef, timeout)

        Catch ex As Exception

        End Try

    End Sub

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
    End Sub

    Public Sub setGammaC(G As Single)
        cam.SetParam(PRM.GAMMAC, G)
    End Sub

    Public Sub SetExposure(ex As Single, save As Boolean)
        If ex = 0 Then MsgBox("Richard : Please Enter a valid value for exposure time.") : Exit Sub
        cam.SetParam(PRM.EXPOSURE, ex * 1000000)
        timeout = ex * 100000
        If save Then Setting.Sett("exposure", ex)
        exp = ex
    End Sub

    Public Sub SetExposure()
        'Sets exposure time in microseconds.
        If exp = 0 Then MsgBox("Richard : Please Enter a valid value for exposure time." + vbCrLf + "Expsure is fixed!") : Exit Sub
        cam.SetParam(PRM.EXPOSURE, exp * 1000000)
        timeout = exp * 100000

    End Sub


End Class
