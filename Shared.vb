Imports System.Windows.Forms.DataVisualization.Charting
Imports Zaber.Motion.Ascii
Imports Zaber.Motion


Public Enum Colortype
    RGB
    Grey
End Enum


Public Enum AcqusitionTypes
    Slide
    FiBi
    EDF_FiBi
    EDF_Slide
End Enum

Public Enum Objectives
    _10X
    _20X
End Enum
Public Structure ByteImage
    Dim data As Byte()
    Dim Width As Integer
    Dim Height As Integer
    Dim Size As Integer
    Dim Sampling As Byte
    Dim sum As Integer
    Dim bmp8bit As Bitmap

    ' when the width is not a whole multiple of the steps  this is needed
    Dim AdjustedWidth, AdjustedHeight As Integer
End Structure
Module SharedResources

    Public GammaY, GammaC As Single
    Public DehazeRadius, DehazeWeight As Single
    Public LEDcontroller As Relay

    Public Setting As New SettingStructure("Settings.xml")
    Public Camera As XimeaXIC
    Public Stage As ZaberASCII
    Public Display As ImageDisplay
    Public Piezo As EO
    Public EDF As ExtendedDepth5
    Public AutoFocus As FocusStructure
    Public rr(), gg(), bb() As Byte
    Public clrs(20) As Color
    Public Preview As PreviewWebcam
    Public Triangle As TriangulationStructure
    Public Tracking As TrackingStructure

    Public Zprofiler As ZstackContinuous
    Public block As Boolean = False
    Public ScanUnits() As ZstackContinuous
    Public Scanning As Boolean
    Public FlatField()(,) As Single
    Public FlatFieldB(), FlatFieldG(), FlatFieldR() As Single
    Public ScanBufferSize As Integer = 10
    Public Const BacklightWhiteLED = 2
    Public Const BlueLED = 3
    Public Const BlueLED_RichardMode = 4
    Public Const PreviewLED = 1
    Public ScanOverlap As Integer = 100
    Public Dehaze As DehazeClass
    Public CrazyCamera As CrazyCameraClass


    Public Sub LoadObjects(ByRef Pbar As ProgressBar, ByRef chart1 As Chart)

        'Dim PortIDs() As String = GetPortIDs()
        'Dim PortNames() As String = GetPortNames()
        '' Array.Sort(PortNames, PortIDs)
        'Dim XYport As String
        'Dim Zport As String
        'Dim StainerStageport As String
        'Dim LEDport As String
        'Dim StainerPort As String
        'For i = 0 To PortNames.Length - 1
        '    If PortIDs(i) = Setting.Gett("PORTID_XY") Then XYport = PortNames(i)
        '    If PortIDs(i) = Setting.Gett("PORTID_Z") Then Zport = PortNames(i)
        '    If PortIDs(i) = Setting.Gett("PORTID_LED") Then LEDport = PortNames(i)
        '    If PortIDs(i) = Setting.Gett("PORTID_Stainer") Then StainerPort = PortNames(i)
        '    If PortIDs(i) = Setting.Gett("PORTID_STAINERSTAGE") Then StainerStageport = PortNames(i)
        'Next

        'ResetPorts()

        LEDcontroller = New Relay(0)
        LEDcontroller.SetRelays(PreviewLED, True)
        LEDcontroller.SetRelays(PreviewLED, False)


        'Stage = New ZaberASCII(Setting.Gett("FOVX"), Setting.Gett("FOVY"), "com7") 'JH COM7 for sys 2 COM2 for SYS 1

        Stage = New ZaberASCII(Setting.Gett("FOVX"), Setting.Gett("FOVY"), "com7") 'JH COM7 for sys 2 COM2 for SYS 1






        Camera = New XimeaXIC(0, Setting.Gett("Gain"), Setting.Gett("GainR"), Setting.Gett("GainG"), Setting.Gett("GainB"), Setting.Gett("Exposure"), 2048, 2048)
        Preview = New PreviewWebcam(380, 0.05, Pbar)

        'Dehaze = New DehazeClass(Camera.W, Camera.H, DehazeRadius, DehazeWeight, GammaY)


        '  Zprofiler = New ZstackContinuous(Camera.W, Camera.H, Setting.Gett("ZSTACRRANGE"), Setting.Gett("ZSTACKSTEPS"), 4, DehazeRadius, DehazeWeight, GammaY, Camera)
        'AutoFocus = New FocusStructure(ZEDOF.Range, 0.1, 4)
        Display = New ImageDisplay(Camera.W, Camera.H, chart1)
        ReDim ScanUnits(ScanBufferSize - 1)

    End Sub




    Public Function factorial(ByVal n As Integer) As Integer
        If n <= 1 Then
            Return 1
        Else
            Return factorial(n - 1) * n
        End If
    End Function
    Public Sub ColorGenerator()

        For i = 0 To 19
            clrs(i) = HSBtoARGB(255, i * 17, 0.8, 1)
        Next


    End Sub


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
        Dim c As Color
        'For t = 1 To 1000
        '    c = HSBtoARGB(255, t * 360 / 1000, 1, 1)

        '    rr(t) = c.R
        '    gg(t) = c.G
        '    bb(t) = c.B

        'Next

    End Sub

    Public Function HSBtoARGB(ByVal a As Integer, ByVal h As Double, ByVal s As Double, ByVal b As Double) As Color

        Dim red As Double = 0.0
        Dim green As Double = 0.0
        Dim blue As Double = 0.0

        If (s = 0) Then

            red = b
            green = b
            blue = b

        Else

            ' the color wheel consists of 6 sectors. Figure out which sector you're in.
            Dim sectorPos As Double = h / 60.0
            Dim sectorNumber As Integer = CInt(Math.Floor(sectorPos))
            ' get the fractional part of the sector
            Dim fractionalSector As Double = sectorPos - sectorNumber

            ' calculate values for the three axes of the color. 
            Dim p As Double = b * (1.0 - s)
            Dim q As Double = b * (1.0 - (s * fractionalSector))
            Dim t As Double = b * (1.0 - (s * (1 - fractionalSector)))

            ' assign the fractional colors to r, g, and b based on the sector the angle is in.
            Select Case sectorNumber
                Case 0
                    red = b
                    green = t
                    blue = p
                Case 1
                    red = q
                    green = b
                    blue = p
                Case 2
                    red = p
                    green = b
                    blue = t
                Case 3
                    red = p
                    green = q
                    blue = b
                Case 4
                    red = t
                    green = p
                    blue = b
                Case 5
                    red = b
                    green = p
                    blue = q
            End Select

        End If

        Return Color.FromArgb(a, red * 255, green * 255, blue * 255)

    End Function

    Public Sub Floodfill(ByRef Ar(,) As Integer, xnode As Integer, ynode As Integer, target As Integer, replacement As Integer)
        If xnode < 0 Or ynode < 0 Then Exit Sub
        If xnode > Ar.GetUpperBound(0) Or ynode > Ar.GetUpperBound(1) Then Exit Sub

        If Ar(xnode, ynode) <> target Then Exit Sub

        Ar(xnode, ynode) = replacement

        Floodfill(Ar, xnode - 1, ynode, target, replacement)
        Floodfill(Ar, xnode + 1, ynode, target, replacement)

        Floodfill(Ar, xnode, ynode - 1, target, replacement)
        Floodfill(Ar, xnode, ynode + 1, target, replacement)

    End Sub

    Public Function GetColor(alpha As Integer, k As Integer) As Color
        Dim H(20) As Integer
        H = {0, 60, 120, 180, 240, 300, 30, 90, 150, 210, 270, 330, 15, 45, 75, 105, 135, 165, 195, 225, 255, 285, 315, 345}
        Return HSBtoARGB(alpha, H(k), 1, 1)

    End Function


    Public Function ReadJPG(address As String) As Single()(,)
        Dim bmp As New Bitmap(address)
        Dim bytes(bmp.Width * bmp.Height * 3 - 1) As Byte

        Dim AdjustedWidth As Integer = BitmapToBytes(bmp, bytes) - bmp.Width * 3
        Dim Image(2)(,) As Single


        For j = 0 To 2
            ReDim Image(j)(bmp.Width - 1, bmp.Height - 1)
        Next

        Dim i As Integer = 0
        For y = 0 To bmp.Height - 1
            For x = 0 To bmp.Width - 1

                Image(0)(x, y) = bytes(i)
                Image(1)(x, y) = bytes(i + 1)
                Image(2)(x, y) = bytes(i + 2)
                i += 3
            Next
            i += AdjustedWidth
        Next
        Return Image
    End Function

    Public Sub Wait(time As Single)
        'Dim watch As New Stopwatch
        'watch.Start()
        'While watch.ElapsedMilliseconds < time
        '    Application.DoEvents()
        'End While
        'watch.Stop()

        Dim ticks As Long = time * Stopwatch.Frequency / 1000
        Dim watch As New Stopwatch
        watch.Start()
        While watch.ElapsedTicks < ticks
            Application.DoEvents()
        End While
        watch.Stop()
    End Sub
End Module
